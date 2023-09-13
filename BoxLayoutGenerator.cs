using System.Diagnostics;
using System.Numerics;
using GraphicsElementGenerator;
using HtmlAgilityPack;
using ProgrammingLanguage.Css;
using WebBrowser;
using Debug = WebBrowser.Debug;

namespace BoxLayout
{
    public static class NodeUtils
    {
        public static string TakeText(LayoutBox node)
            => NodeUtils.TakeText(node.Node);
        public static string TakeText(HtmlNode node)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Element:
                    if (node.Name == "input" && node.GetAttributeValue("type", null) == "submit")
                    {
                        return System.Web.HttpUtility.HtmlDecode(node.GetAttributeValue("value", "Submit").Trim());
                    }
                    return null;
                case HtmlNodeType.Text:
                    return System.Web.HttpUtility.HtmlDecode(node.InnerText.Trim());
                case HtmlNodeType.Document:
                case HtmlNodeType.Comment:
                default:
                    return null;
            }
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class LayoutBox
    {
        public HtmlNode Node;
        public Dimensions Dimensions;
        public BoxDisplay Display;
        public List<LayoutBox> Childrens;

        public LayoutBox(HtmlNode node, Dimensions dimensions, BoxDisplay display)
        {
            Node = node;
            Dimensions = dimensions;
            Display = display;
            Childrens = new List<LayoutBox>();
        }

        string GetDebuggerDisplay() => Node.NodeType switch
        {
            HtmlNodeType.Document => $"Document {{ {Dimensions.Content.size.X}px ; {Dimensions.Content.size.Y}px }}",
            HtmlNodeType.Element => $"<{Node.Name} width={Dimensions.Content.size.X}px height={Dimensions.Content.size.Y}px>",
            HtmlNodeType.Comment => $"<!-- {Node.InnerText} -->",
            HtmlNodeType.Text => $"\"{Node.InnerText.Trim()}\" {{ {Dimensions.Content.size.X}px ; {Dimensions.Content.size.Y}px }}",
            _ => $"Bruh",
        };
    }

    public enum BoxDisplay
    {
        Undefined,
        Block,
        InlineBlock,
        Table,
        None,
    }

    public struct CachedDimensions
    {
        public RectInt Content;
        public RectInt PaddingRect;
        public RectInt BorderRect;
        public RectInt MarginRect;

        public CachedDimensions(Dimensions dimensions)
        {
            Content = dimensions.Content;
            PaddingRect = dimensions.PaddingRect;
            BorderRect = dimensions.BorderRect;
            MarginRect = dimensions.MarginRect;
        }

        public static CachedDimensions operator +(CachedDimensions dimensions, Vector2Int offset)
        {
            dimensions.Content.Position += offset;
            dimensions.PaddingRect.Position += offset;
            dimensions.BorderRect.Position += offset;
            dimensions.MarginRect.Position += offset;
            return dimensions;
        }

        public static CachedDimensions operator -(CachedDimensions dimensions, Vector2Int offset)
        {
            dimensions.Content.Position -= offset;
            dimensions.PaddingRect.Position -= offset;
            dimensions.BorderRect.Position -= offset;
            dimensions.MarginRect.Position -= offset;
            return dimensions;
        }
    }

    public struct Dimensions
    {
        public RectInt Content;
        public SidesInt Padding;
        public SidesInt Border;
        public SidesInt Margin;
        internal int CurrentX;
        /// <summary>
        /// This is <b>not</b> the css property "max-width"!
        /// </summary>
        internal int MaxWidth;

        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly RectInt PaddingRect => Content.Extend(Padding);
        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly RectInt BorderRect => PaddingRect.Extend(Border);
        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly RectInt MarginRect => BorderRect.Extend(Margin);

        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2Int PaddingPosition => Content.Position - Padding.TopLeft;
        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2Int BorderPosition => PaddingPosition - Border.TopLeft;
        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2Int MarginPosition => BorderPosition - Margin.TopLeft;

