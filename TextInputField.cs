namespace WebBrowser
{
    static class KeyboardUtils
    {
        public static char? KeyToChar(KeyCode code) => code switch
        {
            KeyCode.None => null,
            KeyCode.Backspace => null,
            KeyCode.Delete => null,
            KeyCode.Tab => '\t',
            KeyCode.Clear => null,
            KeyCode.Return => '\n',
            KeyCode.Pause => null,
            KeyCode.Escape => null,
            KeyCode.Space => ' ',
            KeyCode.Keypad0 => '0',
            KeyCode.Keypad1 => '1',
            KeyCode.Keypad2 => '2',
            KeyCode.Keypad3 => '3',
            KeyCode.Keypad4 => '4',
            KeyCode.Keypad5 => '5',
            KeyCode.Keypad6 => '6',
            KeyCode.Keypad7 => '7',
            KeyCode.Keypad8 => '8',
            KeyCode.Keypad9 => '9',
            KeyCode.KeypadPeriod => '.',
            KeyCode.KeypadDivide => '/',
            KeyCode.KeypadMultiply => '*',
            KeyCode.KeypadMinus => '-',
            KeyCode.KeypadPlus => '+',
            KeyCode.KeypadEnter => '\r',
            KeyCode.KeypadEquals => '=',
            KeyCode.Alpha0 => '0',
            KeyCode.Alpha1 => '1',
            KeyCode.Alpha2 => '2',
            KeyCode.Alpha3 => '3',
            KeyCode.Alpha4 => '4',
            KeyCode.Alpha5 => '5',
            KeyCode.Alpha6 => '6',
            KeyCode.Alpha7 => '7',
            KeyCode.Alpha8 => '8',
            KeyCode.Alpha9 => '9',
            KeyCode.Exclaim => '!',
            KeyCode.DoubleQuote => '"',
            KeyCode.Hash => '#',
            KeyCode.Dollar => '$',
            KeyCode.Percent => '%',
            KeyCode.Ampersand => '&',
            KeyCode.Quote => '\'',
            KeyCode.LeftParen => '(',
            KeyCode.RightParen => ')',
            KeyCode.Asterisk => '*',
            KeyCode.Plus => '+',
            KeyCode.Comma => ',',
            KeyCode.Minus => '-',
            KeyCode.Period => '.',
            KeyCode.Slash => '/',
            KeyCode.Colon => ':',
            KeyCode.Semicolon => ';',
            KeyCode.Less => '<',
            KeyCode.Equals => '=',
            KeyCode.Greater => '>',
            KeyCode.Question => '?',
            KeyCode.At => '@',
            KeyCode.LeftBracket => '[',
            KeyCode.Backslash => '\\',
            KeyCode.RightBracket => ']',
            KeyCode.Caret => '^',
            KeyCode.Underscore => '_',
            KeyCode.BackQuote => '`',
            KeyCode.A => 'a',
            KeyCode.B => 'b',
            KeyCode.C => 'c',
            KeyCode.D => 'd',
            KeyCode.E => 'e',
            KeyCode.F => 'f',
            KeyCode.G => 'g',
            KeyCode.H => 'h',
            KeyCode.I => 'i',
            KeyCode.J => 'j',
            KeyCode.K => 'k',
            KeyCode.L => 'l',
            KeyCode.M => 'm',
            KeyCode.N => 'n',
            KeyCode.O => 'o',
            KeyCode.P => 'p',
            KeyCode.Q => 'q',
            KeyCode.R => 'r',
            KeyCode.S => 's',
            KeyCode.T => 't',
            KeyCode.U => 'u',
            KeyCode.V => 'v',
            KeyCode.W => 'w',
            KeyCode.X => 'x',
            KeyCode.Y => 'y',
            KeyCode.Z => 'z',
            KeyCode.LeftCurlyBracket => '{',
            KeyCode.Pipe => '|',
            KeyCode.RightCurlyBracket => '}',
            KeyCode.Tilde => '~',
            _ => null,
        };
    }

    class TextInputField
    {
        public string Buffer;
        public int CursorPosition;

        public TextInputField()
        {
            Buffer = string.Empty;
            CursorPosition = 0;
        }

        public TextInputField(string value)
        {
            Buffer = value;
            CursorPosition = value?.Length ?? 0;
        }

        public void FeedKey(KeyCode key)
        {
            if (key == KeyCode.Backspace)
            {
                if (Buffer.Length > 0)
                {
                    if (CursorPosition == 0)
                    {

                    }
                    else if (CursorPosition == Buffer.Length)
                    {
                        Buffer = Buffer[..^1];
                    }
                    else
                    {
                        Buffer = Buffer[..(CursorPosition - 1)] + Buffer[(CursorPosition)..];
                    }
                }
                MoveCursor(-1);
                return;
            }

            if (key == KeyCode.Delete)
            {
                if (Buffer.Length > 0)
                {
                    if (CursorPosition == 0)
                    {
                        Buffer = Buffer[1..];
                    }
                    else if (CursorPosition == Buffer.Length)
                    {

                    }
                    else
                    {
                        Buffer = Buffer[..(CursorPosition)] + Buffer[(CursorPosition + 1)..];
                    }
                }
                return;
            }

            if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            { return; }

            if (key == KeyCode.LeftArrow)
            {
                MoveCursor(-1);
                return;
            }

            if (key == KeyCode.RightArrow)
            {
                MoveCursor(1);
                return;
            }

            char? possibleChar = KeyboardUtils.KeyToChar(key);

            if (possibleChar.HasValue)
            {
                char @char = possibleChar.Value;

                if (CursorPosition == 0)
                { Buffer = @char + Buffer; }
                else if (CursorPosition == Buffer.Length)
                { Buffer += @char; }
                else
                { Buffer = Buffer.Insert(CursorPosition, @char.ToString()); }

                MoveCursor(1);
                return;
            }
        }

        void MoveCursor(int offset)
        { CursorPosition = Math.Clamp(CursorPosition + offset, 0, Buffer.Length); }

        public void Clear()
        {
            Buffer = string.Empty;
            CursorPosition = 0;
        }

        internal void SetCursor(int clickedCharacter)
        { CursorPosition = Math.Clamp(clickedCharacter, 0, Buffer.Length); }
    }

}
