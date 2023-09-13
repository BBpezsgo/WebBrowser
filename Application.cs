using SDL2;
using static SDL2.SDL;
using static SDL2.SDL_ttf;

namespace WebBrowser
{
    internal class Application : IDisposable
    {
        IntPtr Renderer;
        IntPtr Window;

        int Width;
        int Height;

        bool IsRunning;

        TheWebBrowser WebBrowser;

        public Application()
        {
            Width = 640;
            Height = 480;
            IsRunning = true;
            WebBrowser = new TheWebBrowser(new Vector2Int(Width, Height));
        }

        /// <exception cref="SDLException"/>
        internal void Start()
        {
            if (SDL_Init(SDL_INIT_EVERYTHING) != 0)
            { throw SDLException.GetException(); }
            if (TTF_Init() != 0)
            { throw SDLException.GetException(); }

            Window = SDL_CreateWindow("Bruh", SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED, Width, Height, 0);

            if (Window == IntPtr.Zero)
            { throw SDLException.GetException(); }

            Renderer = SDL_CreateRenderer(Window, -1, 0);

            if (Renderer == IntPtr.Zero)
            { throw SDLException.GetException(); }

            WebBrowser.Initialize(Renderer);

            WebBrowser.LoadPage("http://www.google.com/");

            while (IsRunning)
            {
                HandleEvents();

                WebBrowser.Tick();

                Render();
            }

            Dispose();
        }

        void HandleEvents()
        {
            while (SDL_PollEvent(out SDL_Event @event) != 0)
            {
                switch (@event.type)
                {
                    case SDL_EventType.SDL_QUIT:
                        IsRunning = false;
                        break;
                    default:
                        break;
                }
            }
        }

        void Render()
        {
            SDL_SetRenderDrawColor(Renderer, 0, 0, 0, 255);
            SDL_RenderClear(Renderer);

            WebBrowser.Render();

            SDL_RenderPresent(Renderer);
        }

        public void Dispose()
        {
            WebBrowser.Dispose();

            SDL_DestroyRenderer(Renderer);
            SDL_DestroyWindow(Window);
            TTF_Quit();
            SDL_Quit();
        }
    }
}
