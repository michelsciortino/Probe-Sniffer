namespace Core.Utilities
{
    public static class Utilities
    {
        /// <summary>
        /// Stretches a string to <paramref name="lenght"/>.
        /// </summary>
        /// <param name="original">Original string</param>
        /// <param name="lenght">Desired lenght</param>
        /// <returns>The original string stretched to <paramref name="lenght"/> with " ".</returns>
        public static string Stretch(string original, int lenght)
        {
            string output = original;
            for (int i = original.Length; i < lenght; i++)
                output += " ";
            return output;
        }
    }
}
