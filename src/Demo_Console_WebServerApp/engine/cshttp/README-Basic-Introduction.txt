Introducing cshttp: A Standalone HTTP Parser for C#

Where this came from
Imagine you open a browser, visit a page, and see a small form asking for your email — maybe to log in, maybe to subscribe to a newsletter.

https://example.com

The HTML form body:

<form action="/submit?ref=home" method="POST">
  <h2>Please Enter Your Email to Continue</h2>

  <label for="email">Your Email:</label>
  <input type="email" id="email" name="email" required />

  <input type="hidden" name="x" value="1" />

  <button type="submit">Submit</button>
</form>

You type in your email and hit Submit. The browser packages up your input and sends it to the server. On the server side, in normal day-to-day web development, the code that handles that submission usually looks something like this:

string path = Request.Path;
// "/submit"

string query = Request.Url.Query;
// "?ref=home"

string email = Request["email"] + "";
// "user@mail.com"

string emailFromForm = Request.Form["email"] + "";
// "user@mail.com"

string sid = "";
if (Request.Cookies["sid"] != null)
{
    sid = Request.Cookies["sid"].Value;
}
// "abc123"

Clean. Straightforward. You ask for a value by name, and you get it back as a string.

But there is a layer happening underneath this that most developers never see. This is not what the browser actually sends, and it’s not what the web server actually receives. Before any of that nice Request["email"] syntax can work, this is what arrives on the server’s network socket as a stream of raw bytes:


POST /submit?ref=home HTTP/1.1
Host: example.com
Content-Type: application/x-www-form-urlencoded
Content-Length: 25
Cookie: sid=abc123

email=user%40mail.com&x=1

That is the actual HTTP request. Plain text on top, raw bytes underneath. To turn this into the friendly Request["email"] you write in your application code, something has to read those bytes and figure out: where does the first line end? What method is being used? How long is the body? Where do the headers stop and the body begin? What if the body is sent in chunks instead of all at once? What if two values for the same header arrive together? What if some malicious sender tries to sneak a fake request inside another one?

This problem has a name: HTTP parsing. And every web server in the world has to solve it before it can do anything else.

In most languages, there is a small, focused library that does just this one job. You hand it bytes, and it hands you back a clean, structured object — method, headers, body, done.

Language | Library
---------|---------
C        | llhttp (the engine inside Node.js)
Rust     | httparse
Python   | httptools
C#       | (missing)

In C#, that library did not exist.

If you wanted to parse HTTP in .NET, you had two options. You could pull in the entire ASP.NET stack — IIS or IIS Express, Kestrel, middleware, hosting, dependency injection, the whole house — even when all you wanted was the front door. Or you could write your own parser. And writing your own is much harder than it looks. The HTTP specification is long, full of edge cases, and several of those edge cases are how attackers smuggle malicious requests past poorly-written servers.

What it is
cshttp is a standalone HTTP/1.1 parser for C#. You give it the raw bytes that came off a socket, and it gives you back a structured message — method, path, headers, cookies, query string, form fields, uploaded files, all neatly organized.

That is the whole job. It does not run a server. It does not open sockets. It does not need a framework. It is a small set of C# files you drop into your project, and from that point on, parsing HTTP becomes a one-line call.

ParseResult result = HttpParser.ParseRequest(data);
HttpRequestMessage req = result.Request;

string method = req.Method;            // "POST"
string email  = req.Form["email"];     // "user@mail.com"
string sid    = req.Cookies["sid"];    // "abc123"

That is the entire mental model. Bytes go in. A clean object comes out.

What’s in the box
cshttp is built in two layers, and the split matters because it lets you only pay for what you use.

The first layer is the parser itself. It reads the raw bytes and figures out the structure of the HTTP message — the request line, the headers, where the body starts, how long the body is, whether the body arrived all at once or in chunks. This is the work that has to happen before anything else can.

The second layer is a content toolkit. Once the message is parsed, you can ask for things like the query string, the form fields, the uploaded files, or the cookies. These are parsed lazily — meaning, they are not parsed until you actually ask for them. If your application only cares about the method and the headers, the cookie parser never runs. Nothing is wasted.

