namespace LabParser
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

        private void NoDoubleSub()
        {
            if (this.nextToken.Kind == TokenKind.Sub)
            {
                SyntaxError("Unexpected double '-'");
            }
        }

        private bool IsStartOfStatement()
        {
            TokenKind tokenKind = this.nextToken.Kind;
            return tokenKind == TokenKind.Identifier
                || tokenKind == TokenKind.Const
                || tokenKind == TokenKind.Var
                || tokenKind == TokenKind.Do;
        }

        private bool IsStartOfExpression()
        {
            TokenKind tokenKind = this.nextToken.Kind;
            return tokenKind == TokenKind.Identifier
                || tokenKind == TokenKind.Not
                || tokenKind == TokenKind.Number
                || tokenKind == TokenKind.Sub
                || tokenKind == TokenKind.ParBegin;
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
            while (IsStartOfStatement())
            {
                Statement();
            }
        }

        private void Statement()
        {
            switch (this.nextToken.Kind)
            {
                case TokenKind.Const:
                    ConstInit();
                    Expect(TokenKind.Terminator);
                    break;
                case TokenKind.Var:
                    VarInit();
                    Expect(TokenKind.Terminator);
                    break;
                case TokenKind.Identifier:
                    VarAssign();
                    Expect(TokenKind.Terminator);
                    break;
                case TokenKind.Do:
                    DoWhileLoop();
                    Expect(TokenKind.Terminator);
                    break;
                default:
                    SyntaxErrorProduction("Statement");
                    break;
            }
        }

        private void ConstInit()
        {
            Expect(TokenKind.Const);
            Primary();
            Expect(TokenKind.Assign);
            Expect(TokenKind.Number);
        }

        private void VarInit()
        {
            Expect(TokenKind.Var);
            Expect(TokenKind.Identifier);
            Expect(TokenKind.Assign);
            if (IsStartOfExpression())
            {
                Expression();
            }
            else if (this.nextToken.Kind == TokenKind.IndexerBegin)
            {
                ArrayInitializer();
            }
            else
            {
                SyntaxErrorProduction("Var initilization");
            }
        }

        private void VarAssign()
        {
            Primary();
            Expect(TokenKind.Assign);
            if (IsStartOfExpression())
            {
                Expression();
            }
            else if (this.nextToken.Kind == TokenKind.IndexerBegin)
            {
                ArrayInitializer();
            }
            else
            {
                SyntaxErrorProduction("Var assignment");
            }
        }

        private void ArrayInitializer()
        {
            Expect(TokenKind.IndexerBegin);
            Expect(TokenKind.IndexerEnd);
        }

        private void DoWhileLoop()
        {
            Expect(TokenKind.Do);
            Block();
            Expect(TokenKind.While);
            Expect(TokenKind.ParBegin);
            Expression();
            Expect(TokenKind.ParEnd);
        }

        private void Block()
        {
            Expect(TokenKind.BlockBegin);
            while (IsStartOfStatement())
            {
                Statement();
            }
            Expect(TokenKind.BlockEnd);
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
            AndExpression();
            while (this.nextToken.Kind == TokenKind.Or)
            {
                MoveNext();
                Unary();
                AndExpression();
            }
        }

        private void AndExpression()
        {
            EqlExpression();
            while (this.nextToken.Kind == TokenKind.And)
            {
                MoveNext();
                Unary();
                EqlExpression();
            }
        }

        private void EqlExpression()
        {
            RelExpression();
            while (this.nextToken.Kind == TokenKind.Eq || this.nextToken.Kind == TokenKind.Ne)
            {
                MoveNext();
                Unary();
                RelExpression();
            }
        }

        private void RelExpression()
        {
            AddExpression();
            while (this.nextToken.Kind == TokenKind.RelOp)
            {
                MoveNext();
                Unary();
                AddExpression();
            }
        }

        private void AddExpression()
        {
            MulExpression();
            while (this.nextToken.Kind == TokenKind.Add || this.nextToken.Kind == TokenKind.Sub)
            {
                MoveNext();
                if (this.nextToken.Kind == TokenKind.Sub)
                {
                    NoDoubleSub();
                }
                Unary();
                MulExpression();
            }
        }

        private void MulExpression()
        {
            while (this.nextToken.Kind == TokenKind.Mul || this.nextToken.Kind == TokenKind.Div)
            {
                MoveNext();
                Unary();
            }
        }

        private void Unary()
        {
            if (this.nextToken.Kind == TokenKind.Sub)
            {
                MoveNext();
                NoDoubleSub();
            }
            if (this.nextToken.Kind == TokenKind.Identifier)
            {
                Primary();
            }
            else if (this.nextToken.Kind == TokenKind.Number)
            {
                MoveNext();
            }
            else if (this.nextToken.Kind == TokenKind.ParBegin)
            {
                MoveNext();
                Expression();
                Expect(TokenKind.ParEnd);
            }
            else if (this.nextToken.Kind == TokenKind.Not)
            {
                MoveNext();
                Expression();
            }
            else
            {
                SyntaxErrorProduction("Unary");
            }
        }

        private void Primary()
        {
            Expect(TokenKind.Identifier);
            if (this.nextToken.Kind == TokenKind.IndexerBegin)
            {
                Indexer();
            }
        }

        private void Indexer()
        {
            Expect(TokenKind.IndexerBegin);
            Expression();
            Expect(TokenKind.IndexerEnd);
        }
    }
}
