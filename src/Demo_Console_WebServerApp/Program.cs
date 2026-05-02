using CsHttp;
using System.handlers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace System
{
    /// <summary>
    /// Entry point and dispatcher.
    ///
    /// Pageless Architecture, console edition:
    ///
    ///   - <see cref="TcpListener"/> answers the socket.
    ///   - <see cref="HttpParser.ParseRequest"/> turns bytes into a structured request.
    ///   - The big switch below maps the request path to a handler.
    ///   - Each handler fills the response buffer on <see cref="Ctx"/>.
    ///   - <see cref="Ctx.BuildResponseBytes"/> turns it back into bytes.
    ///   - The bytes go down the same socket they came in on.
    ///
    /// No IIS. No Kestrel. No middleware pipeline. Just a switch statement
    /// and the http parser. The switch statement is the routing table.
    /// </summary>
    public static class Program
    {
        const int Port = 8080;

        public static void Main()
        {
            // Load persisted MySQL connection string (if setup ran previously).
            // This is the console-edition equivalent of Application_Start.
            Config.Load();

            var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            string url = "http://localhost:" + Port + "/";
            Console.WriteLine("===============================================");
            Console.WriteLine(" Demo_Console_WebServerApp");
            Console.WriteLine(" Pageless ASP.NET, rebuilt as a console app.");
            Console.WriteLine("===============================================");
            Console.WriteLine(" Listening on " + url);
            Console.WriteLine(" Press Ctrl+C to stop.");
            Console.WriteLine();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                // Spin each connection onto the threadpool. The handlers
                // here are blocking I/O; this is the simplest possible
                // concurrency model that still serves multiple requests.
                ThreadPool.QueueUserWorkItem(_ => HandleConnection(client));
            }
        }

        // ───────────────────────────────────────────────────────────────
        // Per-connection pipeline
        // ───────────────────────────────────────────────────────────────

        static void HandleConnection(TcpClient client)
        {
            try
            {
                client.ReceiveTimeout = 15000;
                client.SendTimeout = 15000;

                using (NetworkStream ns = client.GetStream())
                {
                    byte[] requestBytes = ReadFullRequest(ns);
                    if (requestBytes == null || requestBytes.Length == 0)
                        return;

                    ParseResult parseResult = HttpParser.ParseRequest(requestBytes);

                    byte[] responseBytes;
                    if (!parseResult.Success)
                    {
                        responseBytes = HttpResponse.Status(400);
                        Console.WriteLine("[400] Parse error: " + parseResult.Error?.Message);
                    }
                    else
                    {
                        responseBytes = Dispatch(parseResult.Request);
                    }

                    ns.Write(responseBytes, 0, responseBytes.Length);
                    ns.Flush();
                }
            }
            catch (Exception ex)
            {
                // Don't crash the server on a single misbehaving connection.
                Console.WriteLine("[error] " + ex.Message);
            }
            finally
            {
                try { client.Close(); } catch { }
            }
        }

        /// <summary>
        /// Read the full HTTP request from the socket: headers first
        /// (delimited by \r\n\r\n), then exactly Content-Length bytes
        /// of body if present.
        ///
        /// This is a deliberately simple reader — it covers the
        /// Content-Length-framed and no-body cases, which is all the demo
        /// needs. Chunked request bodies (rare for browser POSTs) and
        /// pipelined requests are out of scope. cshttp handles chunked
        /// just fine on the parse side; we just don't wire that path
        /// into the reader here.
        /// </summary>
        static byte[] ReadFullRequest(NetworkStream ns)
        {
            const int MaxHeaderSize = 64 * 1024;     // 64 KB headers — generous
            const int MaxBodySize   = 16 * 1024 * 1024; // 16 MB body cap

            byte[] buffer = new byte[8192];
            using (var ms = new MemoryStream())
            {
                int headerEnd = -1;

                // Phase 1 — read until we see \r\n\r\n.
                while (true)
                {
                    int n = ns.Read(buffer, 0, buffer.Length);
                    if (n <= 0) return null; // connection closed before headers
                    ms.Write(buffer, 0, n);

                    if (ms.Length > MaxHeaderSize) return null;

                    headerEnd = FindHeaderEnd(ms.GetBuffer(), (int)ms.Length);
                    if (headerEnd >= 0) break;
                }

                int headerLen = headerEnd + 4;
                int alreadyReadOfBody = (int)ms.Length - headerLen;

                // Phase 2 — figure out body length from headers we just read.
                int contentLength = ReadContentLength(ms.GetBuffer(), headerLen);
                if (contentLength > MaxBodySize) return null;

                // Phase 3 — read the rest of the body, if any.
                int remaining = contentLength - alreadyReadOfBody;
                while (remaining > 0)
                {
                    int n = ns.Read(buffer, 0, Math.Min(buffer.Length, remaining));
                    if (n <= 0) break; // truncated, but give the parser what we have
                    ms.Write(buffer, 0, n);
                    remaining -= n;
                }

                return ms.ToArray();
            }
        }

        /// <summary>Locate the \r\n\r\n that ends the header block.</summary>
        static int FindHeaderEnd(byte[] buf, int length)
        {
            for (int i = 0; i + 3 < length; i++)
            {
                if (buf[i] == '\r' && buf[i + 1] == '\n' &&
                    buf[i + 2] == '\r' && buf[i + 3] == '\n')
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Quick, header-only Content-Length read. Just enough to know
        /// how many body bytes to pull off the socket — cshttp will do
        /// the rigorous parse later.
        /// </summary>
        static int ReadContentLength(byte[] buf, int headerLen)
        {
            string headers = Encoding.ASCII.GetString(buf, 0, headerLen);
            string[] lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                int colon = line.IndexOf(':');
                if (colon <= 0) continue;
                string name = line.Substring(0, colon).Trim();
                if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    string val = line.Substring(colon + 1).Trim();
                    int len;
                    if (int.TryParse(val, out len) && len >= 0) return len;
                    return 0;
                }
            }
            return 0;
        }

        // ───────────────────────────────────────────────────────────────
        // Routing — the switch statement IS the routing table.
        //
        // This is the direct port of Global.asax's Application_BeginRequest
        // dispatcher. Add a route by adding a case and a handler call.
        // ───────────────────────────────────────────────────────────────

        static byte[] Dispatch(HttpRequestMessage req)
        {
            string path = (req.Path ?? "").ToLowerInvariant().Trim().TrimEnd('/');
            if (path.Length == 0) path = "/";

            var ctx = new Ctx(req, path);

            // Brief access log — nothing fancy, but it makes the demo feel
            // real when you're poking at it from a browser.
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {req.Method} {req.RequestTarget}");

            // Static assets first — IIS used to handle these for us.
            if (StaticFiles.TryServe(ctx))
                return ctx.BuildResponseBytes();

            // ------------------------------------------------------------
            // Parameterized: /players/edit/{id} and /teams/edit/{id}
            // ------------------------------------------------------------
            if (path.StartsWith("/players/edit/"))
            {
                PlayerEdit.HandleRequest(ctx);
                return ctx.BuildResponseBytes();
            }
            if (path.StartsWith("/teams/edit/"))
            {
                TeamEdit.HandleRequest(ctx);
                return ctx.BuildResponseBytes();
            }

            switch (path)
            {
                // ---- Setup / Dashboard ----
                case "/":
                case "/home":
                    Home.HandleRequest(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/setup/save-conn":
                    SetupApi.SaveConnString(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/setup/test-conn":
                    SetupApi.TestConnString(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/setup/create-tables":
                    SetupApi.CreateTables(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/setup/seed":
                    SetupApi.SeedSampleData(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/setup/drop-tables":
                    SetupApi.DropTables(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/setup/clear-conn":
                    SetupApi.ClearConnString(ctx);
                    return ctx.BuildResponseBytes();

                // ---- Players ----
                case "/players":
                    PlayerList.HandleRequest(ctx);
                    return ctx.BuildResponseBytes();

                case "/players/new":
                    PlayerEdit.HandleRequest(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/players/save":
                    PlayerEditApi.Save(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/players/delete":
                    PlayerEditApi.Delete(ctx);
                    return ctx.BuildResponseBytes();

                // ---- Teams ----
                case "/teams":
                    TeamList.HandleRequest(ctx);
                    return ctx.BuildResponseBytes();

                case "/teams/new":
                    TeamEdit.HandleRequest(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/teams/save":
                    TeamEditApi.Save(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/teams/delete":
                    TeamEditApi.Delete(ctx);
                    return ctx.BuildResponseBytes();

                // ---- Roster (JOIN + Upsert) ----
                case "/roster":
                    Roster.HandleRequest(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/roster/list":
                    RosterApi.List(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/roster/assign":
                    RosterApi.Assign(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/roster/delete":
                    RosterApi.Delete(ctx);
                    return ctx.BuildResponseBytes();

                // ---- Tools ----
                case "/rawsql":
                    RawSql.HandleRequest(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/rawsql/run":
                    RawSqlApi.Run(ctx);
                    return ctx.BuildResponseBytes();

                case "/codegen":
                    CodeGen.HandleRequest(ctx);
                    return ctx.BuildResponseBytes();

                case "/api/codegen/run":
                    CodeGenApi.Run(ctx);
                    return ctx.BuildResponseBytes();
            }

            // Unmatched — 404.
            ctx.StatusCode = 404;
            ctx.ContentType = "text/html; charset=utf-8";
            ctx.Out.Append("<!DOCTYPE html><html><body><h1>404 Not Found</h1><p>")
                  .Append(WebUtility.HtmlEncode(req.RequestTarget))
                  .Append("</p><p><a href='/'>Home</a></p></body></html>");
            return ctx.BuildResponseBytes();
        }
    }
}