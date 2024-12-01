namespace Minimal.Mvvm.SourceGenerator
{
    internal static class TextUtils
    {
        public static (string leadingWhitespace, int leadingWhitespaceLength) GetLeadingWhitespace(string line)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < line.Length; i++)
            {
                if (char.IsWhiteSpace(line[i])) continue;
                leadingWhitespace = line.Substring(0, i);
                break;
            }
            return (leadingWhitespace, leadingWhitespace.Length);
        }

        public static int GetSpaceCount(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (char.IsWhiteSpace(line[i])) continue;
                return i;
            }
            return 0;
        }
    }
}
