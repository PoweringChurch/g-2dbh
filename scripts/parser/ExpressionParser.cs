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
    public abstract double Eval(EvalContext context);
}
public class NumberExpr : Expr
{
    public readonly double v;
    public NumberExpr(double v) => this.v = v;
    public override double Eval(EvalContext context) => v;
}

public class VariableExpr : Expr
{
    public string name;
    public VariableExpr(string n) { name = n; }
    public override double Eval(EvalContext ctx) => name switch
    {
        "t" => ctx.T,
        "i" => ctx.I,
        "n" => ctx.N,
        "l" => ctx.L,
        _ => throw new NotSupportedException($"Unknown variable: {name}")
    };
}
/// <summary>
/// i.e. Negative
/// </summary>
public class UnaryExpr : Expr
{
    public readonly Expr op;
    public UnaryExpr(Expr e) => op = e;
    public override double Eval(EvalContext context) => -op.Eval(context);
}

public class BinaryExpr : Expr
{
    public readonly Expr l, r;
    public readonly char op;
    public BinaryExpr(char op, Expr l, Expr r) { this.op = op; this.l = l; this.r = r; }
    public override double Eval(EvalContext context) => op switch
    {
        '+' => l.Eval(context) + r.Eval(context),
        '-' => l.Eval(context) - r.Eval(context),
        '*' => l.Eval(context) * r.Eval(context),
        '/' => l.Eval(context) / r.Eval(context),
        '%' => l.Eval(context) % r.Eval(context),
        '^' => Math.Pow(l.Eval(context), r.Eval(context)),
        _   => throw new Exception($"Unknown op '{op}'")
    };
}

public class FuncExpr : Expr
{
    public readonly string name;
    public readonly Expr   arg;
    public FuncExpr(string name, Expr arg) { this.name = name.ToLowerInvariant(); this.arg = arg; }
    public override double Eval(EvalContext context)
    {
        double a = arg.Eval(context);
        return name switch
        {
            "sin"  => Math.Sin(a),  "cos"  => Math.Cos(a),
            "tan"  => Math.Tan(a),  "asin" => Math.Asin(a),
            "acos" => Math.Acos(a), "atan" => Math.Atan(a),
            "sqrt" => Math.Sqrt(a), "abs"  => Math.Abs(a),
            "exp"  => Math.Exp(a),  "log"  => Math.Log(a),
            "log2" => Math.Log2(a), "log10"=> Math.Log10(a),
            "ceil" => Math.Ceiling(a), "floor"=> Math.Floor(a),
            "sign" => Math.Sign(a), "tanh" => Math.Tanh(a),
            _ => throw new Exception($"Unknown function '{name}'")
        };
    }
}

public class ConstExpr : Expr
{
    public readonly double v;
    public ConstExpr(string name) => v = name.ToLowerInvariant() switch
    {
        "pi"  => Math.PI,
        "e"   => Math.E,
        "tau" => Math.Tau,
        _ => throw new Exception($"Unknown constant '{name}'")
    };
    public override double Eval(EvalContext context) => v;
}

public struct EvalContext
{
    public double T; // time passed since start of projectile OR pattern
    public double I; // index of a projectile in a pattern
    public double N; // amount of projectiles in a pattern
    public double L; // lifetime
}
// Handler
public class ExpressionHandler
{
    public readonly List<Token> tokens;
    int _pos;

    Token Peek => tokens[_pos];
    Token Consume() => tokens[_pos++];
    bool Match(TokenType t) { if (Peek.Type == t) { _pos++; return true; } return false; }
    private static Dictionary<string, Expr> _parsedCache = new();
    public ExpressionHandler(List<Token> tokens) => this.tokens = tokens;
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
        var parser = new ExpressionHandler(tokens);
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
    public static Func<EvalContext, double> Compile(Expr expr)
    {
        if (expr == null)
            return _ => 0.0;

        switch (expr)
        {
            case NumberExpr n:
            {
                double val = n.v;
                return _ => val;
            }
            case ConstExpr c:
            {
                double val = c.v;
                return _ => val;
            }
            case UnaryExpr u:
            {
                var negFn = Compile(u.op);
                return ctx => -negFn(ctx);
            }
            case VariableExpr v:
                return v.name switch
                {
                    "t" => ctx => ctx.T,
                    "i" => ctx => ctx.I,
                    "n" => ctx => ctx.N,
                    "l" => ctx => ctx.L,
                    _ => throw new NotSupportedException($"Unknown variable: {v.name}")
                };

            case BinaryExpr b:
            {
                var leftFn = Compile(b.l);
                var rightFn = Compile(b.r);
                return b.op switch
                {
                    '+' => ctx => leftFn(ctx) + rightFn(ctx),
                    '-' => ctx => leftFn(ctx) - rightFn(ctx),
                    '*' => ctx => leftFn(ctx) * rightFn(ctx),
                    '/' => ctx => leftFn(ctx) / rightFn(ctx),
                    '%' => ctx => leftFn(ctx) % rightFn(ctx),
                    '^' => ctx => Math.Pow(leftFn(ctx), rightFn(ctx)),
                    _ => throw new NotSupportedException($"Unknown op: {b.op}")
                };
            }
            case FuncExpr f:
            {
                var name = f.name;
                var argFn = Compile(f.arg);
                return ctx => name switch
                {
                    "sin"  => Math.Sin(argFn(ctx)),  "cos"  => Math.Cos(argFn(ctx)),
                    "tan"  => Math.Tan(argFn(ctx)),  "asin" => Math.Asin(argFn(ctx)),
                    "acos" => Math.Acos(argFn(ctx)), "atan" => Math.Atan(argFn(ctx)),
                    "sqrt" => Math.Sqrt(argFn(ctx)), "abs"  => Math.Abs(argFn(ctx)),
                    "exp"  => Math.Exp(argFn(ctx)),  "log"  => Math.Log(argFn(ctx)),
                    "log2" => Math.Log2(argFn(ctx)), "log10"=> Math.Log10(argFn(ctx)),
                    "ceil" => Math.Ceiling(argFn(ctx)), "floor"=> Math.Floor(argFn(ctx)),
                    "sign" => Math.Sign(argFn(ctx)), "tanh" => Math.Tanh(argFn(ctx)),
                    _ => throw new Exception($"Unknown function '{name}'")
                };
            }
            default:
                throw new NotSupportedException($"Unknown expr node: {expr.GetType()}");
        }
    }
}
