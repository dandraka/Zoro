using System;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Utility class that enables random character generation.
    /// </summary>
    public static class CharExtension
    {
        private static readonly Random rnd = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// Returns true if the character is a vowel.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns></returns>
        public static bool IsVowel(this Char c)
        {
            return c == 'e' ||
                   c == 'a' ||
                   c == 'o' ||
                   c == 'i' ||
                   c == 'u' ||
                   c == 'y' ||
                   c == 'ä' ||
                   c == 'ö' ||
                   c == 'ü';
        }

        /// <summary>
        /// Returns true if the character is a consonant.
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns></returns>
        public static bool IsConsonant(this Char c)
        {
            return !c.IsVowel();
        }

        /// <summary>
        /// Returns a random vowel.
        /// </summary>
        /// <returns>A vowel character.</returns>
        public static Char GetRandomVowel()
        {
            Char[] vowels = new Char[] { 'e', 'a', 'o', 'i', 'u' /*, 'ä', 'ö', 'ü'*/ };
            return vowels[rnd.Next(0, vowels.Length)];
        }

        /// <summary>
        /// Returns a random consonant.
        /// </summary>
        /// <returns>A consonant character.</returns>
        public static Char GetRandomConsonant()
        {
            Char[] consonants = new Char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z' };
            return consonants[rnd.Next(0, consonants.Length)];
        }
    }
}