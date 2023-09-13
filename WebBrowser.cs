using GraphicsElementGenerator;
using HtmlAgilityPack;
using ProgrammingLanguage.Css;
using static SDL2.SDL;
using static SDL2.SDL_ttf;

namespace WebBrowser
{
    class DownloadTask : IDisposable
    {
        readonly HttpClient Client;
        readonly HttpRequestMessage RequestMessage;

        readonly Action<DownloadTask, HttpResponseMessage> Callback;
        Action<string>? ContentCallback;
        HttpResponseMessage? ResponseMessage;

        Task<HttpResponseMessage>? RequestTask;
        Task<string>? ContentRequestTask;

        DownloadTask(HttpClient client, HttpRequestMessage requestMessage, Action<DownloadTask, HttpResponseMessage> callback)
        {
            Client = client;
            RequestMessage = requestMessage;
            Callback = callback;
        }

        public static DownloadTask Request(HttpMethod method, Uri uri, Action<DownloadTask, HttpResponseMessage> callback)
            => DownloadTask.Request(new HttpClient(), method, uri, callback);
        public static DownloadTask Request(HttpClient client, HttpMethod method, Uri uri, Action<DownloadTask, HttpResponseMessage> callback)
        {
            DownloadTask task = new(client, new HttpRequestMessage(method, uri), callback);
            task.RequestHeader();
            return task;
        }

        void RequestHeader()
        {
            Console.WriteLine($"[HTTP]: {RequestMessage.Method} headers from {RequestMessage.RequestUri}");
            RequestTask = Client.SendAsync(RequestMessage, HttpCompletionOption.ResponseHeadersRead);
        }

        public void RequestContent(Action<string> callback)
        {
            if (ResponseMessage == null)
            { throw new InvalidOperationException($"{nameof(ResponseMessage)} is null"); }

            ContentCallback = callback;
            Console.WriteLine($"[HTTP]: {RequestMessage.Method} contents from {RequestMessage.RequestUri}");
            ContentRequestTask = ResponseMessage.Content.ReadAsStringAsync();
        }

        public bool Tick()
        {
            if (RequestTask != null)
            {
                if (RequestTask.IsCompletedSuccessfully)
                {
                    ResponseMessage = RequestTask.Result;

                    Console.WriteLine($"[HTTP]: Response received ({ResponseMessage.StatusCode}) from {RequestMessage.RequestUri}");

                    Callback?.Invoke(this, ResponseMessage);

                    try { RequestTask.Dispose(); }
                    catch (Exception ex) { System.Diagnostics.Debug.Fail(ex.GetType().Name, ex.ToString()); }

                    RequestTask = null;
                }

                return false;
            }

            if (ContentRequestTask != null)
            {
                if (ContentRequestTask.IsCompletedSuccessfully)
                {
                    Console.WriteLine($"[HTTP]: Content received ({ContentRequestTask.Result.Length * sizeof(char)} bytes) from {RequestMessage.RequestUri}");

                    ContentCallback?.Invoke(ContentRequestTask.Result);

                    try { ContentRequestTask.Dispose(); }
                    catch (Exception ex) { System.Diagnostics.Debug.Fail(ex.GetType().Name, ex.ToString()); }

                    ContentRequestTask = null;
                }

                return false;
            }

            return true;
        }

        public void Dispose()
        {
            try { RequestTask?.Dispose(); }
            catch (Exception ex) { System.Diagnostics.Debug.Fail(ex.GetType().Name, ex.ToString()); }

            try { ContentRequestTask?.Dispose(); }
            catch (Exception ex) { System.Diagnostics.Debug.Fail(ex.GetType().Name, ex.ToString()); }
        }
    }

    class FontInstance : IDisposable
    {
        public readonly IntPtr Font;
        public readonly int FontSize;

        public FontInstance(IntPtr font, int fontSize)
        {
            Font = font;
            FontSize = fontSize;
        }

        public void Dispose()
        {
            TTF_CloseFont(Font);
        }

        /// <exception cref="SDLException"/>
        public static FontInstance Create(string ttfPath, int fontSize)
        {
            IntPtr font = TTF_OpenFont(ttfPath, fontSize);
            if (font == IntPtr.Zero) throw SDLException.GetTtfException();
            return new FontInstance(font, fontSize);
        }
    }

    class TextInstance : IDisposable
    {
        readonly IntPtr Renderer;
        readonly IntPtr Font;
        readonly IntPtr TextSurface;
        readonly IntPtr TextTexture;

        public readonly int Width;
        public readonly int Height;

        public readonly string Text;
        public readonly SDL_Color Color;
        public readonly int FontSize;

        public IntPtr Texture => TextTexture;

