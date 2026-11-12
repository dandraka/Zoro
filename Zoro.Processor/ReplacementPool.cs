using System;
using System.Collections.Generic;
using System.IO;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Remembers which replacement every original value of one <c>Replacement</c> got,
    /// so that masking is consistent.
    /// </summary>
    internal class ReplacementPool
    {
        private readonly Random rnd;
        private readonly Dictionary<string, string> assigned = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> withoutReplacement = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> unused = new List<string>();
        private readonly List<string> reuseCandidates = new List<string>();

        internal ReplacementPool(Replacement replacement, Random rnd)
        {
            this.rnd = rnd;

            var mappedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var map in replacement.Mappings ?? new List<ReplacementMap>())
            {
                string from = map.From?.Trim();
                if (string.IsNullOrEmpty(from))
                {
                    throw new InvalidDataException($"A Map without a From value was found (selector '{replacement.Selector}').");
                }
                if (assigned.ContainsKey(from))
                {
                    throw new InvalidDataException($"The same From value is mapped more than once (selector '{replacement.Selector}').");
                }

                string to = map.To ?? string.Empty;
                assigned.Add(from, to);
                mappedTargets.Add(to.Trim());
            }

            IEnumerable<string> items = replacement.Items != null
                ? (IEnumerable<string>)replacement.Items
                : (replacement.ReplacementList ?? string.Empty).Split(',');
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var poolItems = new List<string>();
            foreach (string item in items)
            {
                string trimmed = item?.Trim();
                if (string.IsNullOrEmpty(trimmed) || !seen.Add(trimmed))
                {
                    continue;
                }

                poolItems.Add(trimmed);
                // replacements that are given explicitly must not be handed out a second time
                if (!mappedTargets.Contains(trimmed))
                {
                    unused.Add(trimmed);
                }
            }

            reuseCandidates.AddRange(poolItems.Count > 0 ? poolItems : new List<string>(mappedTargets));
        }

        /// <summary>The number of distinct values that were masked with asterisks.</summary>
        internal int FallbackCount => withoutReplacement.Count;

        /// <summary>
        /// Gets the replacement of a value, assigning a new one if the value is seen for the first time.
        /// </summary>
        /// <returns>False if there is no replacement left.</returns>
        internal bool TryAssign(string value, bool allowReuse, out string replacement)
        {
            string key = value.Trim();
            if (assigned.TryGetValue(key, out replacement))
            {
                return true;
            }

            if (unused.Count > 0)
            {
                int index = rnd.Next(unused.Count);
                replacement = unused[index];
                unused.RemoveAt(index);
                assigned.Add(key, replacement);
                return true;
            }

            if (allowReuse)
            {
                if (reuseCandidates.Count == 0)
                {
                    throw new InvalidDataException("OnListExhausted is Reuse but no replacement items or mappings are configured.");
                }
                replacement = reuseCandidates[rnd.Next(reuseCandidates.Count)];
                assigned.Add(key, replacement);
                return true;
            }

            replacement = null;
            return false;
        }

        internal void RegisterFallback(string value)
        {
            withoutReplacement.Add(value.Trim());
        }
    }
}
