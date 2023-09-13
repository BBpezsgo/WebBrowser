using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebBrowser
{
    public struct Vector2
    {
        public float X;
        public float Y;

        public static Vector2 Zero => new(0, 0);

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public readonly Vector2Int RountToInt() => new(Utils.RoundToInt(X), Utils.RoundToInt(Y));

        public static Vector2 operator +(Vector2 a, Vector2 b)
            => new(a.X + b.X, a.Y + b.Y);

        public static Vector2 operator -(Vector2 a, Vector2 b)
            => new(a.X - b.X, a.Y - b.Y);

        public static Vector2 operator *(Vector2 a, float b)
            => new(a.X * b, a.Y * b);

        public static Vector2 operator /(Vector2 a, float b)
            => new(a.X / b, a.Y / b);
    }

    public struct Vector2Int
    {
        public int X;
        public int Y;

        public static Vector2Int Zero => new(0, 0);

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly Vector2 ToFloat() => new(X, Y);

        public static Vector2Int operator +(Vector2Int a, Vector2Int b)
            => new(a.X + b.X, a.Y + b.Y);

        public static Vector2Int operator -(Vector2Int a, Vector2Int b)
            => new(a.X - b.X, a.Y - b.Y);

        public static Vector2Int operator *(Vector2Int a, int b)
            => new(a.X * b, a.Y * b);

        public static Vector2Int operator /(Vector2Int a, int b)
            => new(a.X / b, a.Y / b);

        public static implicit operator SDL2.SDL.SDL_Point(Vector2Int v) => new()
        {
            x = v.X,
            y = v.Y,
        };
    }
}
