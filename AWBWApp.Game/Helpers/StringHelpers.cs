using System.Text;

namespace AWBWApp.Game.Helpers
{
    public static class StringHelpers
    {
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static string SpaceBeforeCaptials(this string value)
        {
            var sb = new StringBuilder();

            foreach (var character in value)
            {
                if (char.IsUpper(character))
                    sb.Append(' ');
                sb.Append(character);
            }

            return sb.ToString();
        }
    }
}
