namespace CsHttp
{
    /// <summary>
    /// Configuration for the HTTP/1.1 parser.
    /// 
    /// Kind 1 (RFC correctness) is always enforced and cannot be disabled.
    /// Kind 2 (policy limits) is controlled by these options.
    /// 
    /// All size limits are in bytes. A value of -1 means unlimited.
    /// </summary>
    public sealed class ParserOptions
    {
        /// <summary>
        /// Maximum length of the request-line or status-line in bytes.
        /// RFC 9112 Section 3 RECOMMENDS support for at least 8000 octets.
        /// Default: 8192.
        /// </summary>
        public int MaxStartLineLength { get; set; } = 8192;

        /// <summary>
        /// Maximum length of a single header field line in bytes (name + colon + value).
        /// Default: 8192.
        /// </summary>
        public int MaxHeaderLineLength { get; set; } = 8192;

        /// <summary>
        /// Maximum number of header field lines in a single message.
        /// Default: 100.
        /// </summary>
        public int MaxHeaderCount { get; set; } = 100;

        /// <summary>
        /// Maximum total size of the header section in bytes (all header lines combined).
        /// Default: 65536 (64 KB).
        /// </summary>
        public int MaxHeaderSectionSize { get; set; } = 65536;

        /// <summary>
        /// Maximum message body size in bytes.
        /// Default: -1 (unlimited).
        /// </summary>
        public long MaxBodySize { get; set; } = -1;

        /// <summary>
        /// Maximum chunk size (hex value) in a chunked transfer coding.
        /// Prevents memory exhaustion from maliciously large chunk-size values.
        /// Default: 0x7FFFFFFF (2 GB, max int).
        /// </summary>
        public long MaxChunkSize { get; set; } = 0x7FFFFFFF;

        /// <summary>
        /// When true, obsolete line folding (obs-fold) in headers is rejected.
        /// When false, obs-fold is replaced with SP and a warning is recorded.
        /// RFC 9112 Section 5.2: a server MUST either reject or replace.
        /// Default: true (reject).
        /// </summary>
        public bool RejectObsoleteLineFolding { get; set; } = true;

        /// <summary>
        /// When true, the parser rejects messages where both Transfer-Encoding
        /// and Content-Length are present (Section 6.3 rule 3).
        /// When false, Transfer-Encoding takes precedence and Content-Length is removed.
        /// Default: true (strict — reject the ambiguity).
        /// </summary>
        public bool RejectConflictingFraming { get; set; } = true;

        /// <summary>
        /// When true, the parser rejects any protocol deviation.
        /// When false, the parser tolerates deviations permitted by MAY/SHOULD 
        /// clauses and records them as warnings.
        /// Default: false (lenient, matching real-world server behavior).
        /// </summary>
        public bool StrictMode { get; set; } = false;

        // --- Validation Callbacks (Kind 2 expansion points) ---

        /// <summary>
        /// Called after the start-line has been parsed.
        /// Return false to abort parsing.
        /// Parameters: (method or statusCode, requestTarget or reasonPhrase, version)
        /// </summary>
        public OnStartLineParsedCallback OnStartLineParsed { get; set; }

        /// <summary>
        /// Called after each header field line has been parsed.
        /// Return false to abort parsing.
        /// Parameters: (fieldName, fieldValue, headerIndex)
        /// </summary>
        public OnHeaderParsedCallback OnHeaderParsed { get; set; }

        /// <summary>
        /// Called when a chunk header is encountered during chunked body reading.
        /// Return false to abort parsing.
        /// Parameters: (chunkSize, totalBytesReadSoFar)
        /// </summary>
        public OnChunkHeaderCallback OnChunkHeader { get; set; }

        /// <summary>
        /// Called periodically during body reading.
        /// Return false to abort parsing.
        /// Parameters: (bytesReadInThisBatch, totalBytesReadSoFar)
        /// </summary>
        public OnBodyProgressCallback OnBodyProgress { get; set; }

        /// <summary>
        /// Creates a ParserOptions instance with all defaults.
        /// </summary>
        public ParserOptions() { }

        /// <summary>
        /// A shared default instance. Do not modify — create a new instance for custom options.
        /// </summary>
        public static readonly ParserOptions Default = new ParserOptions();

        /// <summary>
        /// Creates a strict-mode instance that rejects all deviations.
        /// </summary>
        public static ParserOptions Strict
        {
            get
            {
                return new ParserOptions
                {
                    StrictMode = true,
                    RejectObsoleteLineFolding = true,
                    RejectConflictingFraming = true,
                };
            }
        }
    }

    // --- Callback delegate signatures ---

    /// <summary>
    /// Callback invoked after the start-line is parsed.
    /// For requests: field1 = method, field2 = requestTarget, field3 = version.
    /// For responses: field1 = statusCode (as string), field2 = reasonPhrase, field3 = version.
    /// Return false to abort parsing.
    /// </summary>
    public delegate bool OnStartLineParsedCallback(string field1, string field2, string field3);

    /// <summary>
    /// Callback invoked after each header field line is parsed.
    /// Return false to abort parsing.
    /// </summary>
    public delegate bool OnHeaderParsedCallback(string fieldName, string fieldValue, int headerIndex);

    /// <summary>
    /// Callback invoked when a chunk header is read during chunked decoding.
    /// Return false to abort parsing.
    /// </summary>
    public delegate bool OnChunkHeaderCallback(long chunkSize, long totalBytesRead);

    /// <summary>
    /// Callback invoked during body reading progress.
    /// Return false to abort parsing.
    /// </summary>
    public delegate bool OnBodyProgressCallback(int batchSize, long totalBytesRead);
}
