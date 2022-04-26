namespace LabParserBoolean
{
    public class Parser
    {
        private readonly IEnumerator<Token> tokens;
        private readonly IList<string> errorsList;

        private Token nextToken;
        private int errorTokensAgo;

        public Parser(IEnumerable<Token> tokens)
        {
            this.tokens = tokens.GetEnumerator();
            this.errorsList = new List<string>();
            this.errorTokensAgo = 0;
        }

        private void MoveNext()
        {
            if (this.tokens.MoveNext())
            {
                this.errorTokensAgo++;
                this.nextToken = this.tokens.Current;
            }
        }

        private void Expect(TokenKind tokenKind)
        {
            if (this.nextToken.Kind == tokenKind)
            {
                MoveNext();
            }
            else
            {
                SyntaxErrorExpected(tokenKind);
            }
        }

        private void SyntaxErrorExpected(TokenKind tokenKind)
        {
            SyntaxError($"Expected {tokenKind} but got '{this.nextToken.Kind}'");
        }

        private void SyntaxErrorProduction(string productionName)
        {
            SyntaxError($"Production '{productionName}' is invalid");
        }

        private void SyntaxError(string errorMessage)
        {
            if (this.errorTokensAgo > 2)
            {
                this.errorsList.Add($"{errorMessage} at line {this.nextToken.Line}, col {this.nextToken.Column}.");
            }
            this.errorTokensAgo = 0;
        }

        public IEnumerable<string> Parse()
        {
            MoveNext();
            Program();
            Expect(TokenKind.Eof);

            return this.errorsList;
        }

        private void Program()
        {
            if (nextToken.Kind == TokenKind.Identifier)
            {
                Statement();
                while(nextToken.Kind == TokenKind.Terminator)
                {
                    MoveNext();
                    Statement();
                }
            }
        }

        private void Statement()
        {
            Expect(TokenKind.Identifier);
            Expect(TokenKind.Assign);
            Expression();
        }

        private void Expression()
        {
            Unary();
            AssignExpression();
        }

        private void AssignExpression()
        {
            OrExpression();
            while (this.nextToken.Kind == TokenKind.Assign)
            {
                MoveNext();
                Unary();
                OrExpression();
            }
        }

        private void OrExpression()
        {
            XorExpression();
            while (this.nextToken.Kind == TokenKind.Or)
            {
                MoveNext();
                Unary();
                XorExpression();
            }
        }

        private void XorExpression()
        {
            AndExpression();
            while (this.nextToken.Kind == TokenKind.Xor)
            {
                MoveNext();
                Unary();
                AndExpression();
            }
        }

        private void AndExpression()
        {
            RelExpression();
            while (this.nextToken.Kind == TokenKind.And)
            {
                MoveNext();
                Unary();
                RelExpression();
            }
        }

        private void RelExpression()
        {
            while (this.nextToken.Kind == TokenKind.RelOp)
            {
                MoveNext();
                Unary();
            }
        }

        private void Unary()
        {
            switch (this.nextToken.Kind)
            {
                case TokenKind.Identifier:
                case TokenKind.Number:
                    MoveNext();
                    break;
                case TokenKind.ParBegin:
                    MoveNext();
                    Expression();
                    Expect(TokenKind.ParEnd);
                    break;
                case TokenKind.Not:
                    MoveNext();
                    Expression();
                    break;
                default: 
                    SyntaxErrorProduction("Unary");
                    break;
            }
        }
    }
}