        [DebuggerHidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly SidesInt ExtraSides => Padding + Border + Margin;

        public Dimensions(RectInt content)
        {
            Content = content;
            Padding = SidesInt.Zero;
            Border = SidesInt.Zero;
            Margin = SidesInt.Zero;
            CurrentX = 0;
            MaxWidth = content.Width;
        }

        public Dimensions(RectInt content, SidesInt padding, SidesInt border, SidesInt margin, int current, int maxWidth)
        {
            Content = content;
            Padding = padding;
            Border = border;
            Margin = margin;
            CurrentX = current;
            MaxWidth = maxWidth;
        }

        public Dimensions(Dimensions other)
        {
            Content = other.Content;
            Padding = other.Padding;
            Border = other.Border;
            Margin = other.Margin;
            CurrentX = other.CurrentX;
            MaxWidth = other.MaxWidth;
        }

        public static Dimensions Zero => default;

        public readonly CachedDimensions Cached => new(this);

        internal void BreakLine(ref int lineHeight)
        {
            if (CurrentX == 0) return;

            Content.Height += lineHeight;
            CurrentX = 0;
            lineHeight = 0;
        }

        internal void BreakLineForce(ref int lineHeight)
        {
            Content.Height += lineHeight;
            CurrentX = 0;
            lineHeight = 0;
        }
    }

    public delegate Vector2Int TextMeasurer(string text, int fontSize);

    public static class StyleUtils
    {
        public static DeclarationsContainer GetDeclarations(IEnumerable<Stylesheet> stylesheets, HtmlNode node)
        {
            List<Declaration> result = new();
            foreach (Stylesheet stylesheet in stylesheets)
            { result.AddOrOverride(stylesheet.GetMatches(node)); }
            return new DeclarationsContainer(result);
        }

        public static BoxDisplay GetDisplay(this DeclarationsContainer childStyles)
        {
            string displayValue = childStyles.GetString("display");

            return displayValue switch
            {
                null => BoxDisplay.Undefined,
                "block" => BoxDisplay.Block,
                "inline" => BoxDisplay.InlineBlock,
                "inline-block" => BoxDisplay.InlineBlock,
                "none" => BoxDisplay.None,
                _ => BoxDisplay.Undefined,
            };
        }
    }

    public class BoxLayoutGenerator
    {
        Stylesheet[] Stylesheets;

        TextMeasurer TextMeasurer;
        ImageSizeGetter ImageSizeGetter;

        int FontSize;
        float BoxScale;

        // RectInt Viewport;
        HtmlDocument Document;

        Vector2Int MeasureText(string text, int fontSize) => TextMeasurer?.Invoke(text, fontSize) ?? Vector2Int.Zero;
        Vector2Int MeasureText(LayoutBox node, int fontSize) => MeasureText(node.Node, fontSize);
        Vector2Int MeasureText(HtmlNode node, int fontSize)
        {
            string text = NodeUtils.TakeText(node);
            return TextMeasurer?.Invoke(text, fontSize) ?? Vector2Int.Zero;
        }

        bool TryGetTextSize(LayoutBox node, int fontSize, out Vector2Int size) => TryGetTextSize(node.Node, fontSize, out size);
        bool TryGetTextSize(HtmlNode node, int fontSize, out Vector2Int size)
        {
            size = default;

            if (TextMeasurer == null) return false;

            string text = NodeUtils.TakeText(node);

            if (string.IsNullOrWhiteSpace(text)) return false;

            size = TextMeasurer.Invoke(text, fontSize);
            return true;
        }

        bool TryGetImageSize(LayoutBox node, out Vector2Int size) => TryGetImageSize(node.Node, out size);
        bool TryGetImageSize(HtmlNode node, out Vector2Int size)
        {
            size = default;

            if (node.Name != "img")
            { return false; }

            int width = node.GetAttributeValue("width", -1);
            int height = node.GetAttributeValue("height", -1);

            if (width >= 0 && height >= 0)
            {
                size = new Vector2Int(Utils.RoundToInt(width * BoxScale), Utils.RoundToInt(height * BoxScale));
                return true;
            }

            string url = node.GetAttributeValue("src", null);

            bool success = ImageSizeGetter?.Invoke(url, out size) ?? false;

            if (success)
            {
                size = Utils.RoundToInt(size.ToFloat() * BoxScale);
                return true;
            }

            return false;
        }

        public static LayoutBox LayoutDocument(HtmlDocument document, IEnumerable<Stylesheet> stylesheets, RectInt area, TextMeasurer textMeasurer, ImageSizeGetter imageSizeGetter)
        {
            BoxLayoutGenerator generator = new()
            {
                TextMeasurer = textMeasurer,
                ImageSizeGetter = imageSizeGetter,
                Stylesheets = stylesheets.ToArray(),
                FontSize = 8,
                BoxScale = .5f,
                // Viewport = new RectInt(area.xMin, area.yMin, area.Width, area.Height),
                Document = document,
            };

            Dimensions rootDimensions = new(new RectInt(area.xMin, area.yMin, area.Width, 0));
            LayoutBox root = new(document.DocumentNode, Dimensions.Zero, BoxDisplay.Block);
            generator.Layout(root, rootDimensions);
            return root;
        }

