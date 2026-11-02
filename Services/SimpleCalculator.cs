
namespace EdgeKit.Services;

public static class SimpleCalculator
{
    public static double Evaluate(string expression)
    {
        var parser = new Parser(expression);
        double result = parser.ParseExpression();

        if (!parser.AtEnd)
            throw new FormatException("Unexpected input.");

        if (double.IsNaN(result) || double.IsInfinity(result))
            throw new ArithmeticException("Invalid result.");

        return result;
    }

    private sealed class Parser
    {
        private readonly string _text;
        private int _position;

        public Parser(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public bool AtEnd
        {
            get
            {
                SkipSpaces();
                return _position >= _text.Length;
            }
        }

        private void SkipSpaces()
        {
            while (_position < _text.Length &&
                   char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        private bool Match(char c)
        {
            SkipSpaces();

            if (_position >= _text.Length || _text[_position] != c)
                return false;

            _position++;
            return true;
        }

        public double ParseExpression()
        {
            double value = ParseTerm();

            while (true)
            {
                if (Match('+'))
                    value += ParseTerm();
                else if (Match('-'))
                    value -= ParseTerm();
                else
                    return value;
            }
        }

        private double ParseTerm()
        {
            double value = ParseUnary();

            while (true)
            {
                if (Match('*'))
                {
                    value *= ParseUnary();
                }
                else if (Match('/'))
                {
                    double divisor = ParseUnary();

                    if (divisor == 0)
                        throw new DivideByZeroException();

                    value /= divisor;
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseUnary()
        {
            if (Match('+'))
                return ParseUnary();

            if (Match('-'))
                return -ParseUnary();

            return ParsePrimary();
        }

        private double ParsePrimary()
        {
            if (Match('('))
            {
                double value = ParseExpression();

                if (!Match(')'))
                    throw new FormatException("Missing closing parenthesis.");

                return value;
            }

            SkipSpaces();

            int start = _position;
            bool decimalSeen = false;

            while (_position < _text.Length)
            {
                char c = _text[_position];

                if (char.IsDigit(c))
                {
                    _position++;
                }
                else if (c == '.' && !decimalSeen)
                {
                    decimalSeen = true;
                    _position++;
                }
                else
                {
                    break;
                }
            }

            if (start == _position)
                throw new FormatException("Expected a number.");

            string token = _text[start.._position];

            if (!double.TryParse(
                    token,
                    System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double number))
            {
                throw new FormatException("Invalid number.");
            }

            return number;
        }
    }
}
