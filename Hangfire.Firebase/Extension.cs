namespace Hangfire.Firebase
{
    internal static class Extension
    {
        internal static bool IsNull(this FireSharp.Response.FirebaseResponse response) => response.Body.Equals("null", System.StringComparison.InvariantCultureIgnoreCase);
    }
}
