# TASK — Streaming / Chunked Transfer-Encoding for HttpResponse

**Status:** Open
**Type:** Capability gap
**Affects:** `HttpResponse` (response builder)
**Touches:** No new files required; extends the existing `HttpResponse` API.
**RFC reference:** RFC 9112 §7.1 (chunked transfer coding), RFC 9110 §6.1 (control data)

---

## Background — what currently exists

`HttpResponse` is a buffer-then-send builder. The full body must exist in memory as a `byte[]` before `ToBytes()` runs, and `ToBytes()` returns a single `byte[]` containing the complete framed response. The header section auto-adds `Content-Length` from `_body.Length`.

```csharp
// today's flow
byte[] payload = ...;                  // whole body in memory
byte[] response = new HttpResponse(200)
    .Header("Content-Type", "video/mp4")
    .Body(payload)                     // ← full buffer
    .ToBytes();                        // ← framed bytes, ready for socket
networkStream.Write(response, 0, response.Length);
```

This is correct, simple, and fine for HTML pages, JSON payloads, small static files (CSS/JS/font), and moderate downloads. It is also the reason the current API is so small — there is no streaming state to manage.

---

## The gap — three scenarios where buffer-then-send breaks

### 1. Server-Sent Events (SSE)

The server keeps the response open and writes `data: ...\n\n` frames over time. Each frame must hit the wire as it is produced. There is no `Content-Length` because the body is open-ended. Current `HttpResponse` cannot express this — `ToBytes()` is a one-shot terminal call.

### 2. Large file downloads

A 500 MB video file should not sit in a `byte[]` before the first byte goes down the wire. With the current API, the only options are: (a) load 500 MB into memory and call `Body(bytes)`, or (b) bypass `HttpResponse` entirely and hand-write the framing on the socket. Both are bad.

### 3. Long-polling / streaming API responses

Same shape as SSE — long-lived response, body produced incrementally, no fixed length up front. Common pattern for live dashboards, log tails, progress streams.

---

## Why chunked transfer coding solves this

RFC 9112 §7.1 defines a framing where the body is sent as a sequence of length-prefixed chunks, terminated by a zero-length chunk:

```
HTTP/1.1 200 OK
Content-Type: text/event-stream
Transfer-Encoding: chunked
\r\n
1c\r\n                  ← chunk size in hex (28 bytes)
data: hello world\n\n
\r\n
12\r\n                  ← next chunk size (18 bytes)
data: another\n\n
\r\n
0\r\n                   ← zero-length chunk = end of body
\r\n
```

The crucial property: the server can write each chunk to the socket as soon as the data is ready, and the client can render each chunk as soon as it arrives. No length is declared up front. The body ends when the server sends the zero-length chunk.

cshttp's parser already handles chunked **on the receive side** — that work is done. The gap is on the **send side** of `HttpResponse`.

---

## Proposed shape — minimum viable

The smallest API that solves all three scenarios. Adds streaming as a separate path; existing buffer-then-send code is unchanged.

```csharp
public sealed class HttpResponse
{
    // existing methods unchanged...

    /// <summary>
    /// Begin a chunked-transfer streaming response. Writes the status line,
    /// headers (with Transfer-Encoding: chunked added automatically), and
    /// the blank line terminator to the supplied stream. The caller then
    /// writes chunks via WriteChunk and finishes with EndChunked.
    ///
    /// After calling BeginChunked, do not call ToBytes on the same instance
    /// — the response has already started going down the wire.
    /// </summary>
    public void BeginChunked(Stream output);

    /// <summary>
    /// Write one chunk of body bytes to the stream. The chunk-size header
    /// and CRLF terminators are added automatically. Empty chunks (data.Length == 0)
    /// are silently dropped — calling EndChunked is the way to terminate.
    /// </summary>
    public static void WriteChunk(Stream output, byte[] data);
    public static void WriteChunk(Stream output, byte[] data, int offset, int count);
    public static void WriteChunk(Stream output, string text); // UTF-8

    /// <summary>
    /// Write the zero-length terminating chunk and the final CRLF. After
    /// this returns, the response body is complete and the caller may
    /// close the connection (or, for keep-alive, return it to the pool).
    /// </summary>
    public static void EndChunked(Stream output);
}
```

Usage examples for each scenario:

### SSE

```csharp
var resp = new HttpResponse(200)
    .Header("Content-Type", "text/event-stream")
    .Header("Cache-Control", "no-cache")
    .Header("Connection", "keep-alive");

resp.BeginChunked(networkStream);

while (!cancellation.IsCancellationRequested)
{
    string frame = "data: " + JsonConvert.SerializeObject(currentEvent) + "\n\n";
    HttpResponse.WriteChunk(networkStream, frame);
    networkStream.Flush();
    Thread.Sleep(1000);
}

HttpResponse.EndChunked(networkStream);
```

### Large file download

```csharp
var resp = new HttpResponse(200)
    .Header("Content-Type", "video/mp4")
    .Header("Content-Disposition", "attachment; filename=\"movie.mp4\"");

resp.BeginChunked(networkStream);

byte[] buf = new byte[64 * 1024];
using (var fs = File.OpenRead(path))
{
    int n;
    while ((n = fs.Read(buf, 0, buf.Length)) > 0)
        HttpResponse.WriteChunk(networkStream, buf, 0, n);
}

HttpResponse.EndChunked(networkStream);
```

