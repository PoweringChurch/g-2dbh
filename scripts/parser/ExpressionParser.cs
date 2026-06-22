// ExpressionParser.cs
// Recursive-descent parser → AST.
// Grammar (standard precedence, right-assoc power):
//   expr   = term  (('+' | '-') term)*
//   term   = unary (('*' | '/') unary)*
//   unary  = '-' unary | power
//   power  = primary ('^' unary)?
//   primary= NUMBER | IDENT | IDENT '(' expr ')' | '(' expr ')'
using System;
using System.Collections.Generic;

// ── AST nodes ────────────────────────────────────────────────────────────────

public abstract class Expr
{
    public abstract double Eval(Dictionary<string, double> context);
}
public class NumberExpr : Expr
{
    readonly double _v;
    public NumberExpr(double v) => _v = v;
    public override double Eval(Dictionary<string, double> context) => _v;
}

public class VariableExpr : Expr
{
    public string name;
    public VariableExpr(string n) { name = n; }
    public override double Eval(Dictionary<string, double> context)
    {
        if (context != null && context.TryGetValue(name, out double value))
            return value;
        throw new Exception($"Undefined variable '{name}'");
    }
}
public class UnaryExpr : Expr
{
    readonly Expr _operand;
    public UnaryExpr(Expr e) => _operand = e;
    public override double Eval(Dictionary<string, double> context) => -_operand.Eval(context);
}

public class BinaryExpr : Expr
{
    readonly Expr _l, _r;
    readonly char _op;
    public BinaryExpr(char op, Expr l, Expr r) { _op = op; _l = l; _r = r; }
    public override double Eval(Dictionary<string, double> context) => _op switch
    {
        '+' => _l.Eval(context) + _r.Eval(context),
        '-' => _l.Eval(context) - _r.Eval(context),
        '*' => _l.Eval(context) * _r.Eval(context),
        '/' => _l.Eval(context) / _r.Eval(context),
        '%' => _l.Eval(context) % _r.Eval(context),
        '^' => Math.Pow(_l.Eval(context), _r.Eval(context)),
        _   => throw new Exception($"Unknown op '{_op}'")
    };
}

public class FuncExpr : Expr
{
    readonly string _name;
    readonly Expr   _arg;
    public FuncExpr(string name, Expr arg) { _name = name.ToLowerInvariant(); _arg = arg; }
    public override double Eval(Dictionary<string, double> context)
    {
        double a = _arg.Eval(context);
        return _name switch
        {
            "sin"  => Math.Sin(a),  "cos"  => Math.Cos(a),
            "tan"  => Math.Tan(a),  "asin" => Math.Asin(a),
            "acos" => Math.Acos(a), "atan" => Math.Atan(a),
            "sqrt" => Math.Sqrt(a), "abs"  => Math.Abs(a),
            "exp"  => Math.Exp(a),  "log"  => Math.Log(a),
            "log2" => Math.Log2(a), "log10"=> Math.Log10(a),
            "ceil" => Math.Ceiling(a), "floor"=> Math.Floor(a),
            "sign" => Math.Sign(a), "tanh" => Math.Tanh(a),
            _ => throw new Exception($"Unknown function '{_name}'")
        };
    }
}

public class ConstExpr : Expr
{
    readonly double _v;
    public ConstExpr(string name) => _v = name.ToLowerInvariant() switch
    {
        "pi"  => Math.PI,
        "e"   => Math.E,
        "tau" => Math.Tau,
        _ => throw new Exception($"Unknown constant '{name}'")
    };
    public override double Eval(Dictionary<string, double> context) => _v;
}

// Parser 

public class ExpressionParser
{
    readonly List<Token> _tokens;
    int _pos;

    Token Peek => _tokens[_pos];
    Token Consume() => _tokens[_pos++];
    bool Match(TokenType t) { if (Peek.Type == t) { _pos++; return true; } return false; }
    private static Dictionary<string, Expr> _parsedCache = new();
    public ExpressionParser(List<Token> tokens) => _tokens = tokens;
    /// <summary>
    /// Parse expecting t to be the sole variable
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// 
    public static Expr Parse(string expression)
    {
        if (_parsedCache.TryGetValue(expression, out var cached))
            return cached;
        var tokens = ExpressionLexer.Tokenize(expression);
        var parser = new ExpressionParser(tokens);
        var tree   = parser.ParseExpr();
        if (parser.Peek.Type != TokenType.End)
            throw new Exception($"Unexpected token '{parser.Peek.Raw}' after expression");
        _parsedCache[expression] = tree;
        return tree;
    }
    Expr ParseExpr()
    {
        var left = ParseTerm();
        while (Peek.Type is TokenType.Plus or TokenType.Minus)
        {
            char op = Consume().Type == TokenType.Plus ? '+' : '-';
            left = new BinaryExpr(op, left, ParseTerm());
        }
        return left;
    }
    Expr ParseTerm()
    {
        var left = ParseUnary();
        while (Peek.Type is TokenType.Star or TokenType.Slash)
        {
            char op = Consume().Type == TokenType.Star ? '*' : '/';
            left = new BinaryExpr(op, left, ParseUnary());
        }
        return left;
    }
    Expr ParseUnary()
    {
        if (Peek.Type == TokenType.Minus) { Consume(); return new UnaryExpr(ParseUnary()); }
        return ParsePower();
    }
    Expr ParsePower()
    {
        var b = ParsePrimary();
        if (Peek.Type == TokenType.Caret) { Consume(); return new BinaryExpr('^', b, ParseUnary()); }
        return b;
    }
    Expr ParsePrimary()
    {
        if (Peek.Type == TokenType.Number)
            return new NumberExpr(Consume().Value);

        if (Peek.Type == TokenType.LParen)
        {
            Consume();
            var inner = ParseExpr();
            if (!Match(TokenType.RParen)) throw new Exception("Expected ')'");
            return inner;
        }

        if (Peek.Type == TokenType.Identifier)
        {
            string name = Consume().Raw;
            // function call?
            if (Peek.Type == TokenType.LParen)
            {
                Consume();
                var arg = ParseExpr();
                if (!Match(TokenType.RParen)) throw new Exception($"Expected ')' after '{name}('");
                return new FuncExpr(name, arg);
            }
            // named constant?
            if (name is "pi" or "e" or "tau" or "PI" or "E" or "TAU")
                return new ConstExpr(name);

             return new VariableExpr(name);
        }

        throw new Exception($"Unexpected token '{Peek.Raw}' ({Peek.Type})");
    }
}