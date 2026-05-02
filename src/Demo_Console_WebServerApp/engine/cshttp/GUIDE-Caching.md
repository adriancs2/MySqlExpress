# HTTP Caching with cshttp — Developer Guide

**Audience:** Developers building servers on top of cshttp who want browsers to cache static files (and other cacheable responses) correctly.

**Scope:** This guide explains how to implement HTTP caching manually using only what cshttp gives you today (`HttpRequestMessage`, `HttpResponse`, headers, body). cshttp itself does not generate cache validators, does not honor conditional requests, and does not enforce cache policy — those decisions are intentionally left to the application. This document tells you exactly what to build.

---

## Why caching matters

A typical web page references CSS, JavaScript, fonts, and images that almost never change between page loads. Without caching, the browser re-downloads every one of those bytes on every navigation. Even on localhost it adds latency; over a real network it ruins page-load times.

HTTP caching solves this in two layers, both of which you implement yourself on top of cshttp:

| Layer | Mechanism | Effect |
|-------|-----------|--------|
| 1 | **Conditional GET** — `ETag` / `Last-Modified` + `If-None-Match` / `If-Modified-Since` | Browser still asks, server replies "304 Not Modified" with no body. ~100 bytes round-trip instead of resending the file. |
| 2 | **Cache-Control / Expires** | Browser does not ask at all for a configured duration. Zero round-trips. |

You usually want both. Layer 2 makes 90% of repeat requests free; layer 1 covers the cases where the browser decides to revalidate.

---

## Layer 1 — Conditional GET

### The protocol

When the server returns a static file, it includes one or both validators:

```
HTTP/1.1 200 OK
Content-Type: text/css
ETag: W/"2f52-8de9b9696c3c080"
Last-Modified: Wed, 15 Apr 2026 12:34:56 GMT
Content-Length: 12110

body bytes...
```

The browser caches the bytes alongside the validators. On the next request for the same URL, the browser includes the validators back:

```
GET /css/style.css HTTP/1.1
Host: example.com
If-None-Match: W/"2f52-8de9b9696c3c080"
If-Modified-Since: Wed, 15 Apr 2026 12:34:56 GMT
```

If the file has not changed, the server responds with **304 Not Modified** and no body:

```
HTTP/1.1 304 Not Modified
ETag: W/"2f52-8de9b9696c3c080"
```

Browser uses the cached bytes. ~100 bytes on the wire instead of 12 KB.

If the file has changed, the server responds with a normal `200 OK` and the new body, including new validators. The browser replaces its cached copy.

### Generating an ETag

An ETag is an opaque string the server chooses. The only requirement: it must change when the resource changes. There are two flavors:

- **Strong ETag** (`"abc123"`) — promises byte-for-byte identity. Requires hashing the body, which costs CPU on every request.
- **Weak ETag** (`W/"abc123"`) — promises semantic equivalence. The `W/` prefix tells the browser "this resource is still effectively the same."

For static files served from disk, weak ETags from `(file size, file mtime)` are what nginx, IIS, and ASP.NET Core all use. They are cheap, change the moment the file is saved, and almost never collide.

```csharp
static string BuildWeakETag(long size, DateTime mtimeUtc)
{
    return "W/\"" + size.ToString("x") + "-" + mtimeUtc.Ticks.ToString("x") + "\"";
}
```

For database-backed responses, an ETag from `(row id, updated_at)` works the same way:

```csharp
static string BuildRowETag(long id, DateTime updatedAt)
{
    return "W/\"" + id.ToString("x") + "-" + updatedAt.Ticks.ToString("x") + "\"";
}
```

For computed responses where neither file nor row applies, hash the body bytes (MD5 is fine — this is not a security signature, just a fingerprint).

### Generating Last-Modified

`Last-Modified` is the resource's modification time formatted as an HTTP date (RFC 7231 §7.1.1.1). The format string in .NET is `"r"`:

```csharp
DateTime mtimeUtc = File.GetLastWriteTimeUtc(fullPath);
string lastModifiedHeader = mtimeUtc.ToString("r", CultureInfo.InvariantCulture);
// e.g. "Wed, 15 Apr 2026 12:34:56 GMT"
```

**Critical pitfall: sub-second precision.** Filesystems store mtimes with sub-second precision (NTFS goes to ~100ns). HTTP-date format only carries seconds. If you compare a raw filetime against a header value, the comparison will always say "the file is newer than the cached copy," and 304 will never fire.

**Fix:** truncate to whole seconds before generating the header *and* before comparing:

