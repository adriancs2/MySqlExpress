namespace CsHttp
{
    /// <summary>
    /// Categories of non-fatal deviations the parser tolerated.
    /// These map to RFC 9112 MAY/SHOULD clauses where lenient behavior is permitted.
    /// </summary>
    public enum ParseWarningKind
    {
        /// <summary>A lone LF was accepted as line terminator without preceding CR. (Section 2.2)</summary>
        LFWithoutCR,

        /// <summary>One or more empty lines (CRLF) preceded the start-line. (Section 2.2)</summary>
        LeadingEmptyLines,

        /// <summary>Obsolete line folding was replaced with SP. (Section 5.2)</summary>
        ObsLineFoldingReplaced,

        /// <summary>Bare CR was replaced with SP. (Section 2.2)</summary>
        BareCRReplaced,

        /// <summary>Whitespace-preceded lines after start-line were consumed and discarded. (Section 2.2)</summary>
        WhitespaceAfterStartLineConsumed,

        /// <summary>The reason-phrase in a status-line was absent. (Section 4)</summary>
        EmptyReasonPhrase,

        /// <summary>Lenient whitespace parsing was applied to the start-line. (Section 3, Section 4)</summary>
        LenientStartLineWhitespace,

        /// <summary>Duplicate Content-Length values were identical and collapsed. (Section 6.3 rule 5)</summary>
        DuplicateContentLengthCollapsed,

        /// <summary>Unrecognized chunk extensions were ignored. (Section 7.1.1)</summary>
        ChunkExtensionIgnored,
    }

    /// <summary>
    /// Records a single tolerated deviation with its byte position.
    /// </summary>
    public sealed class ParseWarning
    {
        /// <summary>The category of deviation.</summary>
        public ParseWarningKind Kind { get; }

        /// <summary>Byte offset where the deviation was detected.</summary>
        public int Position { get; }

        /// <summary>Optional human-readable detail.</summary>
        public string Message { get; }

        public ParseWarning(ParseWarningKind kind, int position, string message)
        {
            Kind = kind;
            Position = position;
            Message = message;
        }

        public override string ToString()
        {
            return string.Format("[{0}] at byte {1}: {2}", Kind, Position, Message);
        }
    }
}
