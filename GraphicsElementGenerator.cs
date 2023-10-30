using System.Linq;
using BoxLayout;
using HtmlAgilityPack;
using ProgrammingLanguage.Css;
using WebBrowser;

namespace GraphicsElementGenerator
{
    public delegate bool ImageSizeGetter(string url, out Vector2Int size);

    [System.Diagnostics.DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public abstract class Element
    {
        public readonly CachedDimensions Dimensions;

        protected Element(LayoutBox box)
        {
            Dimensions = box.Dimensions.Cached;
        }

        public abstract ElementKind Kind { get; }

        string GetDebuggerDisplay() => ToString();

        public override string ToString() => Kind.ToString();
    }

    public class ElementLabel : Element
    {
        public string Text;
        public Color Color;

        public string? Link;
        public ushort LinkID;

        public ElementLabel(LayoutBox box, string text, Color color) : base(box)
        {
            Text = text;
            Color = color;
        }

        public override ElementKind Kind => ElementKind.Text;

        public sealed override string ToString() => $"{base.ToString()} \"{Text}\"";
    }

    public abstract class ElementWithID : Element
    {
        public ushort ID;

        protected ElementWithID(LayoutBox box) : base(box)
        {
        }

        public override string ToString() => $"{base.ToString()}#{ID}";
    }

    public abstract class ElementFocusable : ElementWithID
    {
        protected ElementFocusable(LayoutBox box) : base(box)
        {
        }
    }

    public class ElementButton : ElementFocusable
    {
        public string Text;
        public override ElementKind Kind => ElementKind.Button;
        public ElementForm? Form;

        public ElementButton(LayoutBox box, string? text, ushort id) : base(box)
        {
            Text = text ?? string.Empty;
            ID = id;
        }

        public sealed override string ToString() => $"{base.ToString()} {{ Text: \"{Text}\" }}";
    }

    public class ElementImage : Element
    {
        public string? Url;
        public byte ImageID;

        public ElementImage(LayoutBox box) : base(box)
        {
        }

        public override ElementKind Kind => ElementKind.Image;

        public sealed override string ToString() => $"{base.ToString()} {{ Url: \"{Url}\" ImageID: {ImageID} }}";
    }

    public class ElementTextField : ElementFocusable
    {
        public string? Name;
        internal TextInputField Manager;
        public override ElementKind Kind => ElementKind.InputText;
        public ElementForm? Form;

        public ElementTextField(LayoutBox box, string? value, ushort id) : base(box)
        {
            Name = null;
            Manager = new TextInputField(value);
            ID = id;
        }

        public sealed override string ToString() => $"{base.ToString()} {{ Name: \"{Name}\" Value: \"{Manager?.Buffer}\" }}";
    }

    public class ElementSelect : ElementFocusable
    {
        public (string Value, string Label)[] Values;
        public int SelectedIndex;

        public ElementSelect(LayoutBox box, ushort id) : base(box)
        {
            Values = Array.Empty<(string Value, string Label)>();
            ID = id;
        }

        public (string Value, string Label)? Selected => (SelectedIndex < 0 || SelectedIndex > Values.Length) ? null : Values[SelectedIndex];
        public string? Label => (SelectedIndex < 0 || SelectedIndex > Values.Length) ? null : Values[SelectedIndex].Label;

        public override ElementKind Kind => ElementKind.Select;

        public sealed override string ToString() => $"{base.ToString()} {{ SelectedIndex: {SelectedIndex} }}";
    }

    public class ElementForm : ElementWithID
    {
        public override ElementKind Kind => ElementKind.Form;
        public bool Submitted;
        public bool ShouldSubmit;
        internal string Method;
        internal string Target;

        public ElementForm(LayoutBox box, ushort id, string target, string method) : base(box)
        {
            ID = id;
            Target = target;
            Method = method;
        }

        public sealed override string ToString() => $"{base.ToString()} {{ Method: \"{Method}\" Target: \"{Target}\" }}";
    }

    public enum ElementKind
    {
        Text,
        Button,
        InputText,
        Form,
        Image,
        Select,
    }

    readonly struct GeneratorUtils
    {
        public static readonly string[] SupportedTags = new string[]
        {
            "div",
            "p",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "b",
            "u",
            "html",
            "body",
            "a",
            "center",
            "br",
            "span",
        };

        public static string? ConvertHtmlText(string text)
        {
            if (text is null) return null;
            return System.Web.HttpUtility.HtmlDecode(text);
        }

        public static Color FixColor(Color color)
        {
            return color;
            /*
            if (color.grayscale >= .5f) return color;
            Color.RGBToHSV(color, out float h, out float s, out float v);
            v = Mathf.Clamp(v, .5f, 1f);
            return Color.HSVToRGB(h, s, v);
            */
        }

    }

    public class Generator
    {
        public class NeedThisImage
        {
            public string? Url;
            public byte ID;
            public Vector2Int DownloadedSize;
        }

        public readonly List<NeedThisImage> NeedTheseImages;
        public readonly List<ElementForm> Forms;
        public readonly List<Stylesheet> Stylesheets;
        public readonly List<Element> Elements;

        TextMeasurer MeasureText;
        ImageSizeGetter? GetImageSize;

        ushort ElementIDCounter;
        byte ImageIDCounter;
        ushort LinkIDCounter;

        public RectInt PageArea;
        Vector2Int overflow;

        public Vector2Int Overflow => overflow;
        public int OverflowX => overflow.X;
        public int OverflowY => overflow.Y;

        public Vector2Int PageSize => overflow + PageArea.Size;
        public int PageWidth => PageSize.X;
        public int PageHeight => PageSize.Y;

        readonly Stack<string> LinkStack;
        readonly Stack<ElementForm> FormStack;
        readonly Stack<Declaration[]> StyleStack;

        static readonly string[] inheritableStyleProperties = new string[]
        {
            "color",
        };

        public Generator()
        {
            LinkStack = new Stack<string>();
            FormStack = new Stack<ElementForm>();
            StyleStack = new Stack<Declaration[]>();

            NeedTheseImages = new List<NeedThisImage>();

            Stylesheets = new List<Stylesheet>();

            Forms = new List<ElementForm>();
            Elements = new List<Element>();

            ElementIDCounter = 0;

            PageArea = default;
            overflow = Vector2Int.Zero;

            MeasureText = MeasureTextDefault;
            GetImageSize = null;
        }

        public void Reset()
        {
            LinkStack.Clear();
            FormStack.Clear();
            StyleStack.Clear();

            NeedTheseImages.Clear();

            Stylesheets.Clear();

            Forms.Clear();
            Elements.Clear();

            ElementIDCounter = 0;
            ImageIDCounter = 0;
            LinkIDCounter = 0;

            PageArea = default;
            overflow = Vector2Int.Zero;

            MeasureText = MeasureTextDefault;
            GetImageSize = null;
        }

        static Vector2Int MeasureTextDefault(string text, int fontSize) => Vector2Int.Zero;

        NeedThisImage GetOrCreateImage(string url)
        {
            foreach (var image in NeedTheseImages)
            {
                if (string.Equals(image.Url, url, StringComparison.InvariantCulture))
                {
                    return image;
                }
            }

            ImageIDCounter++;
            NeedThisImage newImage = new()
            {
                Url = url,
                ID = ImageIDCounter,
                DownloadedSize = Vector2Int.Zero,
            };
            NeedTheseImages.Add(newImage);
            return newImage;
        }

        public ElementWithID? GetElementByID(ushort id)
        {
            foreach (Element element in Elements)
            {
                if (element is ElementWithID elementFocusable && elementFocusable.ID == id)
                {
                    return elementFocusable;
                }
            }
            return null;
        }
        public Element[] GetFormElements(ElementForm form)
        {
            List<Element> result = new();
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i] is ElementButton button)
                {
                    if (button.Form == null) continue;
                    if (button.Form.ID == form.ID)
                    {
                        result.Add(button);
                        continue;
                    }
                }
                else if (Elements[i] is ElementTextField textField)
                {
                    if (textField.Form == null) continue;
                    if (textField.Form.ID == form.ID)
                    {
                        result.Add(textField);
                        continue;
                    }
                }
            }
            return result.ToArray();
        }

        public void GenerateLayout(HtmlDocument document, TextMeasurer textMeasurer, ImageSizeGetter imageSizeGetter)
        {
            MeasureText = textMeasurer;
            ElementIDCounter = 1;
            Elements.Clear();
            Forms.Clear();
            overflow = Vector2Int.Zero;

            LayoutBox root = BoxLayoutGenerator.LayoutDocument(document, Stylesheets, PageArea, textMeasurer, imageSizeGetter);

            overflow = root.Dimensions.MarginRect.Size - PageArea.Size;

            GenerateElement(root, DeclarationsContainer.Empty);
        }

        void GenerateElement(LayoutBox layoutBox, DeclarationsContainer currentStyles)
        {
            List<Declaration> collectedStyles = new(currentStyles);

            foreach (Stylesheet stylesheet in Stylesheets)
            { collectedStyles.AddRange(stylesheet.GetMatches(layoutBox.Node)); }

            DeclarationsContainer newStyles = new(collectedStyles);

            Action? after;

            switch (layoutBox.Node.NodeType)
            {
                case HtmlNodeType.Document:
                    GenerateElementForChilds(layoutBox, newStyles);
                    return;
                case HtmlNodeType.Element:
                    bool shouldBreak = GenerateElementForElement(layoutBox, newStyles, out after);
                    if (shouldBreak) return;
                    break;
                case HtmlNodeType.Comment:
                    return;
                case HtmlNodeType.Text:
                    GenerateElementForText(layoutBox, newStyles);
                    return;
                default:
                    return;
            }

            GenerateElementForChilds(layoutBox, newStyles);

            after?.Invoke();
        }

        void GenerateElementForChilds(LayoutBox layoutBox, DeclarationsContainer styles)
        {
            Declaration[] inheritingStyles = styles.GetDeclarations(inheritableStyleProperties);

            foreach (LayoutBox child in layoutBox.Childrens)
            { GenerateElement(child, new DeclarationsContainer(inheritingStyles)); }
        }

        void GenerateElementForText(LayoutBox layoutBox, DeclarationsContainer style)
        {
            if (layoutBox.Node.NodeType != HtmlNodeType.Text) return;

            string? text = GeneratorUtils.ConvertHtmlText(layoutBox.Node.InnerText);
            if (string.IsNullOrWhiteSpace(text)) return;

            string? link = null;
            if (LinkStack.Count > 0)
            { link = LinkStack.Peek(); }
            Color defaultColor = Color.White;

            if (!string.IsNullOrWhiteSpace(link))
            { defaultColor = Color.Blue; }

            ElementLabel element = new(layoutBox, text, GeneratorUtils.FixColor(style.GetColor("color") ?? defaultColor));
            if (!string.IsNullOrWhiteSpace(link))
            {
                element.LinkID = LinkIDCounter;
                element.Link = link;
            }
            Elements.Add(element);
        }

        bool GenerateElementForElement(LayoutBox layoutBox, DeclarationsContainer styles, out Action? after)
        {
            after = null;

            if (layoutBox.Node.NodeType != HtmlNodeType.Element) return true;

            if (layoutBox.Node.Name == "button")
            {
                Elements.Add(new ElementButton(layoutBox, layoutBox.Node.InnerText, ElementIDCounter++));

                return true;
            }

            if (layoutBox.Node.Name == "img")
            {
                string src = layoutBox.Node.GetAttributeValue("src", string.Empty);
                NeedThisImage image = GetOrCreateImage(src);

                int width = layoutBox.Node.GetAttributeValue("width", image.DownloadedSize.X);
                int height = layoutBox.Node.GetAttributeValue("height", image.DownloadedSize.Y);

                var d = layoutBox.Dimensions;
                d.Content.Width = width;
                d.Content.Height = height;

                Elements.Add(new ElementImage(layoutBox)
                {
                    Url = src,
                    ImageID = image.ID,
                });

                return true;
            }

            if (layoutBox.Node.Name == "input")
            {
                string inputType = layoutBox.Node.GetAttributeValue("type", null);
                if (inputType == "text" || string.IsNullOrWhiteSpace(inputType))
                {
                    string? text = GeneratorUtils.ConvertHtmlText(layoutBox.Node.GetAttributeValue("value", ""));

                    Elements.Add(new ElementTextField(layoutBox, text, ElementIDCounter++)
                    {
                        Form = FormStack.PeekOrDefault(),
                        Name = layoutBox.Node.GetAttributeValue("name", string.Empty),
                    });

                    return true;
                }

                if (inputType == "submit")
                {
                    string? text = GeneratorUtils.ConvertHtmlText(layoutBox.Node.GetAttributeValue("value", "Submit"));

                    Elements.Add(new ElementButton(layoutBox, text, ElementIDCounter++)
                    {
                        Form = FormStack.PeekOrDefault(),
                    });

                    return true;
                }

                Debug.Log($"Unknown input type \"{inputType}\"");
                return true;
            }

            if (layoutBox.Node.Name == "select")
            {
                List<(string Value, string Label)> values = new();

                int longest = 16;

                foreach (HtmlNode child in layoutBox.Node.ChildNodes)
                {
                    if (child.Name != "option") continue;
                    string value = child.GetAttributeValue("value", null);
                    string? label = GeneratorUtils.ConvertHtmlText(child.InnerText)?.Trim();

                    if (string.IsNullOrWhiteSpace(label)) continue;
                    values.Add((value, label));

                    longest = Math.Max(longest, MeasureText.Invoke(label, 8).X);
                }

                Elements.Add(new ElementSelect(layoutBox, ElementIDCounter++)
                {
                    Values = values.ToArray(),
                    SelectedIndex = 0,
                });

                return true;
            }

            if (layoutBox.Node.Name == "form")
            {
                ElementForm newForm = new(layoutBox, ElementIDCounter++, layoutBox.Node.GetAttributeValue("target", "./"), layoutBox.Node.GetAttributeValue("method", "POST"));
                Elements.Add(newForm);
                Forms.Add(newForm);

                FormStack.Push(newForm);

                after = new Action(() => FormStack.Pop());

                return false;
            }

            if (layoutBox.Node.Name == "a")
            {
                LinkIDCounter++;
                LinkStack.Push(layoutBox.Node.GetAttributeValue("href", null));
                after = new Action(() => LinkStack.Pop());
            }

            if (!GeneratorUtils.SupportedTags.Contains(layoutBox.Node.Name, StringComparison.InvariantCulture))
            { Debug.Log($"[{nameof(GraphicsElementGenerator)}/{nameof(Generator)}]: Unknown tag \"{layoutBox.Node.Name}\""); }

            return false;
        }
    }
}