        public unsafe TextInstance(IntPtr renderer, IntPtr font, int fontSize, string text, SDL_Color color)
        {
            Renderer = renderer;
            Font = font;
            Text = text;
            Color = color;
            FontSize = fontSize;

            TextSurface = TTF_RenderText_Solid(Font, text, color);
            Width = ((SDL_Surface*)TextSurface.ToPointer())->w;
            Height = ((SDL_Surface*)TextSurface.ToPointer())->h;

            TextTexture = SDL_CreateTextureFromSurface(Renderer, TextSurface);
        }

        public void Dispose()
        {
            SDL_DestroyTexture(TextTexture);
            SDL_FreeSurface(TextSurface);
        }
    }

    public class TheWebBrowser : IDisposable
    {
        readonly HttpClient HttpClient;
        readonly List<DownloadTask> Tasks;
        readonly Generator GraphicsElementGenerator;
        readonly List<TextInstance> TextInstances;
        readonly List<FontInstance> FontInstances;
        readonly Stylesheet DefaultStylesheet;

        IntPtr Renderer;

        public TheWebBrowser(Vector2Int size)
        {
            HttpClient = new HttpClient();
            Tasks = new List<DownloadTask>();
            GraphicsElementGenerator = new Generator()
            {
                PageArea = new RectInt(0, 0, size.X, size.Y),
            };

            TextInstances = new List<TextInstance>();
            FontInstances = new List<FontInstance>();

            DefaultStylesheet = new Parser().Parse(File.ReadAllText("C:\\Users\\bazsi\\source\\repos\\WebBrowser\\DefaultStylesheet.css"));
        }

        public void Tick()
        {
            for (int i = Tasks.Count - 1; i >= 0; i--)
            {
                DownloadTask task = Tasks[i];
                bool done = task.Tick();
                if (done) Tasks.RemoveAt(i);
            }
        }

