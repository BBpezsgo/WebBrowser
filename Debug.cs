namespace WebBrowser
{
    internal static class Debug
    {
        internal static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        internal static void LogWarning(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }
    }
}