        void Layout(LayoutBox node, Dimensions dimensions)
        {
            switch (node.Display)
            {
                case BoxDisplay.Block:
                    LayoutBlock(node, dimensions);
                    break;
                case BoxDisplay.InlineBlock:
                    LayoutInline(node, dimensions);
                    break;
                case BoxDisplay.Table:
                    LayoutTable(node, dimensions);
                    break;
                case BoxDisplay.Undefined:
                case BoxDisplay.None:
                default:
                    throw new NotImplementedException();
            }
        }

        void LayoutBlock(LayoutBox node, Dimensions dimensions)
        {
            CalculateBlockWidth(node, dimensions);
            CalculateBlockPosition(node, dimensions);
            LayoutChildren(node);
            CalculateHeight(node, dimensions);
        }

        void LayoutInline(LayoutBox node, Dimensions dimensions)
        {
            CalculateInlineWidth(node, dimensions);
            CalculateInlinePosition(node, dimensions);
            LayoutChildren(node);
            CalculateHeight(node, dimensions);
            if (node.Dimensions.CurrentX > 0)
            { node.Dimensions.Content.Width += node.Dimensions.CurrentX; }
        }

        void CalculateInlineWidth(LayoutBox node, Dimensions dimensions)
        {
            ref Dimensions d = ref node.Dimensions;
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            d.Content.Width = GetAbsoluteWidth(node, dimensions);

            if (TryGetTextSize(node, FontSize, out Vector2Int textSize))
            {
                d.Content.Width = textSize.X;
            }
            else if (TryGetImageSize(node, out Vector2Int imageSize))
            {
                d.Content.Width = imageSize.X;
            }
            else if (node.Node.GetAttributeValue("size", -1) >= 0)
            {
                int widthCharacters = node.Node.GetAttributeValue("size", -1);
                string space = new(' ', widthCharacters);
                d.Content.Width = MeasureText(space, FontSize).X;
            }

            d.MaxWidth = dimensions.MaxWidth;

            d.Padding.SetHorizontal(style.GetSidesPx("padding") * BoxScale, Utils.RoundToInt);
            d.Border.SetHorizontal(style.GetSidesPx("border") * BoxScale, Utils.RoundToInt);
            d.Margin.SetHorizontal(style.GetSidesPx("margin") * BoxScale, Utils.RoundToInt);
        }

        void CalculateInlinePosition(LayoutBox node, Dimensions dimensions)
        {
            ref Dimensions d = ref node.Dimensions;
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            d.Padding.SetVertical(style.GetSidesPx("padding") * BoxScale, Utils.RoundToInt);
            d.Border.SetVertical(style.GetSidesPx("border") * BoxScale, Utils.RoundToInt);
            d.Margin.SetVertical(style.GetSidesPx("margin") * BoxScale, Utils.RoundToInt);

            d.Content.X = dimensions.Content.X + dimensions.CurrentX;
            d.Content.Y = dimensions.Content.Height + dimensions.Content.Y;
        }

        int GetAbsoluteWidth(LayoutBox node, Dimensions dimensions)
        {
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            if (!style.TryGetNumber("width", out Number number))
            { return 0; }

            switch (number.Unit)
            {
                case Unit.Pixels:
                    return Utils.RoundToInt(number.Value * BoxScale);
                case Unit.Percentage:
                    return Utils.RoundToInt(number.Percentage * dimensions.Content.Width);
                case Unit.Em:
                    return Utils.RoundToInt(number.Value * FontSize);
                case Unit.None:
                    Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Value without unit for \"width\"");
                    return 0;
                case Unit.Unknown:
                default:
                    Debug.LogWarning($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Unsupported \"width\" unit \"{number.Unit}\"");
                    return number.Int;
            }
        }

