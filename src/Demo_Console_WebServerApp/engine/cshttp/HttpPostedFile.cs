// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 7578 (multipart/form-data)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

namespace CsHttp
{
    /// <summary>
    /// Represents a single file uploaded via multipart/form-data.
    /// 
    /// Per RFC 7578 Section 4.2, each file part carries:
    ///   - A field name (from Content-Disposition: form-data; name="field")
    ///   - An optional filename (from Content-Disposition: form-data; filename="photo.jpg")
    ///   - An optional Content-Type (defaults to "application/octet-stream")
    ///   - The raw file bytes
    /// 
    /// Security note: the FileName is reported exactly as received in the
    /// Content-Disposition header. It may contain path traversal sequences
    /// ("../../etc/passwd"), Unicode tricks, or other injection attempts.
    /// cshttp does NOT normalize or sanitize the filename — that is the
    /// application's responsibility before using it for file system operations.
    /// </summary>
    public sealed class HttpPostedFile
    {
        /// <summary>
        /// The form field name from Content-Disposition (the "name" parameter).
        /// This is the name attribute of the HTML file input element.
        /// Example: "avatar" from name="avatar"
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The original filename as declared by the client.
        /// From the "filename" parameter of Content-Disposition.
        /// 
        /// May be null if no filename was provided (rare but spec-legal).
        /// May contain path separators, Unicode, or other characters.
        /// The application MUST sanitize this before any filesystem use.
        /// 
        /// Example: "photo.jpg" from filename="photo.jpg"
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The MIME content type declared for this file part.
        /// From the Content-Type header within the multipart part.
        /// 
        /// Defaults to "application/octet-stream" if not specified.
        /// This is the declared type — the actual file content may differ.
        /// The application should validate the content independently.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// The raw file content bytes.
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// The size of the file content in bytes.
        /// Equivalent to Bytes.Length.
        /// </summary>
        public int Size => Bytes != null ? Bytes.Length : 0;

        /// <summary>
        /// Creates a new HttpPostedFile instance.
        /// </summary>
        public HttpPostedFile(string name, string fileName, string contentType, byte[] bytes)
        {
            Name = name;
            FileName = fileName;
            ContentType = contentType ?? "application/octet-stream";
            Bytes = bytes;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1} ({2}, {3} bytes)",
                Name ?? "(no name)",
                FileName ?? "(no filename)",
                ContentType,
                Size);
        }
    }
}
