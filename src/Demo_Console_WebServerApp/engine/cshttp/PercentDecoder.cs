// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 3986 Section 2.1, WHATWG URL Standard
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;
using System.Text;

namespace CsHttp
{
    /// <summary>
    /// Decodes percent-encoded sequences (%HH) per RFC 3986 Section 2.1,
    /// with form-encoding support per the WHATWG URL Standard.
    /// 
    /// This is the foundational decoder used by QueryStringParser, FormParser,
    /// and CookieParser. It operates on raw strings (already extracted from bytes
    /// by the envelope parser) and returns decoded strings.
    /// 
    /// Two modes:
    ///   formMode = false  →  RFC 3986: %HH decoding only, '+' is literal
    ///   formMode = true   →  WHATWG form: %HH decoding + '+' means space
    /// 
    /// Security:
    ///   - Decodes exactly once (no recursive decoding)
    ///   - Rejects overlong UTF-8 sequences when configured
    ///   - Rejects or preserves null bytes (%00) when configured
    ///   - No regex — character-by-character scanning
    /// </summary>
    public static class PercentDecoder
    {
        /// <summary>
        /// Decodes a percent-encoded string.
        /// </summary>
        /// <param name="input">The percent-encoded string to decode.</param>
        /// <param name="formMode">
        /// When true, '+' is decoded as space (WHATWG form-encoding convention).
        /// When false, '+' is treated as literal '+' (RFC 3986).
        /// </param>
        /// <param name="options">
        /// Content parser options controlling null byte and overlong UTF-8 policy.
        /// Null for defaults.
        /// </param>
        /// <returns>A DecodeResult indicating success or failure with the decoded string.</returns>
        public static DecodeResult Decode(string input, bool formMode = false, ContentParserOptions options = null)
        {
            if (input == null)
                return DecodeResult.Ok(null);
            if (input.Length == 0)
                return DecodeResult.Ok("");

            var opts = options ?? ContentParserOptions.Default;

            // Fast path: scan for anything that needs decoding
            bool needsDecode = false;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '%' || (formMode && c == '+'))
                {
                    needsDecode = true;
                    break;
                }
            }

            if (!needsDecode)
                return DecodeResult.Ok(input);

            // Slow path: decode percent sequences and (optionally) '+' → space
            // 
            // Strategy: percent-encoded bytes may form multi-byte UTF-8 sequences.
            // We accumulate decoded raw bytes, then convert the whole thing to a
            // string via UTF-8 at the end. This correctly handles sequences like
            // %C3%A9 (é) where two percent-encoded bytes form one UTF-8 character.
            //
            // Non-encoded ASCII characters are passed through as single bytes.

