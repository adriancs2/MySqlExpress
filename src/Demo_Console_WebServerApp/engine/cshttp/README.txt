# cshttp

**A standalone HTTP/1.1 parser and content toolkit for C#.**
**No framework. No server. No dependencies. Just bytes in, structured message out.**

---

## The Gap

Every major language has a standalone HTTP parser. C has [llhttp](https://github.com/nicholascc/llhttp) (the engine inside Node.js). Rust has [httparse](https://github.com/seanmonstar/httparse). Python has [httptools](https://github.com/MagicStack/httptools). These are small, focused libraries — you give them bytes, they give you back structured data. No framework required.

C# has no equivalent.

If you need to parse HTTP in .NET today, you either adopt the entire ASP.NET stack (Web Forms, MVC, .NET Core, IIS, Kestrel, middleware pipeline, hosting abstractions, dependency injection), or you write a hand-rolled parser that handles the happy path and silently breaks on chunked encoding, request smuggling, bare CR injection, obs-fold headers, and the other edge cases that RFC 9112 defines precisely and most hand-written parsers ignore.

cshttp fills that gap. It is a complete HTTP/1.1 message parser and content toolkit, implemented as plain C# source files you can drop into any project.

---

## Quick Start

### Parse an HTTP Request

```csharp
byte[] data = Encoding.ASCII.GetBytes(
    "POST /submit?ref=home HTTP/1.1\r\n" +
    "Host: example.com\r\n" +
    "Content-Type: application/x-www-form-urlencoded\r\n" +
    "Content-Length: 25\r\n" +
    "Cookie: sid=abc123\r\n" +
    "\r\n" +
    "email=user%40mail.com&x=1");

ParseResult result = HttpParser.ParseRequest(data);
HttpRequestMessage req = result.Request;

// --- Envelope (RFC 9112) ---
string method  = req.Method;            // "POST"
string target  = req.RequestTarget;     // "/submit?ref=home"
string version = req.Version;           // "HTTP/1.1"
string host    = req.Headers["Host"];   // "example.com"

// --- Content (lazy-parsed on first access) ---
string path    = req.Path;              // "/submit"
string refVal  = req.QueryString["ref"];// "home"
string email   = req.Form["email"];     // "user@mail.com" (decoded)
string sid     = req.Cookies["sid"];    // "abc123"

// --- Combined shortcut ---
string val     = req["email"];          // searches QueryString → Form → Cookies → Headers
```

### Parse an HTTP Response

```csharp
byte[] data = Encoding.ASCII.GetBytes(
    "HTTP/1.1 200 OK\r\n" +
    "Content-Length: 13\r\n" +
    "Set-Cookie: sid=xyz; Path=/; HttpOnly; SameSite=Lax\r\n" +
    "\r\n" +
    "Hello, World!");

ParseResult result = HttpParser.ParseResponse(data, "GET");
HttpResponseMessage resp = result.Response;

int code       = resp.StatusCode;               // 200
string body    = Encoding.UTF8.GetString(resp.Body); // "Hello, World!"

// --- Set-Cookie parsing ---
SetCookie cookie = resp.SetCookies["sid"];
string value     = cookie.Value;                // "xyz"
bool httpOnly    = cookie.HttpOnly;             // true
string sameSite  = cookie.SameSite;             // "Lax"
```

### Build an HTTP Response

```csharp
// One-liners
byte[] resp = HttpResponse.Html("<h1>Hello</h1>");
byte[] resp = HttpResponse.Json("{\"ok\":true}");
byte[] resp = HttpResponse.Redirect("/login");
byte[] resp = HttpResponse.Status(204);

// Full builder
var builder = new HttpResponse(200);
builder.Header("Content-Type", "text/html; charset=utf-8");
builder.SetCookie("sid", "abc123", maxAge: 3600, httpOnly: true, secure: true, sameSite: "Lax");
builder.Body("<h1>Hello</h1>");
byte[] bytes = builder.ToBytes();
```

### Handle Errors and Warnings

```csharp
ParseResult result = HttpParser.ParseRequest(data);

if (!result.Success)
{
    // Fatal error — parsing stopped
    Console.WriteLine(result.Error.Kind);     // ParseErrorKind.ConflictingFraming
    Console.WriteLine(result.Error.Position); // byte offset where it was detected
    Console.WriteLine(result.Error.Message);  // human-readable detail
}

// Non-fatal deviations — always available, even on success
foreach (var warn in result.Warnings)
{
    Console.WriteLine(warn.Kind);     // ParseWarningKind.LeadingEmptyLines
    Console.WriteLine(warn.Message);  // "Empty lines preceded the start-line."
}
```

When the RFC says **MUST**, cshttp enforces it as an error. When the RFC says **SHOULD**, cshttp enforces it in strict mode and tolerates it in lenient mode — but always records it as a warning.

---

## What's Inside

cshttp has two layers. The **envelope parser** handles the HTTP message structure per RFC 9112. The **content toolkit** handles everything inside — query strings, form bodies, file uploads, cookies — per their respective specifications. Both layers share the same design: zero dependencies, byte-level parsing, no regex on untrusted input.

### Layer 1: Envelope Parser (RFC 9112)

The core parser reads raw bytes and produces structured `HttpRequestMessage` or `HttpResponseMessage` objects. It implements every section of RFC 9112 that applies to message parsing:

- Request-line and status-line parsing with all four request-target forms (origin, absolute, authority, asterisk)
- Header field parsing with wire-order preservation and case-insensitive access
- All 8 body framing rules from Section 6.3, including chunked transfer encoding with trailers
- Request smuggling detection (conflicting `Transfer-Encoding` and `Content-Length`)
- Bare CR detection, obsolete line folding handling, leading empty line tolerance
- Configurable strict/lenient modes, size limits, and four callback hooks
- Pipeline support via `BytesConsumed` for parsing multiple messages from a single buffer

### Layer 2: Content Toolkit

Content properties are lazy-parsed — they cost nothing until first access. A developer who only reads `Method` and `Headers` pays zero for content parsing.

| Feature | Access | Specification |
|---------|--------|---------------|
| URL path (decoded) | `req.Path` | RFC 3986 |
| Query string parameters | `req.QueryString["key"]` | WHATWG URL Standard |
| Form body parameters | `req.Form["key"]` | WHATWG URL Standard |
| File uploads | `req.Files["field"]` | RFC 7578, RFC 2046 |
| Request cookies | `req.Cookies["name"]` | RFC 6265 Section 5.4 |
| Response Set-Cookie | `resp.SetCookies["name"]` | RFC 6265 Section 4.1, 6265bis |
| Percent-decoding | `PercentDecoder.Decode(s)` | RFC 3986 Section 2.1 |
| Response building | `HttpResponse.Html(s)` | RFC 9112 Section 4, RFC 9110 |
| Combined lookup | `req["key"]` | QueryString → Form → Cookies → Headers |

### Security

Both layers enforce configurable limits to defend against common attacks:

- **Request smuggling** — conflicting TE/CL detected and rejected (configurable)
- **Parameter pollution** — configurable max parameter count with duplicate key policy (PreserveAll, FirstWins, LastWins)
- **Oversized input** — limits on start-line, header count, header size, body size, chunk size, query length, form size, multipart parts
- **Encoding attacks** — overlong UTF-8 rejection, null byte policy, single-pass decoding (no double-decode)
- **Malformed chunked encoding** — hex validation, CRLF enforcement, overflow detection
- **Bare CR injection** — rejected in header names; configurable in values
- **Filename injection** — multipart filenames reported raw; application sanitizes before filesystem use
- **No regex on untrusted input** — all parsing is character-by-character or byte-by-byte

---

## Installation

cshttp is distributed as source files. Copy the `cshttp` folder from `src/cshttp/` into your project. All 19 files use the `CsHttp` namespace.

| File | Purpose |
|------|---------|
| `HttpParser.cs` | Core parser — start-line, headers, body framing |
| `HttpMessage.cs` | Message models with lazy content properties |
| `HttpHeaderCollection.cs` | Wire-order headers, case-insensitive access |
| `ParserOptions.cs` | Envelope parser configuration and callbacks |
| `ParseResult.cs` | Parse result with success/failure/warnings |
| `ParseError.cs` | Error kinds and error model |
| `ParseWarning.cs` | Warning kinds and warning model |
| `ContentParserOptions.cs` | Content parser limits and policies |
| `PercentDecoder.cs` | %HH decoding with UTF-8 validation |
| `QueryStringParser.cs` | URL query string parsing |
| `FormParser.cs` | application/x-www-form-urlencoded parsing |
| `MultipartParser.cs` | multipart/form-data with file upload support |
| `CookieParser.cs` | Cookie request header parsing |
| `SetCookieParser.cs` | Set-Cookie response header parsing |
| `HttpContentCollection.cs` | Key-value collection for content parameters |
| `HttpPostedFile.cs` | Uploaded file model |
| `HttpPostedFileCollection.cs` | Uploaded file collection |
| `SetCookie.cs` | Set-Cookie model with all attributes |
| `HttpResponse.cs` | HTTP response byte array builder |

No NuGet packages. No framework references beyond the base class library.

**Requirements:** C# 7.3 or later. .NET Framework 4.8, .NET 6.0, or any compatible runtime.

**Build:**

```bash
dotnet build
dotnet run
```

---

## Who This Is For

**Custom servers** — build a web server from `TcpListener` and raw sockets with production-grade HTTP parsing.

**Reverse proxies and gateways** — read requests from one socket, inspect them, route to another.

**IoT and embedded devices** — HTTP endpoints on resource-constrained .NET runtimes where ASP.NET Core is too heavy.

**Testing and analysis** — mock servers, traffic recorders, protocol analyzers, packet capture tools.

**Webhook receivers** — lightweight console applications that listen for POST requests from external services.

**Learning** — understand HTTP at the byte level with a parser that shows exactly what the specification requires, decision by decision.

cshttp does not replace ASP.NET Core. It fills a gap that makes ASP.NET Core *optional* — for the scenarios where the full framework was always more than you needed.

---

## Documentation

For the complete technical specification — full API reference, configuration options, specification coverage tables, content parser details, and security defense documentation — see the **[Technical Specification (Wiki)](https://github.com/adriancs2/cshttp/wiki)**.

---

## License

**Public Domain.** No rights reserved. No conditions. No restrictions. Use it for any purpose, without attribution, without permission.

---

*cshttp is built against [RFC 9112: HTTP/1.1](https://www.rfc-editor.org/rfc/rfc9112), [RFC 9110: HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110), [RFC 3986: URI](https://www.rfc-editor.org/rfc/rfc3986), [RFC 6265: Cookies](https://www.rfc-editor.org/rfc/rfc6265), [RFC 7578: Multipart](https://www.rfc-editor.org/rfc/rfc7578), and the [WHATWG URL Standard](https://url.spec.whatwg.org/).*