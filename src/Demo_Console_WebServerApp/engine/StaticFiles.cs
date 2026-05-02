using System.Globalization;
using System.IO;

namespace System
{
    /// <summary>
    /// Serves static files (CSS, JS, fonts, images) from the directory next
    /// to the EXE — with proper HTTP caching, the way IIS / nginx / Kestrel
    /// have always done it.
    ///
    /// HTTP caching for static files works on two layers:
    ///
    /// 1. CONDITIONAL GET (always on)
    ///    The server sends ETag and Last-Modified with every file response.
    ///    The browser sends them back as If-None-Match / If-Modified-Since
    ///    on later requests. If the file hasn't changed, the server replies
    ///    "304 Not Modified" with no body — the round-trip becomes ~100
    ///    bytes instead of resending the file.
    ///
    /// 2. CACHE-CONTROL (skip the round-trip entirely)
    ///    The server adds Cache-Control: max-age=N on the response. For the
    ///    next N seconds, the browser serves from its own cache without
    ///    even asking the server. Faster than 304, because there is no
    ///    network round-trip at all.
    ///
    /// Both layers are implemented here. Real servers do exactly this.
    /// </summary>
    public static class StaticFiles
    {
        /// <summary>
        /// How long browsers may use a cached copy without revalidating
        /// (max-age in Cache-Control). Tuned conservatively — short enough
        /// that you see CSS edits on a page reload, long enough that font
        /// files don't get re-fetched on every navigation within a session.
        /// </summary>
        const int CacheMaxAgeSeconds = 60 * 5;  // 5 minutes

        public static bool IsStaticAsset(string path)
        {
            return path.StartsWith("/css/")
                || path.StartsWith("/js/")
                || path.StartsWith("/fonts/")
                || path.StartsWith("/images/")
                || path == "/favicon.ico";
        }

        public static bool TryServe(Ctx ctx)
        {
            if (!IsStaticAsset(ctx.Path))
                return false;

            // ── Path safety ─────────────────────────────────────────────
            string rel = ctx.Path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            if (rel.Contains("..") || Path.IsPathRooted(rel))
            {
                ctx.StatusCode = 400;
                ctx.ContentType = "text/plain; charset=utf-8";
                ctx.Out.Append("Bad request.");
                return true;
            }

            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rel);

            if (!File.Exists(fullPath))
            {
                ctx.StatusCode = 404;
                ctx.ContentType = "text/plain; charset=utf-8";
                ctx.Out.Append("Not Found: " + ctx.Path);
                return true;
            }

            // ── Build cache validators from file metadata ───────────────
            //
            // The ETag is a "weak" hash of (size, last-write-time). It's not
            // cryptographic — it just needs to change when the file changes.
            // (size, mtime) is what nginx and IIS use too. Cheap to compute,
            // changes the moment you save the file in your editor.
            //
            // Last-Modified is the file's mtime as an HTTP date. Browsers
            // send it back via If-Modified-Since.

            var fi = new FileInfo(fullPath);
            // Round mtime to whole seconds — HTTP-date headers don't carry
            // sub-second precision, so we'd compare a sub-second value
            // against a second-precision value and never match.
            DateTime lastModifiedUtc = new DateTime(
                fi.LastWriteTimeUtc.Year,  fi.LastWriteTimeUtc.Month,
                fi.LastWriteTimeUtc.Day,   fi.LastWriteTimeUtc.Hour,
                fi.LastWriteTimeUtc.Minute, fi.LastWriteTimeUtc.Second,
                DateTimeKind.Utc);

            string etag = BuildETag(fi.Length, lastModifiedUtc);
            string lastModifiedHttp = lastModifiedUtc.ToString("r", CultureInfo.InvariantCulture);
            //                                       ^ "r" = RFC 1123 / HTTP-date format
            //                                       e.g. "Wed, 15 Apr 2026 12:34:56 GMT"

            // ── Conditional request short-circuit ───────────────────────
            //
            // Per RFC 9110, If-None-Match takes precedence over
            // If-Modified-Since when both are present. We honour that by
            // checking ETag first, then mtime as a fallback.

            string ifNoneMatch = ctx.Request.Headers["If-None-Match"];
            string ifModifiedSince = ctx.Request.Headers["If-Modified-Since"];

            bool notModified = false;

            if (!string.IsNullOrEmpty(ifNoneMatch))
            {
                // The browser may send several ETags separated by commas
                // (rare in practice, but allowed). Any match wins.
                notModified = ETagMatches(ifNoneMatch, etag);
            }
            else if (!string.IsNullOrEmpty(ifModifiedSince))
            {
                DateTime since;
                if (DateTime.TryParseExact(ifModifiedSince, "r",
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                        out since))
                {
                    // since is parsed as local with AssumeUniversal flag → UTC.
                    since = since.ToUniversalTime();
                    notModified = lastModifiedUtc <= since;
                }
            }

            // Always include the validators on responses (200 and 304),
            // and a Cache-Control hint for the browser's local cache.
            ctx.Headers.Add(new Collections.Generic.KeyValuePair<string, string>(
                "ETag", etag));
            ctx.Headers.Add(new Collections.Generic.KeyValuePair<string, string>(
                "Last-Modified", lastModifiedHttp));
            ctx.Headers.Add(new Collections.Generic.KeyValuePair<string, string>(
                "Cache-Control", "public, max-age=" + CacheMaxAgeSeconds));

            if (notModified)
            {
                // 304 Not Modified — no body, just tell the browser
                // "what you cached is still good." The browser then renders
                // from its own cache without us shipping any bytes.
                ctx.StatusCode = 304;
                ctx.ContentType = GetContentType(fullPath);
                // No OutBytes, no Out content — the response builder will
                // emit just the headers, which is exactly what 304 needs.
                return true;
            }

            // ── Full response with body ─────────────────────────────────
            ctx.OutBytes = File.ReadAllBytes(fullPath);
            ctx.ContentType = GetContentType(fullPath);
            return true;
        }

        /// <summary>
        /// Build a weak ETag from file size and mtime. Format:
        ///     W/"&lt;size-hex&gt;-&lt;mtime-ticks-hex&gt;"
        ///
        /// The "W/" prefix marks it as a weak validator — meaning we're
        /// asserting the resource is "semantically" the same, not byte-for-
        /// byte identical. That's what we get from (size, mtime), and it
        /// matches what nginx and IIS produce. Strong ETags would require
        /// hashing the file contents, which is slower and unnecessary here.
        /// </summary>
        static string BuildETag(long size, DateTime mtimeUtc)
        {
            return "W/\"" + size.ToString("x") + "-" + mtimeUtc.Ticks.ToString("x") + "\"";
        }

        /// <summary>
        /// Compare an If-None-Match header value against our ETag.
        /// The header may contain a single tag, a comma-separated list,
        /// or "*" (meaning "match any current representation"). For
        /// weak/strong distinction, browsers typically send back exactly
        /// what we sent, so a string compare is sufficient — but we also
        /// match "*" and tolerate whitespace.
        /// </summary>
        static bool ETagMatches(string headerValue, string ourTag)
        {
            string[] parts = headerValue.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p == "*") return true;
                if (p == ourTag) return true;
            }
            return false;
        }

        static string GetContentType(string fullPath)
        {
            string ext = Path.GetExtension(fullPath).ToLowerInvariant();
            switch (ext)
            {
                case ".css":   return "text/css; charset=utf-8";
                case ".js":    return "application/javascript; charset=utf-8";
                case ".html":
                case ".htm":   return "text/html; charset=utf-8";
                case ".json":  return "application/json; charset=utf-8";
                case ".txt":   return "text/plain; charset=utf-8";
                case ".md":    return "text/markdown; charset=utf-8";
                case ".woff":  return "font/woff";
                case ".woff2": return "font/woff2";
                case ".ttf":   return "font/ttf";
                case ".otf":   return "font/otf";
                case ".eot":   return "application/vnd.ms-fontobject";
                case ".svg":   return "image/svg+xml";
                case ".png":   return "image/png";
                case ".jpg":
                case ".jpeg":  return "image/jpeg";
                case ".gif":   return "image/gif";
                case ".webp":  return "image/webp";
                case ".ico":   return "image/x-icon";
                default:       return "application/octet-stream";
            }
        }
    }
}