```csharp
DateTime mtimeRounded = new DateTime(
    mtimeUtc.Year, mtimeUtc.Month, mtimeUtc.Day,
    mtimeUtc.Hour, mtimeUtc.Minute, mtimeUtc.Second,
    DateTimeKind.Utc);
```

### Honoring conditional headers — the algorithm

This is what your handler must do for any cacheable response. Pseudocode first:

```
1. Compute the validators (ETag, Last-Modified) for this resource.
2. Read If-None-Match from the request.
   If present and any of its tags match our ETag → return 304.
3. Else, read If-Modified-Since from the request.
   If present and our Last-Modified <= the parsed date → return 304.
4. Else, return 200 with the body and the validators.
```

`If-None-Match` takes precedence over `If-Modified-Since` per RFC 9110 §13.1.3. Honor that order. If you swap them, browsers that send both (most do) will get inconsistent answers.

In code:

```csharp
string ifNoneMatch    = request.Headers["If-None-Match"];
string ifModifiedSince = request.Headers["If-Modified-Since"];

bool notModified = false;

if (!string.IsNullOrEmpty(ifNoneMatch))
{
    notModified = ETagMatches(ifNoneMatch, ourETag);
}
else if (!string.IsNullOrEmpty(ifModifiedSince))
{
    DateTime since;
    if (DateTime.TryParseExact(
            ifModifiedSince, "r",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out since))
    {
        since = since.ToUniversalTime();
        notModified = ourMtimeUtc <= since;
    }
}
```

`ETagMatches` handles three cases — single tag, comma-separated list, and `*` (wildcard meaning "any current representation"):

```csharp
static bool ETagMatches(string headerValue, string ourTag)
{
    foreach (string raw in headerValue.Split(','))
    {
        string p = raw.Trim();
        if (p == "*") return true;
        if (p == ourTag) return true;
    }
    return false;
}
```

### Building the 304 response with cshttp

A 304 response has headers but no body. The validators (and Cache-Control, see Layer 2) should be echoed so the browser knows what cached version is still good and how long it remains valid:

```csharp
byte[] response = new HttpResponse(304)
    .Header("ETag", ourETag)
    .Header("Last-Modified", ourMtimeHttpDate)
    .Header("Cache-Control", "public, max-age=300")
    .Header("Content-Type", "text/css; charset=utf-8")
    .ToBytes();
```

cshttp will not auto-add `Content-Length: 0` because there is no body. That is correct — RFC 9110 §15.4.5 forbids `Content-Length` on 304. Do not set it manually.

### Building the 200 response (full body)

When the validators do not match, send the full body with the same validators (so the browser caches them):

```csharp
byte[] response = new HttpResponse(200)
    .Header("Content-Type", "text/css; charset=utf-8")
    .Header("ETag", ourETag)
    .Header("Last-Modified", ourMtimeHttpDate)
    .Header("Cache-Control", "public, max-age=300")
    .Body(bytes)
    .ToBytes();
```

`Content-Length` is added automatically from the body length.

---

## Layer 2 — Cache-Control

### What it does

`Cache-Control` tells the browser how long it may serve from its own cache without contacting the server at all. During that window, the browser handles the request entirely locally — no network round-trip, no 304 exchange, nothing.

```
Cache-Control: public, max-age=300
```

That tells the browser: this response is cacheable by anyone (`public`), valid for 300 seconds (`max-age=300`). After 300 seconds, the browser will revalidate (which is where Layer 1 takes over).

### Choosing max-age

The right value depends on how often the content changes and how quickly users need to see updates.

| Asset type | Suggested max-age | Rationale |
|------------|-------------------|-----------|
| Fingerprinted assets (`style.a1b2c3d4.css`) | `31536000` (1 year) + `immutable` | URL changes when content changes; cache forever. |
| Non-fingerprinted CSS / JS during development | `60` to `300` | Edits visible within a minute or two. |
| Non-fingerprinted CSS / JS in production | `3600` to `86400` | Hour to a day. Force-refresh always bypasses. |
| Font files (.woff2) | `2592000` (30 days) | Almost never change. |
| HTML pages | `0`, `no-cache`, or omit | Should always revalidate. |
| API JSON responses | `0` or omit | Almost never cacheable by URL alone. |

### Cache-Control directives that matter

| Directive | Meaning |
|-----------|---------|
| `public` | Any cache (browser, CDN, intermediate proxy) may store it. |
| `private` | Only the user's own browser may cache it. Use for per-user content. |
| `no-cache` | Browser must revalidate every time (still uses Layer 1 conditional GET). |
| `no-store` | Browser must not cache at all. Use for sensitive data. |
| `max-age=N` | Cacheable without revalidation for N seconds. |
| `immutable` | Promises the response will never change (only valid with fingerprinted URLs). |