            var bytes = new byte[input.Length]; // upper bound: decoded is never longer than encoded
            int byteCount = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '%')
                {
                    // Need at least 2 hex digits after '%'
                    if (i + 2 >= input.Length)
                    {
                        // Malformed: not enough characters after '%'
                        // Lenient: pass through the literal '%'
                        bytes[byteCount++] = (byte)'%';
                        continue;
                    }

                    int hi = HexVal(input[i + 1]);
                    int lo = HexVal(input[i + 2]);

                    if (hi < 0 || lo < 0)
                    {
                        // Not valid hex: pass through the literal '%'
                        bytes[byteCount++] = (byte)'%';
                        continue;
                    }

                    byte decoded = (byte)((hi << 4) | lo);

                    // Null byte check
                    if (decoded == 0x00 && opts.RejectNullBytes)
                    {
                        return DecodeResult.Fail(
                            DecodeErrorKind.NullByteRejected, i,
                            "Null byte (%00) rejected at position " + i + ".");
                    }

                    bytes[byteCount++] = decoded;
                    i += 2; // skip the two hex digits (loop will advance past '%')
                }
                else if (formMode && c == '+')
                {
                    bytes[byteCount++] = 0x20; // space
                }
                else
                {
                    // Non-encoded character: pass through as ASCII byte
                    // Characters above 0x7F in the input string are technically
                    // invalid in a percent-encoded context (they should have been
                    // percent-encoded), but we pass them through as UTF-8 bytes.
                    if (c <= 0x7F)
                    {
                        bytes[byteCount++] = (byte)c;
                    }
                    else
                    {
                        // Encode the char as UTF-8 bytes into the buffer
                        byte[] charBytes = Encoding.UTF8.GetBytes(new char[] { c }, 0, 1);
                        for (int j = 0; j < charBytes.Length; j++)
                        {
                            // Ensure we don't overflow (shouldn't happen, but defensive)
                            if (byteCount >= bytes.Length)
                            {
                                var expanded = new byte[bytes.Length * 2];
                                Buffer.BlockCopy(bytes, 0, expanded, 0, byteCount);
                                bytes = expanded;
                            }
                            bytes[byteCount++] = charBytes[j];
                        }
                    }
                }
            }

            // Validate UTF-8 and check for overlong sequences
            if (opts.RejectOverlongUtf8)
            {
                var utf8Err = ValidateUtf8(bytes, byteCount);
                if (utf8Err != null)
                    return utf8Err;
            }

            // Convert the decoded bytes to a string via UTF-8
            string result;
            try
            {
                result = Encoding.UTF8.GetString(bytes, 0, byteCount);
            }
            catch (Exception)
            {
                return DecodeResult.Fail(
                    DecodeErrorKind.InvalidUtf8, 0,
                    "Decoded bytes are not valid UTF-8.");
            }

            return DecodeResult.Ok(result);
        }

        /// <summary>
        /// Decodes a percent-encoded string, returning the decoded value directly.
        /// On decode failure, returns the original input unchanged.
        /// 
        /// This is a convenience method for contexts where decode errors
        /// should be tolerated silently (e.g., cookie values).
        /// </summary>
        public static string DecodeLenient(string input, bool formMode = false, ContentParserOptions options = null)
        {
            if (input == null) return null;
            var result = Decode(input, formMode, options);
            return result.Success ? result.Value : input;
        }

        // ─── UTF-8 Validation ────────────────────────────────────────────

        /// <summary>
        /// Validates that the decoded byte sequence is well-formed UTF-8,
        /// rejecting overlong encodings.
        /// 
        /// Overlong sequences are a security concern: they allow characters
        /// to be represented in more bytes than necessary. For example,
        /// the '/' character (U+002F) can be encoded as:
        ///   Valid:    2F
        ///   Overlong: C0 AF  (2-byte encoding of a 1-byte character)
        /// 
        /// An attacker can use overlong encodings to bypass path filters
        /// that check for '/' but don't check for its overlong form.
        /// </summary>
        private static DecodeResult ValidateUtf8(byte[] data, int length)
        {
            int i = 0;
            while (i < length)
            {
                byte b = data[i];

                if (b <= 0x7F)
                {
                    // 1-byte sequence (ASCII): 0xxxxxxx
                    i++;
                    continue;
                }

                int seqLen;
                int codepoint;
                int minCodepoint;

                if ((b & 0xE0) == 0xC0)
                {
                    // 2-byte sequence: 110xxxxx 10xxxxxx
                    seqLen = 2;
                    codepoint = b & 0x1F;
                    minCodepoint = 0x80; // must encode codepoints >= 0x80
                }
                else if ((b & 0xF0) == 0xE0)
                {
                    // 3-byte sequence: 1110xxxx 10xxxxxx 10xxxxxx
                    seqLen = 3;
                    codepoint = b & 0x0F;
                    minCodepoint = 0x800; // must encode codepoints >= 0x800
                }
                else if ((b & 0xF8) == 0xF0)
                {
                    // 4-byte sequence: 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
                    seqLen = 4;
                    codepoint = b & 0x07;
                    minCodepoint = 0x10000; // must encode codepoints >= 0x10000
                }
                else
                {
                    // Invalid UTF-8 leading byte
                    return DecodeResult.Fail(
                        DecodeErrorKind.InvalidUtf8, i,
                        "Invalid UTF-8 leading byte 0x" + b.ToString("X2") + " at decoded byte " + i + ".");
                }

                // Check we have enough continuation bytes
                if (i + seqLen > length)
                {
                    return DecodeResult.Fail(
                        DecodeErrorKind.InvalidUtf8, i,
                        "Truncated UTF-8 sequence at decoded byte " + i + ".");
                }

                // Read continuation bytes
                for (int j = 1; j < seqLen; j++)
                {
                    byte cb = data[i + j];
                    if ((cb & 0xC0) != 0x80)
                    {
                        return DecodeResult.Fail(
                            DecodeErrorKind.InvalidUtf8, i + j,
                            "Invalid UTF-8 continuation byte 0x" + cb.ToString("X2") +
                            " at decoded byte " + (i + j) + ".");
                    }
                    codepoint = (codepoint << 6) | (cb & 0x3F);
                }

                // Overlong check: the codepoint must require this many bytes
                if (codepoint < minCodepoint)
                {
                    return DecodeResult.Fail(
                        DecodeErrorKind.OverlongUtf8, i,
                        "Overlong UTF-8 encoding at decoded byte " + i +
                        ": codepoint U+" + codepoint.ToString("X4") +
                        " encoded in " + seqLen + " bytes (minimum is " +
                        MinBytesForCodepoint(codepoint) + ").");
                }

                // Reject surrogates (U+D800..U+DFFF) — invalid in UTF-8
                if (codepoint >= 0xD800 && codepoint <= 0xDFFF)
                {
                    return DecodeResult.Fail(
                        DecodeErrorKind.InvalidUtf8, i,
                        "UTF-8 encodes surrogate codepoint U+" + codepoint.ToString("X4") +
                        " at decoded byte " + i + ".");
                }

                // Reject codepoints above U+10FFFF
                if (codepoint > 0x10FFFF)
                {
                    return DecodeResult.Fail(
                        DecodeErrorKind.InvalidUtf8, i,
                        "UTF-8 encodes codepoint U+" + codepoint.ToString("X4") +
                        " above maximum U+10FFFF at decoded byte " + i + ".");
                }

                i += seqLen;
            }

            return null; // valid
        }

        /// <summary>
        /// Returns the minimum number of UTF-8 bytes needed to encode a codepoint.
        /// Used in overlong-encoding error messages.
        /// </summary>
        private static int MinBytesForCodepoint(int codepoint)
        {
            if (codepoint <= 0x7F) return 1;
            if (codepoint <= 0x7FF) return 2;
            if (codepoint <= 0xFFFF) return 3;
            return 4;
        }

        // ─── Hex Digit Parsing ───────────────────────────────────────────

        /// <summary>
        /// Returns the numeric value of a hex digit character (0-15),
        /// or -1 if the character is not a valid hex digit.
        /// </summary>
        private static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        }
    }

    // ─── Decode Result ───────────────────────────────────────────────

    /// <summary>
    /// The result of a percent-decode operation.
    /// </summary>
    public sealed class DecodeResult
    {
        /// <summary>True if decoding succeeded.</summary>
        public bool Success { get; }

        /// <summary>The decoded string (null-preserving: null input → null output).</summary>
        public string Value { get; }

        /// <summary>The error that caused decoding to fail. Null on success.</summary>
        public DecodeError Error { get; }

        private DecodeResult(bool success, string value, DecodeError error)
        {
            Success = success;
            Value = value;
            Error = error;
        }

        internal static DecodeResult Ok(string value)
        {
            return new DecodeResult(true, value, null);
        }

        internal static DecodeResult Fail(DecodeErrorKind kind, int position, string message)
        {
            return new DecodeResult(false, null, new DecodeError(kind, position, message));
        }
    }

    /// <summary>
    /// Describes a decode failure.
    /// </summary>
    public sealed class DecodeError
    {
        /// <summary>The category of error.</summary>
        public DecodeErrorKind Kind { get; }

        /// <summary>Character position in the input where the error was detected.</summary>
        public int Position { get; }

        /// <summary>Human-readable description.</summary>
        public string Message { get; }

        public DecodeError(DecodeErrorKind kind, int position, string message)
        {
            Kind = kind;
            Position = position;
            Message = message;
        }

        public override string ToString()
        {
            return string.Format("[{0}] at position {1}: {2}", Kind, Position, Message);
        }
    }

    /// <summary>
    /// Categories of percent-decode failure.
    /// </summary>
    public enum DecodeErrorKind
    {
        /// <summary>A %00 null byte was encountered and rejected by configuration.</summary>
        NullByteRejected,

        /// <summary>Decoded bytes contain an overlong UTF-8 sequence.</summary>
        OverlongUtf8,

        /// <summary>Decoded bytes are not valid UTF-8.</summary>
        InvalidUtf8,
    }
}
