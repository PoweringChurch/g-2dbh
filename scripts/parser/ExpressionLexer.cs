// ExpressionLexer.cs
// Tokenizes a math expression string into a flat token list.
using System;
using System.Collections.Generic;

public enum TokenType
{
    Number, Identifier,
    Plus, Minus, Star, Slash, Percen, Caret,
    LParen, RParen,
    End
}

public readonly struct Token
{
    public readonly TokenType Type;
    public readonly string    Raw;
    public readonly double    Value;   // valid when Type == Number
    public Token(TokenType t, string raw, double v = 0) { Type = t; Raw = raw; Value = v; }
    public override string ToString() => $"{Type}({Raw})";
}

public static class ExpressionLexer
{
    public static List<Token> Tokenize(string src)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < src.Length)
        {
            char c = src[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (char.IsDigit(c) || (c == '.' && i + 1 < src.Length && char.IsDigit(src[i + 1])))
            {
                int start = i;
                while (i < src.Length && (char.IsDigit(src[i]) || src[i] == '.')) i++;
                string raw = src.Substring(start, i - start);
                tokens.Add(new Token(TokenType.Number, raw, double.Parse(raw)));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < src.Length && (char.IsLetterOrDigit(src[i]) || src[i] == '_')) i++;
                tokens.Add(new Token(TokenType.Identifier, src.Substring(start, i - start)));
                continue;
            }

            TokenType tt = c switch
            {
                '+' => TokenType.Plus,  '-' => TokenType.Minus,
                '*' => TokenType.Star,  '/' => TokenType.Slash,
                '%' => TokenType.Percen,'^' => TokenType.Caret, 
                '(' => TokenType.LParen,')' => TokenType.RParen,
                _ => throw new Exception($"Unknown character '{c}' in expression")
            };
            tokens.Add(new Token(tt, c.ToString()));
            i++;
        }
        tokens.Add(new Token(TokenType.End, ""));
        return tokens;
    }
}