        public void Render()
        {
            /*
            {
                TextInstance textInstance = GetTextInstance("Bruh");
                SDL_Rect dest = new()
                {
                    x = 0,
                    y = 0,
                    w = textInstance.Width,
                    h = textInstance.Height,
                };
                SDL_RenderCopy(Renderer, textInstance.Texture, IntPtr.Zero, ref dest);
            }
            */

            if (GraphicsElementGenerator.Elements.Count == 0) return;
            Element[] elements = GraphicsElementGenerator.Elements.ToArray();

            for (int i = 0; i < elements.Length; i++)
            {
                Element element = elements[i];

                switch (element.Kind)
                {
                    case ElementKind.Text:
                        {
                            ElementLabel elementLabel = (ElementLabel)element;
                            TextInstance? textInstance = GetTextInstance(elementLabel.Text, 8, elementLabel.Color);
                            if (textInstance != null)
                            {
                                SDL_Rect dest = new()
                                {
                                    x = elementLabel.Dimensions.Content.X,
                                    y = elementLabel.Dimensions.Content.Y,
                                    w = textInstance.Width,
                                    h = textInstance.Height,
                                };
                                SDL_RenderCopy(Renderer, textInstance.Texture, IntPtr.Zero, ref dest);
                            }
                            break;
                        }
                    case ElementKind.Button:
                        {
                            ElementButton elementButton = (ElementButton)element;
                            SDL_Rect rect = elementButton.Dimensions.Content;
                            SDL_SetRenderDrawColor(Renderer, 255, 255, 255, 255);
                            SDL_RenderDrawRect(Renderer, ref rect);

                            TextInstance? textInstance = GetTextInstance(elementButton.Text, 8, Color.Black);
                            if (textInstance != null)
                            {
                                SDL_Rect dest = new()
                                {
                                    x = elementButton.Dimensions.Content.X,
                                    y = elementButton.Dimensions.Content.Y,
                                    w = textInstance.Width,
                                    h = textInstance.Height,
                                };
                                SDL_RenderCopy(Renderer, textInstance.Texture, IntPtr.Zero, ref dest);
                            }

                            break;
                        }
                    case ElementKind.InputText:
                        {
                            ElementTextField elementTextField = (ElementTextField)element;

                            SDL_SetRenderDrawColor(Renderer, 255, 255, 255, 255);
                            SDL_Point[] points = new SDL_Point[]
                            {
                                elementTextField.Dimensions.BorderRect.TopLeft,
                                elementTextField.Dimensions.BorderRect.TopRight,
                                elementTextField.Dimensions.BorderRect.BottomRight,
                                elementTextField.Dimensions.BorderRect.BottomLeft,
                            };
                            SDL_RenderDrawLines(Renderer, points, points.Length);

                            TextInstance? textInstance = GetTextInstance(elementTextField.Manager.Buffer, 8, Color.White);
                            if (textInstance != null)
                            {
                                SDL_Rect dest = new()
                                {
                                    x = elementTextField.Dimensions.Content.X,
                                    y = elementTextField.Dimensions.Content.Y,
                                    w = textInstance.Width,
                                    h = textInstance.Height,
                                };
                                SDL_RenderCopy(Renderer, textInstance.Texture, IntPtr.Zero, ref dest);
                            }

                            break;
                        }
                    case ElementKind.Form:
                        {
                            ElementForm elementForm = (ElementForm)element;
                            break;
                        }
                    case ElementKind.Image:
                        {
                            ElementImage elementImage = (ElementImage)element;
                            break;
                        }
                    case ElementKind.Select:
                        {
                            ElementSelect elementSelect = (ElementSelect)element;

                            SDL_SetRenderDrawColor(Renderer, 255, 255, 255, 255);
                            SDL_Point[] points = new SDL_Point[]
                            {
                                elementSelect.Dimensions.BorderRect.TopLeft,
                                elementSelect.Dimensions.BorderRect.TopRight,
                                elementSelect.Dimensions.BorderRect.BottomRight,
                                elementSelect.Dimensions.BorderRect.BottomLeft,
                            };
                            SDL_RenderDrawLines(Renderer, points, points.Length);

                            TextInstance? textInstance = GetTextInstance(elementSelect.Label, 8, Color.White);
                            if (textInstance != null)
                            {
                                SDL_Rect dest = new()
                                {
                                    x = elementSelect.Dimensions.Content.X,
                                    y = elementSelect.Dimensions.Content.Y,
                                    w = textInstance.Width,
                                    h = textInstance.Height,
                                };
                                SDL_RenderCopy(Renderer, textInstance.Texture, IntPtr.Zero, ref dest);
                            }

                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }

        TextInstance? GetTextInstance(string text, int fontSize, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            foreach (TextInstance textInstance in TextInstances)
            {
                if (!string.Equals(textInstance.Text, text)) continue;
                if (textInstance.FontSize != fontSize) continue;
                if (textInstance.Color != color) continue;
                return textInstance;
            }

            FontInstance fontInstance = GetFontInstance(fontSize);

            TextInstance newInstance = new(Renderer, fontInstance.Font, fontSize, text, color);
            TextInstances.Add(newInstance);
            return newInstance;
        }

        TextInstance? GetTextInstance(string text, int fontSize)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            foreach (TextInstance textInstance in TextInstances)
            {
                if (!string.Equals(textInstance.Text, text)) continue;
                if (textInstance.FontSize != fontSize) continue;
                return textInstance;
            }

            FontInstance fontInstance = GetFontInstance(fontSize);

            TextInstance newInstance = new(Renderer, fontInstance.Font, fontSize, text, Color.White);
            TextInstances.Add(newInstance);
            return newInstance;
        }

        FontInstance GetFontInstance(int fontSize)
        {
            foreach (FontInstance fontInstance in FontInstances)
            {
                if (fontInstance.FontSize != fontSize) continue;
                return fontInstance;
            }

            FontInstance newInstance = FontInstance.Create("C:\\Users\\bazsi\\source\\repos\\WebBrowser\\Font.ttf", fontSize);
            FontInstances.Add(newInstance);
            return newInstance;
        }

        public void LoadPage(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            { throw new FormatException(); }

            Utils.DisposeAndClear(Tasks);

            Tasks.Add(DownloadTask.Request(HttpClient, HttpMethod.Get, uri, OnPageDownloaded));
        }

        void OnPageDownloaded(DownloadTask task, HttpResponseMessage message)
        {
            message.EnsureSuccessStatusCode();
            task.RequestContent(OnPageDownloaded);
        }

        void OnPageDownloaded(string html)
        {
            HtmlDocument document = new();
            document.LoadHtml(html);

            Utils.DisposeAndClear(TextInstances);
            Utils.DisposeAndClear(FontInstances);

            GraphicsElementGenerator.Reset();
            GraphicsElementGenerator.Stylesheets.Add(DefaultStylesheet);
            GraphicsElementGenerator.GenerateLayout(document, MeasureText, MeasureImage);
        }

        Vector2Int MeasureText(string text, int fontSize)
        {
            TextInstance? textInstance = GetTextInstance(text, fontSize);
            if (textInstance == null) return Vector2Int.Zero;
            return new Vector2Int(textInstance.Width, textInstance.Height);
        }

        bool MeasureImage(string url, out Vector2Int size)
        {
            size = default;
            return false;
        }

        public void Dispose()
        {
            Utils.DisposeAndClear(Tasks);
            Utils.DisposeAndClear(TextInstances);
            Utils.DisposeAndClear(FontInstances);

            HttpClient.Dispose();
        }

        internal unsafe void Initialize(IntPtr renderer)
        {
            Renderer = renderer;
        }
    }
}