### Long-polling

Same shape as SSE — `BeginChunked` once, `WriteChunk` whenever new data is available, `EndChunked` when the client disconnects or the producer signals done.

---

## Design constraints

1. **No allocations in the hot path.** `WriteChunk(Stream, byte[], int, int)` should write the chunk-size header directly to the stream as ASCII bytes, not allocate a new buffer per chunk. Important for SSE and high-frequency streams.

2. **No threading inside cshttp.** The caller controls the stream. cshttp does not own a background writer, does not spawn threads, does not introduce async/await (cshttp is currently synchronous; do not break that contract).

3. **Mutually exclusive with `ToBytes()`.** Calling `BeginChunked` puts the response in "streaming mode." A subsequent `ToBytes()` call should throw `InvalidOperationException` rather than produce an inconsistent result.

4. **Auto-add `Transfer-Encoding: chunked`.** If the caller has not set it, `BeginChunked` adds it. If the caller has also set `Content-Length`, `BeginChunked` should remove it — RFC 9112 forbids both being present, and `Transfer-Encoding` wins.

5. **Static methods for `WriteChunk` / `EndChunked`.** They do not depend on instance state — they are pure framing helpers on a stream. Static keeps the API honest.

6. **Synchronous IO matches cshttp's current style.** Do not introduce `async Task` overloads in this iteration. If async is added later, it is a separate, additive change.

---

## Edge cases and decisions to make

### Trailers (RFC 9112 §7.1.2)

Chunked encoding allows trailing headers after the zero-length chunk. Real-world use: gRPC over HTTP/1.1, integrity hashes computed during streaming (`Digest:`), checksums. **Recommendation: skip in v1.** Add `EndChunked(Stream output, IDictionary<string, string> trailers)` overload later if a real use case shows up. Most streaming responses do not use trailers.

### Chunk extensions

Chunked encoding allows per-chunk extensions (`a; foo=bar\r\n...`). Almost never used in practice. **Recommendation: never support writing them.** They are a known source of HTTP smuggling vulnerabilities and have no legitimate use cases that buffer-and-send cannot handle.

### Backpressure

If the client reads slowly, `Stream.Write` blocks until the OS send buffer drains. This is the correct, simple behavior — the producer naturally throttles to the consumer's rate. Do not try to be clever here. If a caller needs non-blocking sends, they should wrap the stream themselves.

### Connection lifetime after streaming

After `EndChunked`, the connection is reusable for keep-alive (the framing is cleanly terminated). The current cshttp consumers all close the socket after each response, so this is theoretical for now — but the design should not preclude reuse.

### Error handling mid-stream

If `Stream.Write` throws after `BeginChunked` but before `EndChunked` (client disconnected, network error), there is no clean recovery — the client will see a truncated chunked stream and treat it as a parse error. **This is correct behavior.** The application catches the exception, logs, and moves on. cshttp should not try to send error responses after streaming has begun.

---

## Out of scope

These are deliberately not part of this task. List them so future readers know they were considered:

- **Async / `Task`-based overloads.** Separate task. cshttp is currently synchronous end-to-end.
- **HTTP/2 streaming.** cshttp is HTTP/1.1 only by design.
- **Compression negotiation (`Content-Encoding: gzip` over chunked).** Application's job to gzip the bytes before calling `WriteChunk`.
- **Range requests (`Range:` / `Content-Range:`).** Different feature. Often paired with streaming for video, but the framing logic is independent.
- **Built-in helpers like `HttpResponse.SseStream(...)`.** Convenience layer, can be added later in a companion file. Keep the core API minimal.

---

## Acceptance criteria

The task is complete when all of the following pass:

1. **Unit tests** verify chunked framing matches RFC 9112 §7.1 byte-for-byte for: empty stream (zero-length chunk only), single chunk, multiple chunks, chunk sizes 1, 0xF, 0x10, 0xFF, 0x100, 0xFFFF, 0x10000.

2. **Integration test** — SSE-style scenario: server `BeginChunked` → loop writing 5 chunks one second apart → `EndChunked`. Client (using `HttpClient`) receives all 5 chunks in real time, not after `EndChunked`.

3. **Integration test** — large file scenario: stream a 100 MB file via `WriteChunk(buf, 0, n)` in 64 KB chunks. Memory usage on the server side stays under 1 MB throughout. Output file matches input file byte-for-byte after client receives it.

4. **API enforcement** — calling `ToBytes()` after `BeginChunked()` throws `InvalidOperationException`. Calling `BeginChunked()` twice throws. Calling `WriteChunk` / `EndChunked` without prior `BeginChunked` is allowed (they are pure stream helpers) but is the caller's responsibility.

5. **No regression** — every existing `HttpResponse` test continues to pass. The buffer-then-send path is byte-for-byte unchanged for callers who do not use chunked.

6. **Documentation** — `README-Full-Specification.txt` gets a new section on streaming responses with the three usage patterns above. `README.txt` Quick Start gets a one-paragraph mention with the SSE example.

---

## Estimated size

~150 lines of implementation, ~200 lines of tests. Single PR. No new files required — this is purely an extension to `HttpResponse.cs`.
