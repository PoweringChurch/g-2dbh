using System;
using Godot;

public partial class BackgroundLayerInstance : Parallax2D
{
    public BackgroundLayer Layer;
    public Func<EvalContext, double> scrollx;
    public Func<EvalContext, double> scrolly;
    public Func<EvalContext, double> transparency;
    public Sprite2D Sprite;
    public void ApplyLayerParams()
    {
        var text = RenderingUtils.LoadTexture(Layer.Image);
        scrollx = ExpressionHandler.Compile(ExpressionHandler.Parse(Layer.ScrollFunctionX));
        scrolly = ExpressionHandler.Compile(ExpressionHandler.Parse(Layer.ScrollFunctionY));
        transparency = ExpressionHandler.Compile(ExpressionHandler.Parse(Layer.TransparencyFn));

        Sprite.Texture = text;
        Sprite.Scale = Layer.Scale*Vector2.One;
        if (text != null)
            RepeatSize = text.GetSize()*Scale;
        RepeatTimes = Layer.RepeatCount;
        ZIndex = Layer.Order;
    }
    public void Tick(EvalContext ctx)
    {
        ScrollOffset = (scrollx == null || scrolly == null) ? Vector2.Zero : new((float)scrollx(ctx), (float)scrolly(ctx));
        Modulate = (transparency == null) ? Colors.White : new(1,1,1,1-(float)transparency(ctx));
    }
}