### Common combinations

```csharp
// Short-lived static asset, dev-friendly
.Header("Cache-Control", "public, max-age=300")

// Production CSS / JS without fingerprinting
.Header("Cache-Control", "public, max-age=3600")

// Fingerprinted asset, cache forever
.Header("Cache-Control", "public, max-age=31536000, immutable")

// Per-user content (e.g. dashboard JSON)
.Header("Cache-Control", "private, max-age=0, must-revalidate")

// Sensitive data
.Header("Cache-Control", "no-store")
```

---

## Putting it all together — full static file handler

This is a complete, drop-in static file handler that uses both layers. It expects an application context with `Request` (an `HttpRequestMessage`) and writers for headers, status, body. Adapt to whatever shape your application uses.

```csharp
using CsHttp;
using System.Globalization;
using System.IO;

public static class StaticFileServer
{
    const int CacheMaxAgeSeconds = 300;  // tune per asset type if needed

    public static byte[] Serve(HttpRequestMessage request, string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            return new HttpResponse(404)
                .Header("Content-Type", "text/plain; charset=utf-8")
                .Body("Not Found")
                .ToBytes();
        }

        // Compute validators (truncate mtime to whole seconds — see pitfall above)
        var fi = new FileInfo(fullPath);
        DateTime mtime = TruncateToSeconds(fi.LastWriteTimeUtc);
        string etag    = BuildWeakETag(fi.Length, mtime);
        string mtimeHd = mtime.ToString("r", CultureInfo.InvariantCulture);

        // Conditional GET — short-circuit to 304 if the client's cache is fresh
        if (ClientHasCurrent(request, etag, mtime))
        {
            return new HttpResponse(304)
                .Header("ETag", etag)
                .Header("Last-Modified", mtimeHd)
                .Header("Cache-Control", "public, max-age=" + CacheMaxAgeSeconds)
                .Header("Content-Type", GetContentType(fullPath))
                .ToBytes();
        }

        // Full response
        return new HttpResponse(200)
            .Header("Content-Type", GetContentType(fullPath))
            .Header("ETag", etag)
            .Header("Last-Modified", mtimeHd)
            .Header("Cache-Control", "public, max-age=" + CacheMaxAgeSeconds)
            .Body(File.ReadAllBytes(fullPath))
            .ToBytes();
    }

    static bool ClientHasCurrent(HttpRequestMessage req, string etag, DateTime mtime)
    {
        string ifNoneMatch = req.Headers["If-None-Match"];
        if (!string.IsNullOrEmpty(ifNoneMatch))
            return ETagMatches(ifNoneMatch, etag);

        string ifModifiedSince = req.Headers["If-Modified-Since"];
        if (!string.IsNullOrEmpty(ifModifiedSince))
        {
            DateTime since;
            if (DateTime.TryParseExact(
                    ifModifiedSince, "r",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out since))
            {
                since = since.ToUniversalTime();
                return mtime <= since;
            }
        }
        return false;
    }

    static bool ETagMatches(string headerValue, string ourTag)
    {
        foreach (string raw in headerValue.Split(','))
        {
            string p = raw.Trim();
            if (p == "*" || p == ourTag) return true;
        }
        return false;
    }

    static string BuildWeakETag(long size, DateTime mtimeUtc)
    {
        return "W/\"" + size.ToString("x") + "-" + mtimeUtc.Ticks.ToString("x") + "\"";
    }

    static DateTime TruncateToSeconds(DateTime t)
    {
        return new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second, t.Kind);
    }

    static string GetContentType(string path)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".css":   return "text/css; charset=utf-8";
            case ".js":    return "application/javascript; charset=utf-8";
            case ".html":  return "text/html; charset=utf-8";
            case ".json":  return "application/json; charset=utf-8";
            case ".woff2": return "font/woff2";
            case ".woff":  return "font/woff";
            case ".png":   return "image/png";
            case ".jpg":
            case ".jpeg":  return "image/jpeg";
            case ".svg":   return "image/svg+xml";
            case ".ico":   return "image/x-icon";
            default:       return "application/octet-stream";
        }
    }
}
```

---

## Verifying it works