        void CalculateBlockWidth(LayoutBox node, Dimensions dimensions)
        {
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            Value width = new("auto");

            if (TryGetTextSize(node, FontSize, out Vector2Int textSize))
            {
                width = new Value(new Number(textSize.X, Unit.Pixels));
            }
            else if (TryGetImageSize(node, out Vector2Int imageSize))
            {
                width = new Value(new Number(imageSize.X, Unit.Pixels));
            }
            else if (node.Node.GetAttributeValue("size", -1) >= 0)
            {
                int widthCharacters = node.Node.GetAttributeValue("size", -1);
                string space = new(' ', widthCharacters);
                width = new Value(new Number(MeasureText(space, FontSize).X, Unit.Pixels));
            }

            if (style.TryGetNumber("width", out Number widthNumber))
            {
                width = new Value(new Number(ConvertToPixels(dimensions, widthNumber), Unit.Pixels));
            }

            ref Dimensions d = ref node.Dimensions;

            d.Padding.SetHorizontal(style.GetSidesPx("padding") * BoxScale, Utils.RoundToInt);
            d.Border.SetHorizontal(style.GetSidesPx("border") * BoxScale, Utils.RoundToInt);
            Sides<Value> margin = style.GetSides("margin");
            Sides<int> marginNumbers = margin.ToPixels();

            int widthPixels = ConvertToPixels(dimensions, width.NumberOrZero);

            int total = widthPixels + d.BorderRect.Width + marginNumbers.Left + marginNumbers.Right;

            if (total > dimensions.Content.Width)
            {
                if (margin.Left == "auto")
                { margin.Left = new Value(new Number(0, Unit.Pixels)); }

                if (margin.Right == "auto")
                { margin.Right = new Value(new Number(0, Unit.Pixels)); }
            }

            int underflow = dimensions.Content.Width - total;

            if (width == "auto")
            {
                if (margin.Left == "auto")
                { margin.Left = new Value(new Number(0, Unit.Pixels)); }
                if (margin.Right == "auto")
                { margin.Right = new Value(new Number(0, Unit.Pixels)); }

                if (underflow >= 0)
                {
                    // Expand width to fill the underflow.
                    d.Content.Width = underflow;
                }
                else
                {
                    // Width can't be negative. Adjust the right margin instead.
                    d.Content.Width = 0;
                    margin.Right = new Value(new Number(ConvertToPixels(dimensions, margin.Right.NumberOrZero) + underflow, Unit.Pixels));
                }
            }
            else if (margin.Left.IsNumber && margin.Right.IsNumber)
            {
                margin.Right = new Value(new Number(ConvertToPixels(dimensions, margin.Right.number.Value) + underflow, Unit.Pixels));
            }
            else if (margin.Left.IsNumber && margin.Right == "auto")
            {
                margin.Right = new Value(new Number(underflow, Unit.Pixels));
            }
            else if (margin.Left == "auto" && margin.Right.IsNumber)
            {
                margin.Left = new Value(new Number(underflow, Unit.Pixels));
            }
            else if (margin.Left == "auto" && margin.Right == "auto")
            {
                margin.Left = new Value(new Number(Utils.RoundToInt(underflow / 2f), Unit.Pixels));
                margin.Right = new Value(new Number(Utils.RoundToInt(underflow / 2f), Unit.Pixels));
            }
            d.Margin.SetHorizontal(margin.ToPixels());

            d.MaxWidth = dimensions.MaxWidth;

            /*
            if (width == 0)
            {
                if (underflow >= 0)
                {
                    d.Content.Width = underflow;
                    d.Margin.Right = margin.Right;
                }
                else
                {
                    d.Margin.Right = margin.Right + underflow;
                    d.Content.Width = width;
                }
                d.Margin.Left = margin.Left;
            }
            else
            {
                d.Content.Width = width;
            }
            */
        }

        int ConvertToPixels(Dimensions containingBox, Number number)
        {
            return number.Unit switch
            {
                Unit.Percentage => Utils.RoundToInt(number.Percentage * containingBox.Content.Width),
                Unit.Em => Utils.RoundToInt(number.Value * FontSize),
                Unit.Pixels => number.Int,
                _ => number.Int,
            };
        }

        void CalculateBlockPosition(LayoutBox node, Dimensions dimensions)
        {
            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            ref Dimensions d = ref node.Dimensions;

            d.Margin.SetVertical((style.GetSidesPx("margin") * BoxScale).ToInt());
            d.Border.SetVertical((style.GetSidesPx("border") * BoxScale).ToInt());
            d.Padding.SetVertical((style.GetSidesPx("padding") * BoxScale).ToInt());

            d.Content.X = dimensions.Content.X + d.ExtraSides.Left;
            d.Content.Y = dimensions.Content.yMax + d.ExtraSides.Top;
        }

