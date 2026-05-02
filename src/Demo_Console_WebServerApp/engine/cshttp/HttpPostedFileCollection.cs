// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;
using System.Collections;
using System.Collections.Generic;

namespace CsHttp
{
    /// <summary>
    /// A collection of uploaded files from a multipart/form-data request.
    /// Keyed by form field name, with support for multiple files per field
    /// (e.g., &lt;input type="file" multiple&gt;).
    /// 
    /// Access patterns:
    ///   files["avatar"]       → first file with field name "avatar" (or null)
    ///   files.GetAll("docs")  → all files with field name "docs"
    ///   files.GetAll()        → all files in part order
    ///   files.Count           → total file count
    ///   files.AllKeys          → distinct field names
    /// </summary>
    public sealed class HttpPostedFileCollection : IEnumerable<HttpPostedFile>
    {
        private readonly List<HttpPostedFile> _files;
        private readonly Dictionary<string, List<int>> _index;

        /// <summary>
        /// Creates an empty file collection.
        /// </summary>
        public HttpPostedFileCollection()
        {
            _files = new List<HttpPostedFile>();
            _index = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        }

        /// <summary>Total number of uploaded files.</summary>
        public int Count => _files.Count;

        /// <summary>
        /// Adds a file to the collection.
        /// </summary>
        internal void Add(HttpPostedFile file)
        {
            int pos = _files.Count;
            _files.Add(file);

            string key = file.Name ?? "";
            if (!_index.TryGetValue(key, out var positions))
            {
                positions = new List<int>();
                _index[key] = positions;
            }
            positions.Add(pos);
        }

        /// <summary>
        /// Gets the first file with the given field name.
        /// Returns null if no file exists with that name.
        /// </summary>
        public HttpPostedFile this[string fieldName]
        {
            get
            {
                if (!_index.TryGetValue(fieldName, out var positions))
                    return null;
                return _files[positions[0]];
            }
        }

        /// <summary>
        /// Gets a file by its insertion-order index (0-based).
        /// </summary>
        public HttpPostedFile this[int index] => _files[index];

        /// <summary>
        /// Gets all files with the given field name.
        /// Returns an empty array if no files exist with that name.
        /// </summary>
        public HttpPostedFile[] GetAll(string fieldName)
        {
            if (!_index.TryGetValue(fieldName, out var positions))
                return Array.Empty<HttpPostedFile>();

            var result = new HttpPostedFile[positions.Count];
            for (int i = 0; i < positions.Count; i++)
                result[i] = _files[positions[i]];
            return result;
        }

        /// <summary>
        /// Gets all uploaded files in part order.
        /// </summary>
        public HttpPostedFile[] GetAll()
        {
            return _files.ToArray();
        }

        /// <summary>
        /// Returns true if the collection contains at least one file
        /// with the given field name.
        /// </summary>
        public bool Contains(string fieldName)
        {
            return _index.ContainsKey(fieldName);
        }

        /// <summary>
        /// Returns all distinct field names in insertion order.
        /// </summary>
        public string[] AllKeys
        {
            get
            {
                var keys = new List<string>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var f in _files)
                {
                    string key = f.Name ?? "";
                    if (seen.Add(key))
                        keys.Add(key);
                }
                return keys.ToArray();
            }
        }

        // ─── IEnumerable ────────────────────────────────────────────

        public IEnumerator<HttpPostedFile> GetEnumerator() => _files.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ─── Static Helpers ─────────────────────────────────────────

        /// <summary>
        /// An empty, shared collection for cases where no files were uploaded.
        /// </summary>
        public static readonly HttpPostedFileCollection Empty = new HttpPostedFileCollection();
    }
}
