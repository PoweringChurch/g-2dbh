// ExpressionParser.cs
// Recursive-descent parser → AST.
// Grammar (standard precedence, right-assoc power):
//   expr   = term  (('+' | '-') term)*
//   term   = unary (('*' | '/' | '%' ) unary)*
//   unary  = '-' unary | power
//   power  = primary ('^' unary)?
//   primary= NUMBER | IDENT | IDENT '(' expr ')' | '(' expr ')'
using System;
using System.Collections.Generic;

// AST nodes

public static class Functions
{
    public static double Hash(double x)
    {
        ulong h = unchecked((ulong)BitConverter.DoubleToInt64Bits(x));
        h ^= h >> 33;
        h *= 0xff51afd7ed558ccdUL;
        h ^= h >> 33;
        h *= 0xc4ceb9fe1a85ec53UL;
        h ^= h >> 33;
        return (h >> 11) * (1.0 / (1UL << 53));
    }
}
public struct EvalContext
{
    public double T; // time passed since start of projectile OR pattern
    public double I; // index of a projectile in a pattern
    public double N; // amount of projectiles in a pattern
    public double L; // lifetime of a projectile
    public double Unique; // a unique number generated from the spawning conditions of a reference.
}

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
        "unique" => ctx.Unique,
        _ => throw new NotSupportedException($"Unknown variable: {name}")
    };
}
public class CustomVariableExpr : Expr
{
    public readonly static Dictionary<string, Expr> Definitions = [];
    public string name;
    public CustomVariableExpr(string n) { name = n; }
    public override double Eval(EvalContext context) 
    {
        if (Definitions.TryGetValue(name, out Expr v))
        {
            if (v is CustomVariableExpr cv && cv.name == name)
                throw new NotSupportedException($"Variable '{name}' cannot reference itself.");
            return v.Eval(context);
        }
        else throw new NotSupportedException($"Unknown variable: {name}");
    }
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
        '/' => MathSafe.Div(l.Eval(context), r.Eval(context)),
        '%' => MathSafe.Mod(l.Eval(context), r.Eval(context)),
        '^' => MathSafe.Pow(l.Eval(context), r.Eval(context)),
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
            "tan"  => Math.Tan(a),  "asin" => MathSafe.Asin(a),
            "acos" => MathSafe.Acos(a), "atan" => Math.Atan(a),
            "sqrt" => MathSafe.Sqrt(a), "abs"  => Math.Abs(a),
            "exp"  => Math.Exp(a),  "log"  => MathSafe.Log(a),
            "log2" => MathSafe.Log2(a), "log10"=> MathSafe.Log10(a),
            "ceil" => Math.Ceiling(a), "floor"=> Math.Floor(a),
            "sign" => Math.Sign(a), "tanh" => Math.Tanh(a),
            "hash" => Functions.Hash(a),
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
        "tau" => Math.Tau,
        "phi" => 1.61803399,
        "deg2rad" => 0.017453292,
        "rad2deg" => 57.29578,
        _ => throw new Exception($"Unknown constant '{name}'")
    };
    public override double Eval(EvalContext context) => v;
}
// Handler
public class ExpressionHandler(List<Token> tokens)
{
    public readonly List<Token> tokens = tokens;
    int _pos;

    Token Peek => tokens[_pos];
    Token Consume() => tokens[_pos++];
    bool Match(TokenType t) { if (Peek.Type == t) { _pos++; return true; } return false; }
    private static Dictionary<string, Expr> _parsedCache = new();

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
        while (Peek.Type is TokenType.Star or TokenType.Slash or TokenType.Percen)
        {
            var type = Consume().Type;

            char op = '*';
            if (type == TokenType.Slash)
                op = '/';
            else if (type == TokenType.Percen)
                op = '%';
            
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
            if (name is "pi" or "tau" or "phi" or "deg2rad" or "rad2deg")
                return new ConstExpr(name);
            // variable?
            if (name is "t" or "i" or "n" or "l" or "unique")
                return new VariableExpr(name);
            return new CustomVariableExpr(name);
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
                    "unique" => ctx => ctx.Unique,
                    _ => throw new NotSupportedException($"Unknown variable: {v.name}")
                };
            case CustomVariableExpr c:
            {
                if (!CustomVariableExpr.Definitions.TryGetValue(c.name, out Expr v))
                    throw new NotSupportedException($"Unknown variable: {c.name}");
                if (v is CustomVariableExpr cv && cv.name == c.name)
                    throw new NotSupportedException($"Variable '{c.name}' cannot reference itself.");
                var valFn = Compile(v);
                return ctx => valFn(ctx);
            }
            case BinaryExpr b:
            {
                var leftFn = Compile(b.l);
                var rightFn = Compile(b.r);
                return b.op switch
                {
                    '+' => ctx => leftFn(ctx) + rightFn(ctx),
                    '-' => ctx => leftFn(ctx) - rightFn(ctx),
                    '*' => ctx => leftFn(ctx) * rightFn(ctx),
                    '/' => ctx => MathSafe.Div(leftFn(ctx), rightFn(ctx)),
                    '%' => ctx => MathSafe.Mod(leftFn(ctx), rightFn(ctx)),
                    '^' => ctx => MathSafe.Pow(leftFn(ctx), rightFn(ctx)),
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
                    "tan"  => Math.Tan(argFn(ctx)),  "asin" => MathSafe.Asin(argFn(ctx)),
                    "acos" => MathSafe.Acos(argFn(ctx)), "atan" => Math.Atan(argFn(ctx)),
                    "sqrt" => MathSafe.Sqrt(argFn(ctx)), "abs"  => Math.Abs(argFn(ctx)),
                    "exp"  => Math.Exp(argFn(ctx)),  "log"  => MathSafe.Log(argFn(ctx)),
                    "log2" => MathSafe.Log2(argFn(ctx)), "log10"=> MathSafe.Log10(argFn(ctx)),
                    "ceil" => Math.Ceiling(argFn(ctx)), "floor"=> Math.Floor(argFn(ctx)),
                    "sign" => Math.Sign(argFn(ctx)), "tanh" => Math.Tanh(argFn(ctx)),
                    "hash" => Functions.Hash(argFn(ctx)),
                    _ => throw new Exception($"Unknown function '{name}'")
                };
            }
            default:
                throw new NotSupportedException($"Unknown expr node: {expr.GetType()}");
        }
    }
}

public static class MathSafe
{
    public static double Mod(double l, double r) => r == 0.0 ? 0.0 : l % r;
    public static double Div(double l, double r) => r == 0.0 ? 0.0 : l / r;
    public static double Pow(double b, double e)
    {
        if (b == 0.0 && e < 0.0) return 0.0;
        if (b < 0.0 && e != Math.Floor(e)) return 0.0;
        double result = Math.Pow(b, e);
        return double.IsFinite(result) ? result : 0.0;
    }
    public static double Sqrt(double a) => a < 0.0 ? 0.0 : Math.Sqrt(a);
    public static double Log(double a)   => a <= 0.0 ? 0.0 : Math.Log(a);
    public static double Log2(double a)  => a <= 0.0 ? 0.0 : Math.Log2(a);
    public static double Log10(double a) => a <= 0.0 ? 0.0 : Math.Log10(a);
    public static double Asin(double a) => Math.Asin(Math.Clamp(a, -1.0, 1.0));
    public static double Acos(double a) => Math.Acos(Math.Clamp(a, -1.0, 1.0));
    public static double Sanitize(double v) => double.IsFinite(v) ? v : 0.0;
}