        void CalculateHeight(LayoutBox node, Dimensions dimensions)
        {
            ref Dimensions d = ref node.Dimensions;

            if (TryGetTextSize(node, FontSize, out Vector2Int textSize))
            {
                d.Content.Height = textSize.Y;
            }
            else if (TryGetImageSize(node, out Vector2Int imageSize))
            {
                d.Content.Height = imageSize.X;
            }

            DeclarationsContainer style = StyleUtils.GetDeclarations(Stylesheets, node.Node);

            if (style.TryGetNumber("height", out Number number))
            {
                switch (number.Unit)
                {
                    case Unit.Pixels:
                        d.Content.Height = (int)(number.Value * BoxScale);
                        break;
                    case Unit.Em:
                        d.Content.Height = Utils.RoundToInt(number.Value * FontSize);
                        break;
                    case Unit.Percentage:
                        // d.Content.Height = Utils.RoundToInt(number.Percentage * dimensions.Content.Height);
                        break;
                    case Unit.None:
                        Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Value without unit for \"height\"");
                        break;
                    case Unit.Unknown:
                    default:
                        Debug.LogWarning($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Unsupported \"height\" unit \"{number.Unit}\"");
                        break;
                }
            }
        }

        void LayoutChildren(LayoutBox node)
        {
            List<HtmlNode> childs = new();

            foreach (HtmlNode child in node.Node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Comment)
                { continue; }

                if (child is HtmlTextNode textNode)
                { childs.AddRange(BreakText(textNode, node.Dimensions.MaxWidth)); }
                else
                { childs.Add(child); }
            }

            int lineHeight = 0;
            bool prevIsBlock = true;

            foreach (HtmlNode child in childs)
            {
                if (child.NodeType == HtmlNodeType.Comment)
                { continue; }

                DeclarationsContainer childStyles = StyleUtils.GetDeclarations(Stylesheets, child);
                BoxDisplay childDisplay = childStyles.GetDisplay();

                if (childDisplay == BoxDisplay.Undefined)
                {
                    childDisplay = child.NodeType switch
                    {
                        HtmlNodeType.Document => BoxDisplay.Block,
                        HtmlNodeType.Text => BoxDisplay.InlineBlock,
                        HtmlNodeType.Element => child.Name switch
                        {
                            "table" => BoxDisplay.Table,
                            _ => childDisplay,
                        },
                        HtmlNodeType.Comment => BoxDisplay.None,
                        _ => childDisplay,
                    };
                }

                if (childDisplay == BoxDisplay.None)
                { continue; }

                if (childDisplay == BoxDisplay.Undefined)
                {
                    Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: No display specified for element \"<{child.Name}>\"");
                    childDisplay = BoxDisplay.Block;
                }

                bool isBlock = (childDisplay == BoxDisplay.Block || childDisplay == BoxDisplay.Table);

                LayoutBox childBox = new(child, new Dimensions(Dimensions.Zero)
                {
                    MaxWidth = Math.Max(0, node.Dimensions.Content.Width - node.Dimensions.CurrentX),
                }, childDisplay);
                node.Childrens.Add(childBox);

                if (isBlock)
                { node.Dimensions.BreakLineForce(ref lineHeight); }

                Layout(childBox, node.Dimensions);
                node.Dimensions.CurrentX += childBox.Dimensions.MarginRect.Width;

                lineHeight = Math.Max(lineHeight, childBox.Dimensions.MarginRect.Height);

                if (isBlock)
                {
                    node.Dimensions.BreakLineForce(ref lineHeight);
                }
                else
                {
                    if (node.Dimensions.CurrentX > node.Dimensions.Content.Width)
                    {
                        node.Dimensions.BreakLine(ref lineHeight);

                        Layout(childBox, node.Dimensions);
                        node.Dimensions.CurrentX += childBox.Dimensions.MarginRect.Width;

                        lineHeight = Math.Max(lineHeight, childBox.Dimensions.MarginRect.Height);
                    }
                }

