using System;
using System.Collections.Generic;
using System.Globalization;

namespace TehranNoir.Dialogue
{
    public enum DialogueValueKind
    {
        Undefined,
        Number,
        Text,
        Bool
    }

    /// یک مقدار در دنیای دیالوگ: عدد، متن، بولی یا تعریف‌نشده.
    public readonly struct DialogueValue
    {
        public readonly DialogueValueKind Kind;
        private readonly double _number;
        private readonly string _text;
        private readonly bool _flag;

        private DialogueValue(DialogueValueKind kind, double number, string text, bool flag)
        {
            Kind = kind;
            _number = number;
            _text = text;
            _flag = flag;
        }

        public static DialogueValue Undefined { get { return new DialogueValue(DialogueValueKind.Undefined, 0d, null, false); } }

        public static DialogueValue Number(double value)
        {
            return new DialogueValue(DialogueValueKind.Number, value, null, false);
        }

        public static DialogueValue Text(string value)
        {
            return new DialogueValue(DialogueValueKind.Text, 0d, value ?? string.Empty, false);
        }

        public static DialogueValue Bool(bool value)
        {
            return new DialogueValue(DialogueValueKind.Bool, 0d, null, value);
        }

        public bool IsUndefined { get { return Kind == DialogueValueKind.Undefined; } }

        public bool Truthy()
        {
            if (Kind == DialogueValueKind.Bool) return _flag;
            if (Kind == DialogueValueKind.Number) return _number != 0d;
            if (Kind == DialogueValueKind.Text)
                return _text.Length > 0 && _text.ToLowerInvariant() != "false";
            return false;
        }

        public double AsNumber()
        {
            if (Kind == DialogueValueKind.Number) return _number;
            if (Kind == DialogueValueKind.Bool) return _flag ? 1d : 0d;

            if (Kind == DialogueValueKind.Text)
            {
                string trimmed = _text.Trim();
                if (trimmed.Length == 0) return double.NaN;

                double parsed;
                if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                    return parsed;
                return double.NaN;
            }

            return double.NaN;
        }

        public string AsText()
        {
            if (Kind == DialogueValueKind.Text) return _text;
            if (Kind == DialogueValueKind.Bool) return _flag ? "true" : "false";
            if (Kind == DialogueValueKind.Number) return _number.ToString(CultureInfo.InvariantCulture);
            return "undefined";
        }
    }

    /// پایه‌ی همه‌ی شرط‌ها.
    public abstract class Condition
    {
        public abstract DialogueValue Evaluate(WorldState world);

        public abstract void CollectVariables(List<string> result);

        public bool IsTrue(WorldState world)
        {
            return Evaluate(world).Truthy();
        }
    }

    public sealed class LiteralCondition : Condition
    {
        private readonly DialogueValue _value;

        public LiteralCondition(DialogueValue value)
        {
            _value = value;
        }

        public override DialogueValue Evaluate(WorldState world)
        {
            return _value;
        }

        public override void CollectVariables(List<string> result)
        {
        }
    }

    public sealed class VariableCondition : Condition
    {
        public readonly string Name;

        public VariableCondition(string name)
        {
            Name = name ?? string.Empty;
        }

        public override DialogueValue Evaluate(WorldState world)
        {
            return world == null ? DialogueValue.Undefined : world.Get(Name);
        }

        public override void CollectVariables(List<string> result)
        {
            if (result == null || !WorldState.IsKnownVariable(Name)) return;
            if (!result.Contains(Name)) result.Add(Name);
        }
    }

    public sealed class NotCondition : Condition
    {
        private readonly Condition _inner;

        public NotCondition(Condition inner)
        {
            _inner = inner;
        }

        public override DialogueValue Evaluate(WorldState world)
        {
            return DialogueValue.Bool(!_inner.Evaluate(world).Truthy());
        }

        public override void CollectVariables(List<string> result)
        {
            _inner.CollectVariables(result);
        }
    }

    public sealed class AndCondition : Condition
    {
        private readonly Condition _left;
        private readonly Condition _right;

        public AndCondition(Condition left, Condition right)
        {
            _left = left;
            _right = right;
        }

        public override DialogueValue Evaluate(WorldState world)
        {
            if (!_left.Evaluate(world).Truthy()) return DialogueValue.Bool(false);
            return DialogueValue.Bool(_right.Evaluate(world).Truthy());
        }

        public override void CollectVariables(List<string> result)
        {
            _left.CollectVariables(result);
            _right.CollectVariables(result);
        }
    }

    public sealed class OrCondition : Condition
    {
        private readonly Condition _left;
        private readonly Condition _right;

        public OrCondition(Condition left, Condition right)
        {
            _left = left;
            _right = right;
        }

