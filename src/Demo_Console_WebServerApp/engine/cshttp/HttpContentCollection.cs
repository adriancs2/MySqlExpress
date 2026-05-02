// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;
using System.Collections;
using System.Collections.Generic;

namespace CsHttp
{
    /// <summary>
    /// An ordered key-value collection for HTTP content parameters.
    /// Used by QueryString, Form, and Cookie parsers.
    /// 
    /// Design parallels HttpHeaderCollection:
    ///   - Preserves insertion order
    ///   - Case-sensitive keys (unlike headers, parameter names are case-sensitive)
    ///   - Supports duplicate keys (controlled by DuplicateKeyPolicy)
    ///   - Indexer returns first value for a key (or null if absent)
    ///   - GetValues() returns all values for a key
    /// 
    /// Case sensitivity: query string and form parameter names are case-sensitive
    /// per the WHATWG URL Standard and HTML specification. Cookie names are also
    /// case-sensitive per RFC 6265 Section 5.4. This differs from HTTP header
    /// field names, which are case-insensitive per RFC 9110.
    /// </summary>
    public sealed class HttpContentCollection : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly List<KeyValuePair<string, string>> _ordered;
        private readonly Dictionary<string, List<int>> _index;

        /// <summary>
        /// Creates an empty content collection with case-sensitive keys.
        /// </summary>
        public HttpContentCollection()
        {
            _ordered = new List<KeyValuePair<string, string>>();
            _index = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Creates an empty content collection with the specified key comparer.
        /// </summary>
        internal HttpContentCollection(StringComparer comparer)
        {
            _ordered = new List<KeyValuePair<string, string>>();
            _index = new Dictionary<string, List<int>>(comparer);
        }

        /// <summary>Number of individual key-value entries.</summary>
        public int Count => _ordered.Count;

        /// <summary>
        /// Adds a key-value pair, preserving insertion order.
        /// Duplicate keys are permitted (stored as separate entries).
        /// </summary>
        public void Add(string key, string value)
        {
            int pos = _ordered.Count;
            _ordered.Add(new KeyValuePair<string, string>(key, value));

            if (!_index.TryGetValue(key, out var positions))
            {
                positions = new List<int>();
                _index[key] = positions;
            }
            positions.Add(pos);
        }

        /// <summary>
        /// Adds a key-value pair, respecting the specified duplicate key policy.
        /// </summary>
        /// <param name="key">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="policy">How to handle duplicate keys.</param>
        internal void Add(string key, string value, DuplicateKeyPolicy policy)
        {
            switch (policy)
            {
                case DuplicateKeyPolicy.PreserveAll:
                    Add(key, value);
                    break;

                case DuplicateKeyPolicy.FirstWins:
                    if (!_index.ContainsKey(key))
                        Add(key, value);
                    // else: silently discard duplicate
                    break;

                case DuplicateKeyPolicy.LastWins:
                    if (_index.TryGetValue(key, out var positions))
                    {
                        // Overwrite the existing entry's value
                        int existingPos = positions[positions.Count - 1];
                        _ordered[existingPos] = new KeyValuePair<string, string>(key, value);
                    }
                    else
                    {
                        Add(key, value);
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets the value for a key. If multiple values exist for the key,
        /// returns the first value.
        /// Returns null if the key is not present.
        /// </summary>
        public string this[string key]
        {
            get
            {
                if (!_index.TryGetValue(key, out var positions))
                    return null;
                return _ordered[positions[0]].Value;
            }
        }

        /// <summary>
        /// Gets a key-value pair by its insertion-order index (0-based).
        /// </summary>
        public KeyValuePair<string, string> this[int index] => _ordered[index];

        /// <summary>
        /// Gets all values for a key as separate strings.
        /// Returns an empty array if the key is not present.
        /// </summary>
        public string[] GetValues(string key)
        {
            if (!_index.TryGetValue(key, out var positions))
                return Array.Empty<string>();

            var result = new string[positions.Count];
            for (int i = 0; i < positions.Count; i++)
                result[i] = _ordered[positions[i]].Value;
            return result;
        }

        /// <summary>
        /// Returns true if the collection contains at least one entry with this key.
        /// </summary>
        public bool Contains(string key)
        {
            return _index.ContainsKey(key);
        }

        /// <summary>
        /// Returns all distinct keys in insertion order (first occurrence order).
        /// </summary>
        public string[] AllKeys
        {
            get
            {
                var keys = new List<string>();
                var seen = new HashSet<string>(_index.Comparer);
                foreach (var kvp in _ordered)
                {
                    if (seen.Add(kvp.Key))
                        keys.Add(kvp.Key);
                }
                return keys.ToArray();
            }
        }

        /// <summary>
        /// Returns all distinct keys in insertion order as an enumerable.
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            var seen = new HashSet<string>(_index.Comparer);
            foreach (var kvp in _ordered)
            {
                if (seen.Add(kvp.Key))
                    yield return kvp.Key;
            }
        }

        /// <summary>
        /// Formats the collection as a query string or form body.
        /// Keys and values are NOT percent-encoded — this returns
        /// the decoded representation for display/debugging.
        /// </summary>
        public override string ToString()
        {
            if (_ordered.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _ordered.Count; i++)
            {
                if (i > 0) sb.Append('&');
                sb.Append(_ordered[i].Key);
                sb.Append('=');
                sb.Append(_ordered[i].Value);
            }
            return sb.ToString();
        }

        // ─── IEnumerable ────────────────────────────────────────────

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _ordered.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ─── Static Helpers ─────────────────────────────────────────

        /// <summary>
        /// An empty, shared collection for cases where no parameters exist.
        /// </summary>
        public static readonly HttpContentCollection Empty = new HttpContentCollection();
    }
}