You can confirm caching is wired up correctly with three quick checks. Use any HTTP client (curl, Postman, PowerShell's `Invoke-WebRequest`); the examples below use PowerShell.

### Check 1 — first request returns validators

```powershell
$r = Invoke-WebRequest http://localhost:8080/css/style.css -UseBasicParsing
$r.Headers['ETag']           # → W/"..."
$r.Headers['Last-Modified']  # → Wed, 15 Apr 2026 12:34:56 GMT
$r.Headers['Cache-Control']  # → public, max-age=300
```

### Check 2 — repeat with If-None-Match returns 304

```powershell
$etag = $r.Headers['ETag']
try {
    Invoke-WebRequest http://localhost:8080/css/style.css `
        -Headers @{ "If-None-Match" = $etag } `
        -UseBasicParsing
} catch {
    [int]$_.Exception.Response.StatusCode  # → 304
}
```

PowerShell's `Invoke-WebRequest` treats 304 as an exception — that is a quirk of the client, not the server. The server is correctly returning 304 with no body.

### Check 3 — repeat with If-Modified-Since returns 304

```powershell
$lm = $r.Headers['Last-Modified']
try {
    Invoke-WebRequest http://localhost:8080/css/style.css `
        -Headers @{ "If-Modified-Since" = $lm } `
        -UseBasicParsing
} catch {
    [int]$_.Exception.Response.StatusCode  # → 304
}
```

In a real browser, opening DevTools → Network and reloading a page should show the static assets as `(memory cache)` or `(disk cache)` for subsequent navigations within the `max-age` window, and `304 Not Modified` after that window expires.

---

## Common mistakes

**Forgetting to truncate mtime to whole seconds.** Sub-second filetime precision will defeat `If-Modified-Since` comparisons every time. Symptom: 304 never fires even though the file has not changed. Always truncate.

**Putting `Content-Length: 0` on a 304.** RFC 9110 forbids it. cshttp will not add it automatically because there is no body, which is correct. Do not set it yourself.

**Using strong ETags from a hash of the whole body.** This is correct but wastes CPU on every request. Weak ETags from `(size, mtime)` are what real servers use for files. Reserve hash-based ETags for content where size and mtime are not available (database BLOBs, computed responses).

**Setting `Cache-Control: no-cache` thinking it disables caching.** It does not. `no-cache` means "always revalidate" — caching still happens, the browser just runs the conditional GET round-trip every time. To disable caching entirely, use `no-store`.

**Returning 304 without echoing `Cache-Control`.** Without it, the browser may treat the cached copy as expired and revalidate again on the very next request. Always echo the same `Cache-Control` on 304 that you would have sent on 200.

**Returning 304 without echoing the validators.** Without `ETag` / `Last-Modified` on the 304, the browser cannot update its cache metadata. The next request might still send the old validators. Always echo them.

**Caching dynamic per-user data publicly.** `Cache-Control: public` on a logged-in user's dashboard JSON will let intermediate caches serve that user's data to other users. Use `private` for anything user-specific.

**Caching error responses.** `404`, `500`, etc. should generally not be cached. If you do not set `Cache-Control` on them, browsers and proxies will not cache them by default — leave it that way unless you have a specific reason.

---

## What cshttp does and does not do

For clarity, here is the exact split of responsibilities:

| Concern | cshttp | Your code |
|---------|--------|-----------|
| Parse `If-None-Match` from incoming request | ✓ (`request.Headers["If-None-Match"]`) | — |
| Parse `If-Modified-Since` from incoming request | ✓ (`request.Headers["If-Modified-Since"]`) | — |
| Decide whether the client's cache is fresh | — | ✓ |
| Generate `ETag` from a resource | — | ✓ |
| Generate `Last-Modified` from a resource | — | ✓ |
| Format a date as HTTP-date (`"r"`) | — | ✓ (use `.ToString("r", CultureInfo.InvariantCulture)`) |
| Build a 304 response | ✓ (`new HttpResponse(304).Header(...).ToBytes()`) | calls cshttp |
| Build a 200 response with validators | ✓ (`new HttpResponse(200).Header(...).Body(...).ToBytes()`) | calls cshttp |
| Set `Cache-Control` | — | ✓ (your policy decision) |
| Omit `Content-Length` on 304 | ✓ (auto, when no body) | — |

cshttp is deliberately a wire-format library — it understands HTTP messages but does not interpret semantics. Caching policy is application logic and lives in your code. This guide gives you the recipe; the implementation is straightforward and fits in ~100 lines once you have the algorithm right.

---

## References

- RFC 9110 — HTTP Semantics — https://www.rfc-editor.org/rfc/rfc9110
  - §13 — Conditional Requests
  - §15.4.5 — 304 Not Modified
  - §5.5 — Field Values (Cache-Control parsing)
- RFC 9111 — HTTP Caching — https://www.rfc-editor.org/rfc/rfc9111
- RFC 7231 §7.1.1.1 — Date/Time Formats (still the canonical reference for the HTTP-date grammar)
- MDN — HTTP caching — https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching
