// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

namespace CsHttp
{
    /// <summary>
    /// Configuration for content-level parsers (query strings, form bodies,
    /// multipart, cookies). Separate from ParserOptions, which controls
    /// the envelope parser.
    /// 
    /// All size limits are in bytes unless otherwise noted.
    /// These limits defend against denial-of-service attacks at the content layer:
    /// parameter pollution, oversized inputs, hash collision attacks, and
    /// malformed encodings.
    /// </summary>
    public sealed class ContentParserOptions
    {
        // ─── Query String ────────────────────────────────────────────

        /// <summary>
        /// Maximum total length of the query string (characters after '?').
        /// Default: 8192.
        /// </summary>
        public int MaxQueryStringLength { get; set; } = 8192;

        /// <summary>
        /// Maximum number of key-value parameters in a query string.
        /// Defends against parameter pollution and HashDoS.
        /// Default: 1000.
        /// </summary>
        public int MaxQueryParameters { get; set; } = 1000;

        /// <summary>
        /// Maximum length of a single query parameter value (after decoding).
        /// Default: 8192.
        /// </summary>
        public int MaxQueryParameterValueLength { get; set; } = 8192;

        // ─── Form Body (application/x-www-form-urlencoded) ───────────

        /// <summary>
        /// Maximum total size of a form-encoded body in bytes.
        /// Default: 4 MB.
        /// </summary>
        public int MaxFormBodySize { get; set; } = 4 * 1024 * 1024;

        /// <summary>
        /// Maximum number of key-value parameters in a form body.
        /// Defends against parameter pollution and HashDoS.
        /// Default: 1000.
        /// </summary>
        public int MaxFormParameters { get; set; } = 1000;

        /// <summary>
        /// Maximum length of a single form parameter value (after decoding).
        /// Default: 4 MB.
        /// </summary>
        public int MaxFormParameterValueLength { get; set; } = 4 * 1024 * 1024;

        // ─── Multipart (multipart/form-data) ─────────────────────────

        /// <summary>
        /// Maximum number of parts in a multipart message.
        /// Default: 100.
        /// </summary>
        public int MaxMultipartParts { get; set; } = 100;

        /// <summary>
        /// Maximum size of a single multipart part body in bytes.
        /// Default: 50 MB.
        /// </summary>
        public long MaxMultipartPartSize { get; set; } = 50L * 1024 * 1024;

        /// <summary>
        /// Maximum length of the multipart boundary string.
        /// RFC 2046 limits this to 70 characters.
        /// Default: 70.
        /// </summary>
        public int MaxMultipartBoundaryLength { get; set; } = 70;

        /// <summary>
        /// Maximum total size of per-part headers in a multipart message.
        /// Default: 8192.
        /// </summary>
        public int MaxMultipartHeaderSize { get; set; } = 8192;

        // ─── Cookies ─────────────────────────────────────────────────

        /// <summary>
        /// Maximum number of cookies parsed from a Cookie header.
        /// Defends against cookie bomb attacks.
        /// Default: 50.
        /// </summary>
        public int MaxCookieCount { get; set; } = 50;

        /// <summary>
        /// Maximum total size of the Cookie header value in bytes.
        /// Default: 8192.
        /// </summary>
        public int MaxCookieHeaderSize { get; set; } = 8192;

        // ─── Percent Encoding Policies ───────────────────────────────

        /// <summary>
        /// When true, overlong UTF-8 sequences in percent-decoded output
        /// are rejected. Overlong encodings can bypass path and character
        /// filters (e.g., '/' encoded as %C0%AF instead of %2F).
        /// Default: true (reject).
        /// </summary>
        public bool RejectOverlongUtf8 { get; set; } = true;

        /// <summary>
        /// When true, %00 (null byte) in percent-encoded input is rejected.
        /// Null bytes can cause truncation in downstream C/native code.
        /// When false, null bytes are preserved as \0 in the decoded string.
        /// Default: false (preserve — application decides).
        /// </summary>
        public bool RejectNullBytes { get; set; } = false;

        /// <summary>
        /// When true, percent-decoding is performed exactly once.
        /// Double-encoded values like %2525 decode to %25, not to %.
        /// This is always true and cannot be changed — the property exists
        /// to document the behavior and make it discoverable.
        /// Default: true (always).
        /// </summary>
        public bool DecodeOnce { get; } = true;

        // ─── Duplicate Key Policy ────────────────────────────────────

        /// <summary>
        /// How duplicate keys are handled in query string, form, and cookie
        /// collections. See DuplicateKeyPolicy for options.
        /// Default: PreserveAll (expose all values, application decides).
        /// </summary>
        public DuplicateKeyPolicy DuplicatePolicy { get; set; } = DuplicateKeyPolicy.PreserveAll;

        // ─── Constructors and Presets ─────────────────────────────────

        /// <summary>
        /// Creates a ContentParserOptions instance with all defaults.
        /// </summary>
        public ContentParserOptions() { }

        /// <summary>
        /// A shared default instance. Do not modify — create a new instance for custom options.
        /// </summary>
        public static readonly ContentParserOptions Default = new ContentParserOptions();

        /// <summary>
        /// Creates a strict instance with tighter limits suitable for
        /// security-sensitive applications.
        /// </summary>
        public static ContentParserOptions Strict
        {
            get
            {
                return new ContentParserOptions
                {
                    MaxQueryParameters = 100,
                    MaxFormParameters = 100,
                    MaxMultipartParts = 20,
                    MaxCookieCount = 20,
                    RejectOverlongUtf8 = true,
                    RejectNullBytes = true,
                    DuplicatePolicy = DuplicateKeyPolicy.FirstWins,
                };
            }
        }
    }

    /// <summary>
    /// Determines how duplicate keys are stored in content collections
    /// (query string, form body, cookies).
    /// 
    /// This is a real-world concern: parameter pollution attacks send
    /// duplicate keys (e.g., ?role=user&amp;role=admin) hoping that a proxy
    /// reads the first value while the backend reads the last.
    /// </summary>
    public enum DuplicateKeyPolicy
    {
        /// <summary>
        /// All values for a key are preserved and accessible via GetValues().
        /// The indexer (collection["key"]) returns the first value.
        /// This is the safest default — the application sees everything and decides.
        /// </summary>
        PreserveAll,

        /// <summary>
        /// Only the first occurrence of a key is stored. Subsequent duplicates
        /// are silently discarded. Matches PHP's default behavior.
        /// </summary>
        FirstWins,

        /// <summary>
        /// Only the last occurrence of a key is stored. Earlier values are
        /// overwritten. Matches IIS/ASP.NET Web Forms default behavior.
        /// </summary>
        LastWins,
    }
}
