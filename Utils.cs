namespace WebBrowser
{
    internal struct Utils
    {
        public static int RoundToInt(float v) => (int)MathF.Round(v);
        public static Vector2Int RoundToInt(Vector2 v) => new(Utils.RoundToInt(v.X), Utils.RoundToInt(v.Y));

        public static void DisposeStuff(IEnumerable<IDisposable> list)
        {
            foreach (IDisposable item in list)
            { item.Dispose(); }
        }

        public static void DisposeAndClear<T>(ICollection<T> list) where T : IDisposable
        {
            foreach (T item in list)
            { item.Dispose(); }
            list.Clear();
        }
    }

    public static class Extensions
    {
        public static T? PeekOrDefault<T>(this Stack<T> stack, T? @default = default)
        {
            if (stack.TryPeek(out T? result))
            { return result; }
            return @default;
        }

        internal static bool Contains(this string[] self, string v, StringComparison comparison = StringComparison.InvariantCulture)
        {
            for (int i = 0; i < self.Length; i++)
            {
                if (string.Equals(self[i], v, comparison)) return true;
            }
            return false;
        }
    }
}
