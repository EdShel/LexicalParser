using System.Text.RegularExpressions;

namespace LabParserBoolean
{
    public enum TokenKind
    {
        Eof,
        Ignore,
        Identifier,
        Number,
        RelOp,
        And,
        Or,
        Xor,
        Not,
        Assign,
        Terminator,
        ParBegin,
        ParEnd,
    }

    public record Token(TokenKind Kind, string Value, int Line, int Column);

    public class ScannerRegex
    {
    // Characters
    private const string charDigit = "[0-9]";
    private const string charHexDigit = "[0-9a-f]";
    private const string charLetter = "[A-Za-z_]";
    private const string charSpace = "[ \r\n\t]";

    // Tokens
    private readonly Dictionary<TokenKind, string> tokens = new()
    {
        [TokenKind.Ignore] = $"{charSpace}+|$",
        [TokenKind.Or] = "or",
        [TokenKind.Xor] = "xor",
        [TokenKind.And] = "and",
        [TokenKind.Not] = "not",
        [TokenKind.Terminator] = $";",
        [TokenKind.Identifier] = $"{charLetter}({charLetter}|{charDigit})*",
        [TokenKind.Number] = $"({charDigit}({charHexDigit})*)",
        [TokenKind.ParBegin] = "\\(",
        [TokenKind.ParEnd] = "\\)",
        [TokenKind.RelOp] = ">=|<=|>|<",
        [TokenKind.Assign] = ":=",
    };

        private readonly string input;

        public ScannerRegex(string input)
        {
            this.input = input;
        }

        public IEnumerable<Token> Scan()
        {
            List<Token> parsedTokens = new List<Token>();
            for (int i = 0; i < this.input.Length;)
            {
                Match? successfulMatch = null;
                TokenKind? successfulToken = null;

                foreach (KeyValuePair<TokenKind, string> token in this.tokens)
                {
                    var regex = new Regex($"^({token.Value})");
                    string nextTokenBegin = this.input.Substring(i);
                    Match match = regex.Match(nextTokenBegin);
                    if (match.Success)
                    {
                        successfulMatch = match;
                        successfulToken = token.Key;
                        break;
                    }
                }

                if (successfulMatch == null)
                {
                    throw Error(this.input, i);
                }

                i += successfulMatch.Length;

                if (successfulToken == TokenKind.Ignore)
                {
                    continue;
                }

                var (line, column) = GetLineAndColumn(this.input, i);
                Token parsedToken = new Token(
                    Value: successfulMatch.Value,
                    Kind: successfulToken!.Value,
                    Line: line,
                    Column: column
                );
                parsedTokens.Add(parsedToken);
            }

            var (endLine, endColumn) = GetLineAndColumn(this.input, this.input.Length);
            Token eof = new Token
            (
                Value: string.Empty,
                Kind: TokenKind.Eof,
                Line: endLine,
                Column: endColumn
            );
            parsedTokens.Add(eof);

            return parsedTokens;
        }

        private static InvalidOperationException Error(string input, int pos)
        {
            var (lineNumber, columnNumber) = GetLineAndColumn(input, pos);
            var error = $"Unexpected token, line {lineNumber}, column {columnNumber}.";
            return new InvalidOperationException(error);
        }

        private static (int, int) GetLineAndColumn(string input, int pos)
        {
            var linesMatches = new Regex("^.+", RegexOptions.Multiline).Matches(input[..pos]);
            int lineNumber = linesMatches.Count;
            int columnNumber = linesMatches.LastOrDefault()?.Length ?? 1;

            return (lineNumber, columnNumber);
        }
    }
}
