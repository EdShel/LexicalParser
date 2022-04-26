using System.Text.RegularExpressions;

namespace LabParser
{
    public enum TokenKind
    {
        Eof,
        Ignore,
        Const,
        Var,
        Do,
        While,
        Or,
        And,
        Not,
        Terminator,
        Identifier,
        Number,
        BlockBegin,
        BlockEnd,
        IndexerBegin,
        IndexerEnd,
        ParBegin,
        ParEnd,
        RelOp,
        Eq,
        Ne,
        Assign,
        Add,
        Sub,
        Mul,
        Div,
    }

    public record Token(TokenKind Kind, string Value, int Line, int Column);

    public class ScannerRegex
    {
        // Characters
        private const string charDigit = "[0-9]";
        private const string charDigitNotZero = "[1-9]";
        private const string charLetter = "[A-Za-z_]";
        private const string charSpace = "[ \r\n\t]";
        private const string intNum = $"({charDigitNotZero}{charDigit}*|0)";
        private const string floatNum = $@"({intNum}\.{charDigit}+)";

        // Tokens
        private readonly Dictionary<TokenKind, string> tokens = new()
        {
            [TokenKind.Ignore] = $"{charSpace}+|$",
            [TokenKind.Const] = "const",
            [TokenKind.Var] = "var",
            [TokenKind.Do] = $"do",
            [TokenKind.While] = $"while",
            [TokenKind.Or] = @"\|\|",
            [TokenKind.And] = "&&",
            [TokenKind.Not] = "!",
            [TokenKind.Terminator] = $";",
            [TokenKind.Identifier] = $"{charLetter}",
            [TokenKind.Number] = $"({floatNum}|{intNum})",
            [TokenKind.BlockBegin] = "\\{",
            [TokenKind.BlockEnd] = "\\}",
            [TokenKind.IndexerBegin] = "\\[",
            [TokenKind.IndexerEnd] = "\\]",
            [TokenKind.ParBegin] = "\\(",
            [TokenKind.ParEnd] = "\\)",
            [TokenKind.RelOp] = ">=|<=|>|<",
            [TokenKind.Eq] = "==",
            [TokenKind.Ne] = "!=",
            [TokenKind.Assign] = ":=",
            [TokenKind.Add] = @"\+",
            [TokenKind.Sub] = "-",
            [TokenKind.Mul] = @"\*",
            [TokenKind.Div] = "/",
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

                var (line, column) = GetLineAndColumn(input, i);
                Token parsedToken = new Token(
                    Value: successfulMatch.Value,
                    Kind: successfulToken!.Value,
                    Line: line,
                    Column: column
                );
                parsedTokens.Add(parsedToken);
            }

            var (endLine, endColumn) = GetLineAndColumn(input, input.Length);
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
