using System;
using System.Collections;
using System.Collections.Generic;

namespace CsHttp
{
    /// <summary>
    /// Represents a single HTTP header field as received on the wire.
    /// Preserves the original field-name casing.
    /// </summary>
    public sealed class HttpHeader
    {
        /// <summary>
        /// The field name exactly as received (original casing preserved).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The field value with leading/trailing OWS trimmed per RFC 9112 Section 5.1.
        /// </summary>
        public string Value { get; internal set; }

        /// <summary>
        /// The raw bytes of this header line as received (name ":" OWS value OWS).
        /// Available for security auditing and binary inspection.
        /// Null if the parser was invoked with string input rather than byte input.
        /// </summary>
        public byte[] RawBytes { get; internal set; }

        public HttpHeader(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public HttpHeader(string name, string value, byte[] rawBytes)
        {
            Name = name;
            Value = value;
            RawBytes = rawBytes;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Name, Value);
        }
    }

    /// <summary>
    /// A header collection that preserves insertion order (wire order) while
    /// providing case-insensitive keyed access.
    /// 
    /// Multiple headers with the same field name are stored as separate entries
    /// in the ordered list. Keyed access returns comma-joined values per
    /// RFC 9110 Section 5.3 ("A recipient MAY combine multiple field lines 
    /// with the same name into one field-line ... by appending each subsequent
    /// field line value ... separated by a comma").
    /// </summary>
    public sealed class HttpHeaderCollection : IEnumerable<HttpHeader>
    {
        private readonly List<HttpHeader> _ordered;
        private readonly Dictionary<string, List<int>> _index;

        public HttpHeaderCollection()
        {
            _ordered = new List<HttpHeader>();
            _index = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Number of individual header field lines.</summary>
        public int Count => _ordered.Count;

        /// <summary>
        /// Adds a header field line, preserving wire order.
        /// Duplicate field names are permitted (stored separately).
        /// </summary>
        public void Add(HttpHeader header)
        {
            int pos = _ordered.Count;
            _ordered.Add(header);

            if (!_index.TryGetValue(header.Name, out var positions))
            {
                positions = new List<int>();
                _index[header.Name] = positions;
            }
            positions.Add(pos);
        }

        /// <summary>
        /// Adds a header field line by name and value.
        /// </summary>
        public void Add(string name, string value)
        {
            Add(new HttpHeader(name, value));
        }

        /// <summary>
        /// Gets the combined value for a field name (comma-joined if multiple lines exist).
        /// Case-insensitive lookup.
        /// Returns null if the field name is not present.
        /// </summary>
        public string GetValue(string fieldName)
        {
            if (!_index.TryGetValue(fieldName, out var positions))
                return null;

            if (positions.Count == 1)
                return _ordered[positions[0]].Value;

            // Combine per RFC 9110 Section 5.3
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < positions.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(_ordered[positions[i]].Value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets all individual values for a field name as separate strings.
        /// Case-insensitive lookup.
        /// Returns an empty array if the field name is not present.
        /// </summary>
        public string[] GetValues(string fieldName)
        {
            if (!_index.TryGetValue(fieldName, out var positions))
                return Array.Empty<string>();

            var result = new string[positions.Count];
            for (int i = 0; i < positions.Count; i++)
                result[i] = _ordered[positions[i]].Value;
            return result;
        }

        /// <summary>
        /// Gets all HttpHeader entries for a field name.
        /// Case-insensitive lookup.
        /// </summary>
        public HttpHeader[] GetHeaders(string fieldName)
        {
            if (!_index.TryGetValue(fieldName, out var positions))
                return Array.Empty<HttpHeader>();

            var result = new HttpHeader[positions.Count];
            for (int i = 0; i < positions.Count; i++)
                result[i] = _ordered[positions[i]];
            return result;
        }

        /// <summary>
        /// Returns true if the collection contains at least one header with this field name.
        /// Case-insensitive.
        /// </summary>
        public bool Contains(string fieldName)
        {
            return _index.ContainsKey(fieldName);
        }

        /// <summary>
        /// Gets a header by its wire-order index (0-based).
        /// </summary>
        public HttpHeader this[int index] => _ordered[index];

        /// <summary>
        /// Gets the combined value for a field name. Same as GetValue().
        /// Returns null if not present.
        /// </summary>
        public string this[string fieldName] => GetValue(fieldName);

        /// <summary>
        /// Removes all header lines with the given field name.
        /// Used internally when Transfer-Encoding overrides Content-Length.
        /// </summary>
        internal bool Remove(string fieldName)
        {
            if (!_index.TryGetValue(fieldName, out var positions))
                return false;

            // Remove from ordered list in reverse order to preserve indices
            for (int i = positions.Count - 1; i >= 0; i--)
            {
                _ordered.RemoveAt(positions[i]);
            }

            _index.Remove(fieldName);

            // Rebuild index (positions shifted after removal)
            RebuildIndex();
            return true;
        }

        private void RebuildIndex()
        {
            _index.Clear();
            for (int i = 0; i < _ordered.Count; i++)
            {
                var name = _ordered[i].Name;
                if (!_index.TryGetValue(name, out var positions))
                {
                    positions = new List<int>();
                    _index[name] = positions;
                }
                positions.Add(i);
            }
        }

        /// <summary>
        /// Returns all distinct field names in wire order (first occurrence order).
        /// </summary>
        public IEnumerable<string> GetFieldNames()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in _ordered)
            {
                if (seen.Add(h.Name))
                    yield return h.Name;
            }
        }

        public IEnumerator<HttpHeader> GetEnumerator() => _ordered.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