        public override DialogueValue Evaluate(WorldState world)
        {
            if (_left.Evaluate(world).Truthy()) return DialogueValue.Bool(true);
            return DialogueValue.Bool(_right.Evaluate(world).Truthy());
        }

        public override void CollectVariables(List<string> result)
        {
            _left.CollectVariables(result);
            _right.CollectVariables(result);
        }
    }

    /// مقایسه: == != > < >= <=
    public sealed class CompareCondition : Condition
    {
        private readonly string _operator;
        private readonly Condition _left;
        private readonly Condition _right;

        public CompareCondition(string op, Condition left, Condition right)
        {
            _operator = op;
            _left = left;
            _right = right;
        }

        public override DialogueValue Evaluate(WorldState world)
        {
            DialogueValue a = Resolve(_left, world);
            DialogueValue b = Resolve(_right, world);

            double numberA = a.AsNumber();
            double numberB = b.AsNumber();
            bool numeric = !double.IsNaN(numberA) && !double.IsNaN(numberB);

            bool result = numeric
                ? CompareNumbers(_operator, numberA, numberB)
                : CompareText(_operator, a.AsText(), b.AsText());

            return DialogueValue.Bool(result);
        }

        public override void CollectVariables(List<string> result)
        {
            _left.CollectVariables(result);
            _right.CollectVariables(result);
        }

        /// در مقایسه، متغیر تعریف‌نشده با نام خودش مقایسه می‌شود؛
        /// یعنی «time == night» بدون گیومه هم کار می‌کند.
        private static DialogueValue Resolve(Condition node, WorldState world)
        {
            DialogueValue value = node.Evaluate(world);
            if (value.IsUndefined && node is VariableCondition variable)
                return DialogueValue.Text(variable.Name);
            return value;
        }

        private static bool CompareNumbers(string op, double a, double b)
        {
            switch (op)
            {
                case "==": return a == b;
                case "!=": return a != b;
                case ">": return a > b;
                case "<": return a < b;
                case ">=": return a >= b;
                case "<=": return a <= b;
            }
            return false;
        }

        private static bool CompareText(string op, string a, string b)
        {
            int compare = string.CompareOrdinal(a, b);
            switch (op)
            {
                case "==": return compare == 0;
                case "!=": return compare != 0;
                case ">": return compare > 0;
                case "<": return compare < 0;
                case ">=": return compare >= 0;
                case "<=": return compare <= 0;
            }
            return false;
        }
    }

    public sealed class ConditionException : Exception
    {
        public ConditionException(string message) : base(message)
        {
        }
    }

    /// خواندن متن شرط و ساختن درخت آن.
    public static class ConditionParser
    {
        private enum TokenKind
        {
            Operator,
            Number,
            Text,
            Identifier,
            OpenParen,
            CloseParen
        }

        private struct Token
        {
            public TokenKind Kind;
            public string Value;
            public double Number;
        }

