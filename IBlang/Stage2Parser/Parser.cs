﻿namespace IBlang.Stage2Parser;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using IBlang.Stage1Lexer;

#pragma warning disable CS8618

public class Parser
{
    private readonly Context ctx;

    private Token[] tokens;
    private int currentTokenIndex;
    private Token PeekToken { get; set; }

    public TokenType Peek => tokens[currentTokenIndex].Type;

    public Parser(Context ctx)
    {
        tokens = Array.Empty<Token>();
        this.ctx = ctx;
    }

    public Ast Parse(Token[] tokens)
    {
        currentTokenIndex = 0;
        this.tokens = tokens;
        PeekToken = tokens[currentTokenIndex];
        List<FunctionDecleration> functions = new();
        while (true)
        {
            switch (Peek)
            {
                case TokenType.KeywordFunc: functions.Add(ParseFuncDecleration()); break;
                default: return new Ast(functions.ToArray());
            }
        }

        throw new CompilerDebugException("Unreachable");
    }

    private FunctionDecleration ParseFuncDecleration()
    {
        ctx.TraceParser();

        EatToken(TokenType.KeywordFunc);
        string identifier = EatIdentifier();
        EatToken(TokenType.OpenParenthesis);

        List<ParameterDecleration> parameters = new();

        while (Peek != TokenType.CloseParenthesis)
        {
            parameters.Add(new(EatIdentifier(), EatIdentifier()));
        }

        EatToken(TokenType.CloseParenthesis);

        BlockStatement body = ParseBlock();

        return new FunctionDecleration(identifier, parameters.ToArray(), body);
    }


    private BlockStatement ParseBlock()
    {
        ctx.TraceParser();

        EatToken(TokenType.OpenScope);

        List<INode> statements = new();
        while (Peek != TokenType.CloseScope)
        {
            statements.Add(ParseStatement());
        }

        EatToken(TokenType.CloseScope);

        return new BlockStatement(statements.ToArray());
    }

    private INode ParseStatement()
    {
        ctx.TraceParser();

        return Peek switch
        {
            TokenType.Identifier => ParseIdentifier(EatIdentifier()),
            TokenType.KeywordIf => ParseIf(),
            TokenType.KeywordReturn => ParseReturn(),
            _ => throw new NotImplementedException(Peek.ToString())
        };
    }

    private INode ParseIdentifier(string identifier)
    {
        ctx.TraceParser();

        return Peek switch
        {
            TokenType.OpenParenthesis => ParseFuncCall(identifier),
            TokenType.Assignment => ParseVariableDecleration(identifier),
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// IF [EXPRESSION] BLOCK <br/>
    /// Optional ELSE BLOCK
    /// </summary>
    private IfStatement ParseIf()
    {
        ctx.TraceParser();

        EatToken(TokenType.KeywordIf);

        INode condition = ParseExpression();

        BlockStatement body = ParseBlock();
        BlockStatement? elseBody = null;

        if (Peek == TokenType.KeywordElse)
        {
            EatToken(TokenType.KeywordElse);
            elseBody = ParseBlock();
        }

        return new IfStatement(condition, body, elseBody);
    }

    /// <summary> RETURN [STATEMENT] </summary>
    private ReturnStatement ParseReturn()
    {
        ctx.TraceParser();

        EatToken(TokenType.KeywordReturn);
        return new ReturnStatement(ParseExpression());
    }

    private INode ParseExpression()
    {
        ctx.TraceParser();

        INode left = ParseUnaryExpression();

        if (TokenHelper.IsBinaryToken(Peek))
        {
            Token op = NextToken();
            return new BinaryExpression(left, op.Value, ParseUnaryExpression());
        }
        else
        {
            return left;
        }
    }

    private INode ParseUnaryExpression()
    {
        Token token = NextToken();
        return token.Type switch
        {
            TokenType.Identifier => Peek switch
            {
                TokenType.OpenParenthesis => ParseFuncCall(token.Value),
                _ => new Identifier(token.Value),
            },
            TokenType.IntegerLiteral => new ValueLiteral(ValueType.Int, token.Value),
            TokenType.FloatLiteral => new ValueLiteral(ValueType.Float, token.Value),
            TokenType.StringLiteral => new ValueLiteral(ValueType.String, token.Value),
            _ => new GarbageExpression(token),
        };
    }

    private INode ParseFuncCall(string identifier)
    {
        ctx.TraceParser();

        EatToken(TokenType.OpenParenthesis);

        List<INode> args = new();
        while (Peek != TokenType.CloseParenthesis)
        {
            args.Add(ParseExpression());
        }

        EatToken(TokenType.CloseParenthesis);
        return new FunctionCallExpression(identifier, args.ToArray());
    }

    private INode ParseVariableDecleration(string identifier)
    {
        ctx.TraceParser();

        EatToken(TokenType.Assignment);

        INode rhs = ParseStatement();

        return new AssignmentExpression(new Identifier(identifier), rhs);
    }

    private void EatToken(TokenType expected, [CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string method = "")
    {
        Token got = NextToken();

        if (got.Type != expected)
        {
            Log.Error($"Expected '{string.Join(' ', expected)}' but got '{got}'", file, lineNumber, method);
        }
    }

    private string EatIdentifier([CallerFilePath] string file = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string method = "")
    {
        Token got = NextToken();
        const TokenType expected = TokenType.Identifier;
        if (got.Type != expected)
        {
            Log.Error($"Expected '{expected}' but got '{got}'", file, lineNumber, method);
        }
        return got.Value;
    }

    private ValueLiteral EatConstant()
    {
        string value = PeekToken.Value;

        TokenType got = NextToken().Type;

        switch (got)
        {
            case TokenType.StringLiteral: return new(ValueType.String, value);
            case TokenType.CharLiteral: return new(ValueType.Char, value);
            case TokenType.IntegerLiteral: return new(ValueType.Int, value);
            default: Log.Error($"Expected 'Identifier' but got '{got}'"); break;
        }

        return new(ValueType.String, value);
    }

    public Token NextToken()
    {
        Token token = PeekToken;
        ctx.LogToken("Used -> " + token.ToString());
        currentTokenIndex++;
        PeekToken = tokens[currentTokenIndex];
        return token;
    }

}
#pragma warning restore CS8618
