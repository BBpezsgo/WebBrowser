using static SDL2.SDL;
using static SDL2.SDL_ttf;

namespace WebBrowser
{
    internal class SDLException : Exception
    {
        SDLException(string message) : base(message) { }
        public static SDLException GetException() => new(SDL_GetError());
        public static SDLException GetTtfException() => new(SDL_GetError());
    }
}