        public static bool TryParse(string source, out Condition condition, out string error)
        {
            condition = null;
            error = null;

            try
            {
                List<Token> tokens = Tokenize(source);
                Parser parser = new Parser(tokens);
                condition = parser.ParseAll();
                return true;
            }
            catch (ConditionException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static Condition Parse(string source)
        {
            Condition condition;
            string error;
            if (TryParse(source, out condition, out error)) return condition;
            throw new ConditionException(error);
        }

        private static void AddOperator(List<Token> tokens, string op)
        {
            tokens.Add(new Token { Kind = TokenKind.Operator, Value = op });
        }

        private static bool IsIdentifierStart(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                   c == '_' || (c >= '\u0600' && c <= '\u06FF');
        }

        private static bool IsIdentifierPart(char c)
        {
            return IsIdentifierStart(c) || (c >= '0' && c <= '9') || c == '.';
        }

        private static List<Token> Tokenize(string source)
        {
            List<Token> tokens = new List<Token>();
            string text = source ?? string.Empty;
            int index = 0;

            while (index < text.Length)
            {
                char c = text[index];

                if (char.IsWhiteSpace(c))
                {
                    index++;
                    continue;
                }

                if (index + 1 < text.Length)
                {
                    string pair = text.Substring(index, 2);
                    if (pair == ">=" || pair == "<=" || pair == "==" || pair == "!=")
                    {
                        AddOperator(tokens, pair);
                        index += 2;
                        continue;
                    }
                }

                if (c == '>' || c == '<' || c == '=')
                {
                    AddOperator(tokens, c.ToString());
                    index++;
                    continue;
                }

                if (c == '(')
                {
                    tokens.Add(new Token { Kind = TokenKind.OpenParen, Value = "(" });
                    index++;
                    continue;
                }

                if (c == ')')
                {
                    tokens.Add(new Token { Kind = TokenKind.CloseParen, Value = ")" });
                    index++;
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    char quote = c;
                    int cursor = index + 1;
                    string buffer = string.Empty;

                    while (cursor < text.Length && text[cursor] != quote)
                    {
                        buffer += text[cursor];
                        cursor++;
                    }

                    tokens.Add(new Token { Kind = TokenKind.Text, Value = buffer });
                    index = cursor + 1;
                    continue;
                }

                if (c >= '0' && c <= '9')
                {
                    int cursor = index;
                    string digits = string.Empty;

                    while (cursor < text.Length &&
                           ((text[cursor] >= '0' && text[cursor] <= '9') || text[cursor] == '.'))
                    {
                        digits += text[cursor];
                        cursor++;
                    }

                    double parsed;
                    if (!double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                        parsed = 0d;

                    tokens.Add(new Token { Kind = TokenKind.Number, Number = parsed });
                    index = cursor;
                    continue;
                }

                if (IsIdentifierStart(c))
                {
                    int cursor = index;
                    string word = string.Empty;

                    while (cursor < text.Length && IsIdentifierPart(text[cursor]))
                    {
                        word += text[cursor];
                        cursor++;
                    }

                    tokens.Add(new Token { Kind = TokenKind.Identifier, Value = word });
                    index = cursor;
                    continue;
                }

                throw new ConditionException("کاراکتر ناشناخته در شرط: «" + c + "»");
            }

            return tokens;
        }

        private sealed class Parser
        {
            private readonly List<Token> _tokens;
            private int _position;

            public Parser(List<Token> tokens)
            {
                _tokens = tokens;
            }

            public Condition ParseAll()
            {
                Condition condition = ParseOr();

                if (_position < _tokens.Count)
                    throw new ConditionException("بخش اضافه در شرط: «" + _tokens[_position].Value + "»");

                return condition;
            }

            private Token? Peek()
            {
                if (_position >= _tokens.Count) return null;
                return _tokens[_position];
            }

            private Token? Take()
            {
                if (_position >= _tokens.Count) return null;
                Token token = _tokens[_position];
                _position++;
                return token;
            }

            private static bool IsKeyword(Token? token, string keyword)
            {
                return token.HasValue && token.Value.Kind == TokenKind.Identifier &&
                       string.Equals(token.Value.Value, keyword, StringComparison.OrdinalIgnoreCase);
            }

            private Condition ParseOr()
            {
                Condition left = ParseAnd();

                while (IsKeyword(Peek(), "or"))
                {
                    Take();
                    left = new OrCondition(left, ParseAnd());
                }

                return left;
            }

            private Condition ParseAnd()
            {
                Condition left = ParseNot();

                while (IsKeyword(Peek(), "and"))
                {
                    Take();
                    left = new AndCondition(left, ParseNot());
                }

                return left;
            }

            private Condition ParseNot()
            {
                if (IsKeyword(Peek(), "not"))
                {
                    Take();
                    return new NotCondition(ParseNot());
                }

                return ParseCompare();
            }

            private Condition ParseCompare()
            {
                Condition left = ParsePrimary();
                Token? token = Peek();

                if (token.HasValue && token.Value.Kind == TokenKind.Operator)
                {
                    string op = token.Value.Value;

                    // یک «=» تنها هم برابر است: در فایل آیتم‌ها شرط با همان «=»ی
                    // اثرها نوشته می‌شود، و رد کردنش قاعده را بی‌صدا حذف می‌کرد.
                    if (op == "=") op = "==";

                    if (op == "==" || op == "!=" || op == ">" || op == "<" || op == ">=" || op == "<=")
                    {
                        Take();
                        return new CompareCondition(op, left, ParsePrimary());
                    }
                }

                return left;
            }

            private Condition ParsePrimary()
            {
                Token? taken = Take();
                if (!taken.HasValue) throw new ConditionException("شرط ناقص است");

                Token token = taken.Value;

                if (token.Kind == TokenKind.OpenParen)
                {
                    Condition inner = ParseOr();
                    Token? close = Take();

                    if (!close.HasValue || close.Value.Kind != TokenKind.CloseParen)
                        throw new ConditionException("پرانتز بسته نشده");

                    return inner;
                }

                if (token.Kind == TokenKind.Number)
                    return new LiteralCondition(DialogueValue.Number(token.Number));

                if (token.Kind == TokenKind.Text)
                    return new LiteralCondition(DialogueValue.Text(token.Value));

                if (token.Kind == TokenKind.Identifier)
                {
                    string lowered = token.Value.ToLowerInvariant();
                    if (lowered == "true") return new LiteralCondition(DialogueValue.Bool(true));
                    if (lowered == "false") return new LiteralCondition(DialogueValue.Bool(false));
                    return new VariableCondition(token.Value);
                }

                throw new ConditionException("عبارت شرط نامعتبر: «" + token.Value + "»");
            }
        }
    }
}