                prevIsBlock = isBlock;
            }

            if (lineHeight > 0)
            {
                node.Dimensions.BreakLineForce(ref lineHeight);
                node.Dimensions.Content.Width += node.Dimensions.CurrentX;
            }
        }

        void LayoutTable(LayoutBox node, Dimensions dimensions)
        {
            List<List<HtmlNode>> table = new();
            foreach (HtmlNode row in node.Node.ChildNodes)
            {
                if (row.NodeType == HtmlNodeType.Comment) continue;

                if (row.Name != "tr")
                {
                    Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Unexpected table element as row: <{row.Name}>");
                    continue;
                }

                List<HtmlNode> rowContent = new();
                foreach (HtmlNode col in row.ChildNodes)
                {
                    if (col.NodeType == HtmlNodeType.Comment) continue;

                    if (col.Name != "td" && col.Name != "th")
                    {
                        Debug.Log($"[{nameof(BoxLayout)}/{nameof(Generator)}]: Unexpected table element as cell: <{row.Name}>");
                        continue;
                    }

                    rowContent.Add(col);
                }
                table.Add(rowContent);
            }

            int cellSpacing = 4;

            int columns = 0;

            foreach (List<HtmlNode> row in table)
            {
                columns = Math.Max(row.Count, columns);
            }

            foreach (List<HtmlNode> row in table)
            {
                for (int i = row.Count; i < columns; i++)
                {
                    row.Add(Document.CreateElement("td"));
                }
            }

            CalculateBlockWidth(node, dimensions);
            CalculateBlockPosition(node, dimensions);

            ref Dimensions d = ref node.Dimensions;

            int[] columnWidths = new int[columns];
            for (int i = 0; i < columnWidths.Length; i++)
            {
                columnWidths[i] = (d.Content.Width - (cellSpacing * (columns - 1))) / columns;
            }

            Dimensions dCopy = node.Dimensions;

            for (int rowIndex = 0; rowIndex < table.Count; rowIndex++)
            {
                List<HtmlNode> row = table[rowIndex];

                int rowHeight = 0;

                for (int cellIndex = 0; cellIndex < row.Count; cellIndex++)
                {
                    LayoutBox newBox = new(row[cellIndex], new Dimensions(Dimensions.Zero)
                    {
                        MaxWidth = Math.Max(0, dCopy.Content.Width - dCopy.CurrentX),
                    }, BoxDisplay.InlineBlock);
                    Layout(newBox, dCopy);
                    rowHeight = Math.Max(rowHeight, newBox.Dimensions.MarginRect.Height);

                    dCopy.CurrentX += newBox.Dimensions.MarginRect.Width;
                    dCopy.CurrentX += cellSpacing;

                    columnWidths[cellIndex] = Math.Max(columnWidths[cellIndex], newBox.Dimensions.MarginRect.Width);
                }

                dCopy.Content.Width = Math.Max(dCopy.CurrentX, dCopy.Content.Width);
                dCopy.CurrentX = 0;
                dCopy.Content.Height += rowHeight;
                dCopy.Content.Height += cellSpacing;
            }

            for (int rowIndex = 0; rowIndex < table.Count; rowIndex++)
            {
                List<HtmlNode> row = table[rowIndex];

                int rowHeight = 0;

                for (int cellIndex = 0; cellIndex < row.Count; cellIndex++)
                {
                    LayoutBox newBox = new(row[cellIndex], new Dimensions(Dimensions.Zero)
                    {
                        MaxWidth = columnWidths[cellIndex],
                    }, BoxDisplay.InlineBlock);
                    node.Childrens.Add(newBox);
                    Layout(newBox, d);
                    rowHeight = Math.Max(rowHeight, newBox.Dimensions.MarginRect.Height);

                    d.CurrentX += columnWidths[cellIndex];
                    d.CurrentX += cellSpacing;
                }

                d.Content.Width = Math.Max(d.CurrentX, d.Content.Width);
                d.CurrentX = 0;
                d.Content.Height += rowHeight;
                d.Content.Height += cellSpacing;
            }
        }

        HtmlTextNode[] BreakText(HtmlTextNode node, int maxWidth)
        {
            if (node == null) return null;

            List<HtmlTextNode> result = new();

            List<string> remaingWords = new(node.Text.Split(' ', '\n', '\r', '\t'));
            string currentText = "";

            int endlessSafe = 0;
            while (remaingWords.Count > 0)
            {
                string word = remaingWords[0];
                remaingWords.RemoveAt(0);

                if (string.IsNullOrWhiteSpace(word))
                { continue; }

                string space = "";
                if (currentText.Length > 0)
                {
                    space = " ";
                }

                int width = MeasureText(currentText + space + word, FontSize).X;

                if (width > maxWidth)
                {
                    if (!string.IsNullOrWhiteSpace(currentText))
                    { result.Add(Document.CreateTextNode(currentText)); }
                    currentText = word;
                }
                else
                {
                    currentText += space + word;
                }

                if (endlessSafe++ > 50)
                { break; }
            }

            if (!string.IsNullOrWhiteSpace(currentText))
            {
                result.Add(Document.CreateTextNode(currentText));
                currentText = "";
            }

            return result.ToArray();
        }
    }
}