Building a response is just as simple:

byte[] resp = HttpResponse.Html("<h1>Hello</h1>");
byte[] resp = HttpResponse.Json("{\"ok\":true}");
byte[] resp = HttpResponse.Redirect("/login");

Or with a builder, if you need more control:

var builder = new HttpResponse(200);
builder.Header("Content-Type", "text/html; charset=utf-8");
builder.SetCookie("sid", "abc123", maxAge: 3600, httpOnly: true);
builder.Body("<h1>Hello</h1>");
byte[] resp = builder.ToBytes();

Send those bytes back down the socket, and you have answered the request.

A web server and a web application, in one process
Here is what cshttp quietly opens up: you can build a 2-in-1 web server and web application without an actual web server.

The simplest possible example is a console application that listens directly on port 80. No IIS. No IIS Express. No Kestrel. No hosting layer behind it. Just a TcpListener, cshttp doing the parsing, and your code deciding what to send back. The whole thing fits in one process and runs on its own.

That single idea scales in two directions.

Going small. When the web application is light enough, the entire server-plus-app can be embedded into something tiny — an IoT device, an industrial sensor, a small appliance. You get a real HTTP endpoint without dragging a web server stack onto the device.

Going large. In the other direction, cshttp opens the door to engineering a custom web server from the ground up — without inheriting anything from IIS, Kestrel, or any of the hosting infrastructure built around them. You are also free from any pre-installed middleware or framework patterns. No request pipeline you didn’t design. No conventions you didn’t choose. You invent your own patterns. You shape the architecture from the bytes upward.

For a long time in .NET, that kind of freedom required either heroic effort or accepting the heavy framework. cshttp removes the parsing problem as a barrier, so the rest becomes a design choice rather than an obstacle.

What makes it careful
A parser that just handles the easy cases is dangerous. The hard cases in HTTP are exactly where attacks live. cshttp was built with those attacks in mind from the start.

It detects request smuggling — a class of attack where a sender tries to confuse the server about where one request ends and the next begins. It rejects oversized inputs before they can exhaust memory. It refuses overlong UTF-8 encodings, which are sometimes used to slip past path filters. It never decodes input twice (a classic source of security bugs). It uses no regular expressions on untrusted input, which avoids a whole family of denial-of-service attacks. And every limit — how long a header can be, how many parameters are allowed, how big a file upload can be — is configurable.

When the HTTP specification says something must not happen, cshttp treats it as an error and stops. When the specification says something should not happen, cshttp records it as a warning and lets the application decide. Strict mode and lenient mode are both available depending on how forgiving you want to be.

Who might use it
cshttp is not for everyone. If your application is an ordinary web app, ASP.NET stack is still a perfectly fine tool. cshttp does not aim to replace it.

But there are situations where the full framework is more than you need. Some examples:

Custom servers built on raw TCP sockets, where ASP.NET would be overkill.
Proxies and gateways that need to read a request, look at it, and forward it somewhere else.
IoT and embedded devices running on small .NET runtimes where every megabyte counts.
Webhook receivers — small services that just need to listen for incoming POSTs from third parties.
Testing and analysis tools — mock servers, traffic recorders, protocol inspectors.
Custom web frameworks — for anyone who wants to design their own request pipeline and conventions from scratch.
Learning — for anyone who wants to understand what HTTP actually looks like at the byte level.
For all of these, cshttp gives you proper HTTP parsing without dragging in a whole web framework.

How to get it
cshttp is published on GitHub:

github.com/adriancs2/cshttp

There is no NuGet package. The library is distributed as plain source files — copy the cshttp folder into your project, and that is the installation. It targets C# 7.3 or later, and runs on any compatible runtime (.NET Framework, .NET Core). There are no external dependencies.

The full technical reference, including every API, every configuration option, and every specification covered, lives on the project wiki.

cshttp is built against RFC 9112 (HTTP/1.1), RFC 9110 (HTTP Semantics), RFC 3986 (URIs), RFC 6265 (Cookies), RFC 7578 (Multipart), and the WHATWG URL Standard.

