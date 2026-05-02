# cshttp — Technical Specification

This document is the complete technical reference for cshttp. It covers both the envelope parser (RFC 9112) and the content toolkit (query strings, forms, multipart, cookies, response building), including full API surface, configuration options, specification coverage, and security considerations.

For the project introduction and quick start, see the [README](../README.md).

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Envelope Parser API](#envelope-parser-api)
  - [HttpParser](#cshttpparser)
  - [ParseResult](#parseresult)
  - [HttpRequestMessage](#httprequestmessage)
  - [HttpResponseMessage](#httpresponsemessage)
  - [HttpHeaderCollection](#httpheadercollection)
  - [BodyFrameKind](#bodyframekind)
  - [RequestTargetForm](#requesttargetform)
- [Content Toolkit API](#content-toolkit-api)
  - [Lazy Content Properties](#lazy-content-properties)
  - [HttpContentCollection](#httpcontentcollection)
  - [QueryStringParser](#querystringparser)
  - [FormParser](#formparser)
  - [MultipartParser](#multipartparser)
  - [CookieParser](#cookieparser)
  - [SetCookieParser](#setcookieparser)
  - [PercentDecoder](#percentdecoder)
  - [HttpPostedFile and HttpPostedFileCollection](#httppostedfile-and-httppostedfilecollection)
  - [SetCookie and SetCookieCollection](#setcookie-and-setcookiecollection)
  - [HttpResponse](#cshttpresponse)
- [Configuration](#configuration)
  - [ParserOptions (Envelope)](#parseroptions-envelope)
  - [ContentParserOptions (Content)](#contentparseroptions-content)
  - [Callbacks](#callbacks)
- [Warnings and Errors](#warnings-and-errors)
  - [ParseErrorKind](#parseerrorkind)
  - [ParseWarningKind](#parsewarningkind)
  - [ContentParseErrorKind](#contentparseerrorkind)
  - [DecodeErrorKind](#decodeerrorkind)
- [Security](#security)
- [Specification Coverage](#specification-coverage)
- [Limitations](#limitations)

---

## Architecture Overview

cshttp is organized into two layers across 19 source files, all in the `CsHttp` namespace.

**Layer 1 — Envelope Parser** parses the HTTP/1.1 message structure: start-line, headers, and body framing per RFC 9112. This is the core — 7 files that handle the wire protocol.

**Layer 2 — Content Toolkit** parses the content inside the envelope: query strings, form bodies, multipart uploads, cookies, and Set-Cookie headers per their respective specifications. These are 11 additional files that add content interpretation. A 12th file provides response building.

The two layers are connected through lazy properties on `HttpRequestMessage` and `HttpResponseMessage`. Content parsing is deferred — it costs nothing until first access. A developer who only reads `Method` and `Headers` pays zero for query string or cookie parsing.

```
Layer 1: Envelope (RFC 9112)
  HttpParser.cs ─── ParseRequest() / ParseResponse()
  HttpMessage.cs ──── HttpRequestMessage / HttpResponseMessage
  HttpHeaderCollection.cs ── wire-order, case-insensitive
  ParserOptions.cs ── limits, strict/lenient, callbacks
  ParseResult.cs ──── success / failure / warnings
  ParseError.cs ───── error kinds
  ParseWarning.cs ─── warning kinds

Layer 2: Content
  PercentDecoder.cs ─────── %HH decoding, UTF-8 validation
  QueryStringParser.cs ──── ?key=value&key=value
  FormParser.cs ─────────── application/x-www-form-urlencoded
  MultipartParser.cs ────── multipart/form-data (files + fields)
  CookieParser.cs ────────── Cookie header → name-value pairs
  SetCookieParser.cs ────── Set-Cookie → structured attributes
  HttpContentCollection.cs ── ordered key-value store
  HttpPostedFile.cs ─────── single uploaded file model
  HttpPostedFileCollection.cs ── file collection
  SetCookie.cs ──────────── Set-Cookie model
  ContentParserOptions.cs ── content limits and policies
  HttpResponse.cs ── response byte[] builder
```

---

## Envelope Parser API

### HttpParser

The static entry point. All state lives in the parse context during a single invocation — the class is thread-safe.

```csharp
// Parse a request from the entire buffer
ParseResult result = HttpParser.ParseRequest(byte[] data, ParserOptions options = null);

// Parse a request from a region of a buffer (pipelined messages)
ParseResult result = HttpParser.ParseRequest(byte[] data, int offset, int length, ParserOptions options = null);

// Parse a response (requestMethod needed for body framing rules 1-2)
ParseResult result = HttpParser.ParseResponse(byte[] data, string requestMethod = null, ParserOptions options = null);

// Parse a response from a region of a buffer
ParseResult result = HttpParser.ParseResponse(byte[] data, int offset, int length, string requestMethod = null, ParserOptions options = null);
```

### ParseResult

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether parsing completed successfully |
| `Error` | `ParseError` | The error that caused failure (null on success) |
| `Request` | `HttpRequestMessage` | The parsed request (null if response or failure) |
| `Response` | `HttpResponseMessage` | The parsed response (null if request or failure) |
| `Message` | `HttpMessage` | The parsed message regardless of type (convenience) |
| `IsRequest` | `bool` | True if the parsed message was a request |
| `IsResponse` | `bool` | True if the parsed message was a response |
| `Warnings` | `IReadOnlyList<ParseWarning>` | Tolerated deviations (present even on success) |
| `BytesConsumed` | `int` | Total bytes consumed from input |

`BytesConsumed` is essential for pipelined messages. After parsing one message, the next message starts at `data[result.BytesConsumed]`.

### HttpRequestMessage

Inherits from `HttpMessage`. Contains the parsed request-line components and lazy content properties.

**Envelope properties (populated during parsing):**

| Property | Type | Description |
|----------|------|-------------|
| `Method` | `string` | The request method token, case-sensitive ("GET", "POST", etc.) |
| `RequestTarget` | `string` | The request-target as received, not decoded ("/path?query") |
| `RequestTargetForm` | `RequestTargetForm` | Origin, Absolute, Authority, or Asterisk |
| `Version` | `string` | "HTTP/1.1" or "HTTP/1.0" |
| `VersionMajor` | `int` | Major version number (1) |
| `VersionMinor` | `int` | Minor version number (0 or 1) |
| `Headers` | `HttpHeaderCollection` | The header fields |
| `Body` | `byte[]` | Decoded body content (null if no body) |
| `RawBody` | `byte[]` | Raw body bytes as received on the wire |
| `Trailers` | `HttpHeaderCollection` | Trailer fields after chunked body |
| `BodyFraming` | `BodyFrameKind` | How the body length was determined |
| `RawStartLine` | `byte[]` | Raw start-line bytes for security auditing |
| `BytesConsumed` | `int` | Total bytes consumed from input |

**Content properties (lazy-parsed on first access):**

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string` | Percent-decoded path from RequestTarget |
| `RawQueryString` | `string` | Raw query string (after '?', not decoded) |
| `QueryString` | `HttpContentCollection` | Parsed query parameters (decoded) |
| `Form` | `HttpContentCollection` | Parsed form body parameters (decoded) |
| `Files` | `HttpPostedFileCollection` | Uploaded files from multipart/form-data |
| `Cookies` | `HttpContentCollection` | Parsed cookies from Cookie header |
| `ContentOptions` | `ContentParserOptions` | Options for content parsing (set before first access) |
| `this[key]` | `string` | Combined indexer: QueryString → Form → Cookies → Headers |

### HttpResponseMessage

Inherits from `HttpMessage`. Contains the parsed status-line components.

| Property | Type | Description |
|----------|------|-------------|
| `StatusCode` | `int` | The 3-digit status code (200, 404, etc.) |
| `ReasonPhrase` | `string` | The reason phrase ("OK", "Not Found"). May be empty. |
| `Version` | `string` | "HTTP/1.1" or "HTTP/1.0" |
| `Headers` | `HttpHeaderCollection` | The header fields |
| `Body` | `byte[]` | Decoded body content (null if no body) |
| `RawBody` | `byte[]` | Raw body bytes as received |
| `Trailers` | `HttpHeaderCollection` | Trailer fields after chunked body |
| `BodyFraming` | `BodyFrameKind` | How the body length was determined |
| `SetCookies` | `SetCookieCollection` | Parsed Set-Cookie headers (lazy) |

### HttpHeaderCollection

Preserves wire order. Case-insensitive keyed access. Multiple headers with the same name are stored as separate entries.

| Method | Description |
|--------|-------------|
| `headers["Name"]` | Get combined value (comma-joined if multiple). Returns null if absent. |
| `headers.GetValues("Name")` | Get all individual values as `string[]` |
| `headers.GetHeaders("Name")` | Get all `HttpHeader` entries with raw bytes |
| `headers.Contains("Name")` | Check if a field name exists |
| `headers[index]` | Access by wire-order index (0-based) |
| `headers.Count` | Total number of header lines |
| `headers.GetFieldNames()` | Distinct field names in first-occurrence order |

**Important:** `Set-Cookie` headers must NOT be retrieved via the string indexer (`headers["Set-Cookie"]`), because the indexer comma-joins multiple values per RFC 9110 — but Set-Cookie is an explicit exception to that rule. Use `headers.GetValues("Set-Cookie")` instead, or access the parsed `SetCookies` collection on `HttpResponseMessage`.

### BodyFrameKind

Maps to the 8 rules in RFC 9112 Section 6.3.

| Value | Rule | Description |
|-------|------|-------------|
| `None` | — | No body present |
| `NoBodyByStatus` | 1 | HEAD response or 1xx/204/304 status |
| `Tunnel` | 2 | 2xx response to CONNECT |
| `Chunked` | 4 | Chunked transfer coding |
| `ContentLength` | 6 | Content-Length specifies exact byte count |
| `ZeroByAbsence` | 7 | Request with no TE or CL — zero body |
| `UntilClose` | 8 | Response with no framing — read until close |

### RequestTargetForm

The four forms per RFC 9112 Section 3.2.

| Value | Section | Example |
|-------|---------|---------|
| `Origin` | 3.2.1 | `/path?query` |
| `Absolute` | 3.2.2 | `http://example.com/path` |
| `Authority` | 3.2.3 | `example.com:443` (CONNECT) |
| `Asterisk` | 3.2.4 | `*` (server-wide OPTIONS) |

---

## Content Toolkit API

### Lazy Content Properties

All content properties on `HttpRequestMessage` are lazy-parsed on first access. This means:

- Accessing `req.Method` or `req.Headers` never triggers content parsing.
- The first access to `req.QueryString` triggers query string parsing.
- The first access to `req.Form` or `req.Files` triggers form/multipart parsing.
- The first access to `req.Cookies` triggers cookie parsing.
- Results are cached — subsequent access returns the cached value.

To control content parsing behavior, set `req.ContentOptions` before accessing any lazy property:

```csharp
req.ContentOptions = new ContentParserOptions
{
    MaxQueryParameters = 100,
    RejectNullBytes = true,
    DuplicatePolicy = DuplicateKeyPolicy.FirstWins
};

// Now access triggers parsing with these options
string page = req.QueryString["page"];
```

### HttpContentCollection

An ordered key-value collection used by QueryString, Form, and Cookies. Case-sensitive keys (unlike headers).

| Method | Description |
|--------|-------------|
| `collection["key"]` | Get first value for key. Returns null if absent. |
| `collection.GetValues("key")` | Get all values for key as `string[]` |
| `collection.Contains("key")` | Check if key exists |
| `collection.AllKeys` | All distinct keys in insertion order |
| `collection.Count` | Total number of entries |
| `collection[index]` | Access by insertion-order index |

### QueryStringParser

Parses `?key=value&key=value` from the request target. Algorithm follows the WHATWG URL Standard's `application/x-www-form-urlencoded` parser: split on `&`, split on first `=`, percent-decode with `+` → space.

```csharp
// Usually accessed via the lazy property:
string page = req.QueryString["page"];

// Direct use:
ContentParseResult result = QueryStringParser.Parse("q=hello%20world&page=2");
string raw = QueryStringParser.ExtractFromTarget("/search?q=hello"); // "q=hello"
string path = QueryStringParser.ExtractPath("/search?q=hello");      // "/search"
```

### FormParser

Parses `application/x-www-form-urlencoded` body bytes. Same algorithm as QueryStringParser, different input source and limits.

```csharp
// Usually accessed via the lazy property:
string email = req.Form["email"];

// Direct use:
ContentParseResult result = FormParser.Parse(bodyBytes, contentType);
bool isForm = FormParser.IsFormContentType(contentType);
```

### MultipartParser

Parses `multipart/form-data` body bytes into form fields and uploaded files. Handles boundary extraction, per-part headers (Content-Disposition, Content-Type), and binary file content.

```csharp
// Usually accessed via the lazy properties:
HttpPostedFile file = req.Files["avatar"];
string fieldValue = req.Form["description"]; // text fields from multipart

// Direct use:
MultipartParseResult result = MultipartParser.Parse(bodyBytes, contentType);
string boundary = MultipartParser.ExtractBoundary(contentType);
```

### CookieParser

Parses the `Cookie` request header into name-value pairs per RFC 6265 Section 5.4. Pairs separated by `; `, split on first `=`. Values are NOT percent-decoded (cookies use their own encoding conventions).

```csharp
// Usually accessed via the lazy property:
string sid = req.Cookies["sid"];

// Direct use:
ContentParseResult result = CookieParser.Parse("sid=abc123; theme=dark");
```

### SetCookieParser

Parses `Set-Cookie` response headers into structured `SetCookie` objects with all attributes per RFC 6265 Section 4.1 and RFC 6265bis (SameSite).

```csharp
// Usually accessed via the lazy property:
SetCookie cookie = resp.SetCookies["sid"];

// Direct use:
SetCookie cookie = SetCookieParser.Parse("sid=abc123; Path=/; HttpOnly; SameSite=Lax");
SetCookieCollection all = SetCookieParser.ParseAll(headers);
```

### PercentDecoder

Decodes `%HH` sequences per RFC 3986 Section 2.1, with optional form-mode where `+` means space (WHATWG URL Standard).

```csharp
// Strict — returns DecodeResult with success/failure
DecodeResult result = PercentDecoder.Decode("hello%20world", formMode: true);
if (result.Success) string decoded = result.Value; // "hello world"

// Lenient — returns original string on failure
string decoded = PercentDecoder.DecodeLenient("hello%20world");
```

Two modes:
- `formMode: false` (default) — RFC 3986: `%HH` decoding only, `+` is literal
- `formMode: true` — WHATWG: `%HH` decoding + `+` → space

Security features: overlong UTF-8 rejection, surrogate codepoint rejection, null byte policy, single-pass decoding (never double-decodes).

### HttpPostedFile and HttpPostedFileCollection

```csharp
HttpPostedFile file = req.Files["avatar"];
string name      = file.Name;         // form field name ("avatar")
string fileName  = file.FileName;     // "photo.jpg" (raw — sanitize before filesystem use)
string type      = file.ContentType;  // "image/jpeg"
byte[] bytes     = file.Bytes;        // raw file content
int size         = file.Size;         // byte count

// Multiple files with same field name (<input type="file" multiple>)
HttpPostedFile[] all = req.Files.GetAll("docs");
```

### SetCookie and SetCookieCollection

```csharp
SetCookie cookie = resp.SetCookies["sid"];
string name     = cookie.Name;       // "sid"
string value    = cookie.Value;      // "abc123"
DateTime? exp   = cookie.Expires;    // parsed Expires attribute
int? maxAge     = cookie.MaxAge;     // seconds
string domain   = cookie.Domain;     // ".example.com" (leading dot stripped)
string path     = cookie.Path;       // "/"
bool secure     = cookie.Secure;     // true/false
bool httpOnly   = cookie.HttpOnly;   // true/false
string sameSite = cookie.SameSite;   // "Lax", "Strict", "None"
string raw      = cookie.RawValue;   // original header value
```

### HttpResponse

Builds HTTP/1.1 response byte arrays ready for socket writing. Content-Length is auto-calculated.

**One-liner static methods:**

```csharp
byte[] resp = HttpResponse.Html("<h1>Hello</h1>");
byte[] resp = HttpResponse.Json("{\"ok\":true}");
byte[] resp = HttpResponse.Text("plain text");
byte[] resp = HttpResponse.Bytes(pdfBytes, "application/pdf");
byte[] resp = HttpResponse.Status(204);
byte[] resp = HttpResponse.Status(404);
byte[] resp = HttpResponse.Redirect("/login");             // 302
byte[] resp = HttpResponse.RedirectPermanent("/new-path"); // 301
byte[] resp = HttpResponse.File(pdfBytes, "report.pdf", "application/pdf");
```

**Builder pattern:**

```csharp
var builder = new HttpResponse(200);
builder.Header("Content-Type", "text/html; charset=utf-8");
builder.Header("X-Custom", "value");
builder.SetCookie("sid", "abc123",
    maxAge: 3600,
    httpOnly: true,
    secure: true,
    sameSite: "Lax",
    path: "/");
builder.Body("<h1>Hello</h1>");
byte[] resp = builder.ToBytes();
```

---

## Configuration

### ParserOptions (Envelope)

Controls the envelope parser. All size limits are in bytes.

| Option | Default | Description |
|--------|---------|-------------|
| `MaxStartLineLength` | 8192 | Maximum request-line or status-line length. RFC 9112 recommends at least 8000. |
| `MaxHeaderLineLength` | 8192 | Maximum length of a single header field line |
| `MaxHeaderCount` | 100 | Maximum number of header field lines |
| `MaxHeaderSectionSize` | 65536 | Maximum total header section size (64 KB) |
| `MaxBodySize` | -1 (unlimited) | Maximum message body size |
| `MaxChunkSize` | 0x7FFFFFFF | Maximum chunk size in chunked encoding |
| `RejectObsoleteLineFolding` | true | Reject obs-fold in headers (Section 5.2) |
| `RejectConflictingFraming` | true | Reject messages with both TE and CL (Section 6.3 rule 3) |
| `StrictMode` | false | Reject all deviations from strict RFC compliance |

**Presets:**

```csharp
var opts = new ParserOptions();        // Default: lenient, production-safe limits
var opts = ParserOptions.Strict;       // Reject everything the RFC says SHOULD be rejected
var opts = ParserOptions.Default;      // Shared instance — do not modify
```

### ContentParserOptions (Content)

Controls the content toolkit parsers. Separate from `ParserOptions`.

| Option | Default | Description |
|--------|---------|-------------|
| `MaxQueryStringLength` | 8192 | Maximum query string length |
| `MaxQueryParameters` | 1000 | Maximum query parameter count |
| `MaxQueryParameterValueLength` | 8192 | Maximum single parameter value length |
| `MaxFormBodySize` | 4 MB | Maximum form-encoded body size |
| `MaxFormParameters` | 1000 | Maximum form parameter count |
| `MaxFormParameterValueLength` | 4 MB | Maximum single form value length |
| `MaxMultipartParts` | 100 | Maximum multipart part count |
| `MaxMultipartPartSize` | 50 MB | Maximum single part body size |
| `MaxMultipartBoundaryLength` | 70 | Maximum boundary length (RFC 2046 limit) |
| `MaxMultipartHeaderSize` | 8192 | Maximum per-part header size |
| `MaxCookieCount` | 50 | Maximum cookie count |
| `MaxCookieHeaderSize` | 8192 | Maximum Cookie header size |
| `RejectOverlongUtf8` | true | Reject overlong UTF-8 in percent-decoded output |
| `RejectNullBytes` | false | Reject %00 null bytes (false = preserve as \0) |
| `DecodeOnce` | true (readonly) | Always decodes exactly once — never double-decodes |
| `DuplicatePolicy` | PreserveAll | How duplicate keys are handled |

**DuplicateKeyPolicy:**

| Value | Behavior |
|-------|----------|
| `PreserveAll` | All values stored. Indexer returns first. `GetValues()` returns all. |
| `FirstWins` | Only first occurrence stored. Duplicates silently discarded. |
| `LastWins` | Last occurrence overwrites. Matches IIS/ASP.NET Web Forms behavior. |

**Presets:**

```csharp
var opts = new ContentParserOptions();       // Default: permissive limits
var opts = ContentParserOptions.Strict;      // Tighter limits for security-sensitive apps
var opts = ContentParserOptions.Default;     // Shared instance — do not modify
```

### Callbacks

Four hooks for integration during envelope parsing. Each returns `bool` — returning `false` aborts parsing with `ParseErrorKind.RejectedByCallback`.

**OnStartLineParsed** — called after the request-line or status-line is parsed.

```csharp
var opts = new ParserOptions
{
    OnStartLineParsed = (method, target, version) =>
    {
        return method == "GET" || method == "POST";
    }
};
```

For requests: parameters are (method, requestTarget, version).
For responses: parameters are (statusCode as string, reasonPhrase, version).

**OnHeaderParsed** — called after each header field line.

```csharp
var opts = new ParserOptions
{
    OnHeaderParsed = (name, value, index) =>
    {
        if (name.Equals("Cookie", StringComparison.OrdinalIgnoreCase) && value.Length > 4096)
            return false;
        return true;
    }
};
```

**OnChunkHeader** — called when a chunk header is read during chunked decoding. Parameters: (chunkSize, totalBytesReadSoFar).

**OnBodyProgress** — called during body reading. Parameters: (bytesReadInThisBatch, totalBytesReadSoFar).

---

## Warnings and Errors

cshttp uses a two-tier reporting system. Errors are fatal — parsing stops. Warnings are non-fatal — parsing continues and the deviation is recorded.

### ParseErrorKind

| Kind | Section | Description |
|------|---------|-------------|
| `EmptyInput` | — | Input is null or empty |
| `InvalidStartLine` | 3, 4 | Start-line doesn't match grammar |
| `InvalidMethod` | 3.1 | Method token is empty or invalid |
| `InvalidRequestTarget` | 3.2 | Request-target is empty or invalid |
| `InvalidVersion` | 2.3 | HTTP-version is malformed |
| `InvalidStatusCode` | 4 | Status-code is not a 3-digit integer |
| `WhitespaceBeforeColon` | 5.1 | Space between field-name and colon |
| `InvalidHeaderLine` | 5 | Header line is malformed |
| `ObsoleteLineFolding` | 5.2 | Obs-fold detected and rejected |
| `BareCR` | 2.2 | Bare CR in protocol element |
| `ConflictingFraming` | 6.3 r3 | Both TE and CL present |
| `InvalidContentLength` | 6.2 | CL value is not valid decimal |
| `InvalidTransferEncoding` | 6.3 r4 | TE not understood or chunked not final |
| `InvalidChunkedEncoding` | 7.1 | Chunked encoding is malformed |
| `ChunkSizeOverflow` | 7.1 | Chunk size overflows integer range |
| `StartLineTooLong` | — | Exceeds MaxStartLineLength |
| `HeaderLineTooLong` | — | Exceeds MaxHeaderLineLength |
| `TooManyHeaders` | — | Exceeds MaxHeaderCount |
| `HeaderSectionTooLarge` | — | Exceeds MaxHeaderSectionSize |
| `BodyTooLarge` | — | Exceeds MaxBodySize |
| `ChunkTooLarge` | — | Exceeds MaxChunkSize |
| `RejectedByCallback` | — | A callback returned false |
| `Incomplete` | 8 | Data ended before message was complete |
| `WhitespaceAfterStartLine` | 2.2 | Whitespace between start-line and first header |

### ParseWarningKind

| Kind | Section | Description |
|------|---------|-------------|
| `LFWithoutCR` | 2.2 | Bare LF accepted as line terminator |
| `LeadingEmptyLines` | 2.2 | Empty lines preceded the start-line |
| `ObsLineFoldingReplaced` | 5.2 | Obs-fold replaced with SP |
| `BareCRReplaced` | 2.2 | Bare CR replaced with SP |
| `WhitespaceAfterStartLineConsumed` | 2.2 | Whitespace lines consumed and discarded |
| `EmptyReasonPhrase` | 4 | Status-line has no SP after status-code |
| `LenientStartLineWhitespace` | 3, 4 | Lenient whitespace in start-line |
| `DuplicateContentLengthCollapsed` | 6.3 r5 | Identical CL values collapsed |
| `ChunkExtensionIgnored` | 7.1.1 | Unrecognized chunk extension ignored |

### ContentParseErrorKind

| Kind | Description |
|------|-------------|
| `InputTooLarge` | Input exceeds configured size limit |
| `TooManyParameters` | Parameter count exceeds maximum |
| `ValueTooLarge` | Parameter value exceeds maximum length |
| `DecodingFailed` | Percent-decoding failed |
| `ContentTypeMismatch` | Content-Type doesn't match expected type |
| `InvalidBoundary` | Multipart boundary is missing/empty/too long |
| `MalformedPart` | Multipart part is malformed |
| `TooManyParts` | Multipart part count exceeds maximum |
| `PartTooLarge` | Multipart part body exceeds maximum size |

### DecodeErrorKind

| Kind | Description |
|------|-------------|
| `NullByteRejected` | %00 null byte rejected by configuration |
| `OverlongUtf8` | Overlong UTF-8 encoding detected |
| `InvalidUtf8` | Decoded bytes are not valid UTF-8 |

---

## Security

### Envelope Layer

| Attack | Defense |
|--------|---------|
| Request smuggling (TE + CL conflict) | Detected and rejected (configurable) |
| Oversized start-line | `MaxStartLineLength` limit (default 8192) |
| Header flooding | `MaxHeaderCount` limit (default 100) |
| Oversized header section | `MaxHeaderSectionSize` limit (default 64 KB) |
| Oversized body | `MaxBodySize` limit (configurable) |
| Malicious chunk sizes | `MaxChunkSize` limit, hex validation, overflow detection |
| Bare CR injection | Rejected in names; configurable in values |
| Obs-fold attacks | Rejected by default |
| Invalid Content-Length | Non-decimal rejected; conflicting duplicates rejected |

### Content Layer

| Attack | Defense |
|--------|---------|
| Parameter pollution | Configurable duplicate key policy |
| HashDoS (crafted key collisions) | Configurable max parameter count |
| Null byte injection | `RejectNullBytes` option |
| Overlong UTF-8 (path filter bypass) | `RejectOverlongUtf8` (default true) |
| Double encoding | `DecodeOnce` — always true, never auto-decodes twice |
| Filename injection (path traversal) | Filenames reported raw; application sanitizes |
| Content-Type mismatch | Declared type reported; application validates content |
| Boundary manipulation | Boundary validated from Content-Type; length limited |
| Cookie bomb | `MaxCookieCount` and `MaxCookieHeaderSize` limits |
| ReDoS | No regex on untrusted input — all byte/char-level scanning |

---

## Specification Coverage

### RFC 9112 — HTTP/1.1 Message Syntax

| Section | Feature | Implementation |
|---------|---------|---------------|
| 2.1 | Message format (start-line, headers, body) | Full parsing pipeline |
| 2.2 | Leading empty lines tolerated | `SkipLeadingEmptyLines` with warning |
| 2.2 | Bare LF as line terminator | Tolerated in lenient mode with warning |
| 2.2 | Bare CR detection | Rejected in names; configurable in values |
| 2.2 | Whitespace after start-line | Rejected strict; consumed lenient |
| 2.3 | HTTP-version validation | Case-sensitive, exact format enforced |
| 3 | Request-line parsing | method SP request-target SP HTTP-version |
| 3.2 | Request-target forms | Origin, Absolute, Authority, Asterisk |
| 4 | Status-line parsing | HTTP-version SP status-code SP reason-phrase |
| 5 | Header field syntax | field-name ":" OWS field-value OWS |
| 5.1 | No whitespace before colon | Enforced as error |
| 5.2 | Obsolete line folding | Rejected by default; replaceable with SP |
| 6.2 | Content-Length framing | Exact byte-count body reading |
| 6.3 Rule 1 | HEAD/1xx/204/304 no body | `BodyFrameKind.NoBodyByStatus` |
| 6.3 Rule 2 | CONNECT 2xx tunnel | `BodyFrameKind.Tunnel` |
| 6.3 Rule 3 | TE + CL conflict | Rejected or TE overrides CL |
| 6.3 Rule 4 | Chunked transfer coding | Full chunk parsing with trailers |
| 6.3 Rule 5 | Duplicate Content-Length | Identical values collapsed; conflicts rejected |
| 6.3 Rule 7 | No TE, no CL on request | Zero body by absence |
| 6.3 Rule 8 | No framing on response | Read until close |
| 7.1 | Chunked encoding | Hex size, chunk data, CRLF, terminal chunk |
| 7.1.1 | Chunk extensions | Ignored with warning |
| 7.1.2 | Trailer headers | Parsed into `Trailers` collection |

### Additional Specifications

| Specification | Feature | Implementation |
|---------------|---------|---------------|
| WHATWG URL Standard | Query string and form body parsing | `QueryStringParser`, `FormParser` |
| RFC 3986 | URI structure, percent-encoding | `PercentDecoder`, path extraction |
| RFC 7578 | multipart/form-data | `MultipartParser` |
| RFC 2046 | MIME multipart boundary syntax | Boundary parsing in `MultipartParser` |
| RFC 6265 §5.4 | Cookie header parsing | `CookieParser` |
| RFC 6265 §4.1 | Set-Cookie header parsing | `SetCookieParser` |
| RFC 6265bis | SameSite attribute | `SetCookie.SameSite` |
| RFC 9110 §15 | Status codes and reason phrases | `HttpResponse.GetDefaultReasonPhrase` |
| RFC 6266 | Content-Disposition in HTTP | `HttpResponse.File()` |

---

## Limitations

cshttp is a message parser and content toolkit. It is deliberately scoped. The following are outside its scope:

- **Connection management** — cshttp performs no I/O. It does not open sockets, manage connections, or handle keep-alive. You supply bytes; it parses them.
- **TLS/SSL** — encryption is a transport concern. cshttp operates on decrypted bytes.
- **HTTP/2 and HTTP/3** — these protocols use binary framing, not the text-based format of HTTP/1.1.
- **Content decompression** — `Content-Encoding: gzip` applies to the representation, not the message framing. cshttp decodes `Transfer-Encoding: chunked` but does not decompress gzip/deflate body content.
- **Streaming / incremental parsing** — the current implementation requires the complete message in a single `byte[]` buffer. Incremental parsing across multiple reads is a potential future enhancement.
- **Content validation** — cshttp reports what it received. It does not validate that an uploaded file's actual content matches its declared Content-Type, or that a filename is safe for filesystem use. Content-level validation is the application's responsibility.

These are deliberate scope boundaries. cshttp parses the HTTP/1.1 message envelope and interprets its content — completely, correctly, and securely.

---

*cshttp is built against [RFC 9112](https://www.rfc-editor.org/rfc/rfc9112), [RFC 9110](https://www.rfc-editor.org/rfc/rfc9110), [RFC 3986](https://www.rfc-editor.org/rfc/rfc3986), [RFC 6265](https://www.rfc-editor.org/rfc/rfc6265), [RFC 7578](https://www.rfc-editor.org/rfc/rfc7578), [RFC 2046](https://www.rfc-editor.org/rfc/rfc2046), and the [WHATWG URL Standard](https://url.spec.whatwg.org/).*

*License: Public Domain. No rights reserved.*