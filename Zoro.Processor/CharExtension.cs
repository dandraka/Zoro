using System;

namespace Zoro.Processor
{
    public static class CharExtension
    {
        private static Random rnd = new Random(DateTime.Now.Millisecond);

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

        public static bool IsConsonant(this Char c)
        {
            return !c.IsVowel();
        }

        public static Char GetRandomVowel()
        {
            Char[] vowels = new Char[] { 'e', 'a', 'o', 'i', 'u' /*, 'ä', 'ö', 'ü'*/ };
            return vowels[rnd.Next(0, vowels.Length)];
        }

        public static Char GetRandomConsonant()
        {
            Char[] consonants = new Char[] { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z' };
            return consonants[rnd.Next(0, consonants.Length)];
        }
    }
}