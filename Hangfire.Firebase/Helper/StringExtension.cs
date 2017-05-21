using System.Text.RegularExpressions;

namespace Hangfire.Firebase.Helper
{
    internal static class StringExtension
    {
        private static readonly Regex VALID_KEY = new Regex(@"[\.|#|$|\[|\]|\/]", RegexOptions.Compiled);

        internal static string ToValidKey(this string key) => VALID_KEY.Replace(key, "_");
    }
}
