using Godot;
using System;

public partial class PlayingField : Node
{
	public static readonly Vector2I[] Resolutions =
	[
		new(506  , 900 ),   // 9:16
		new(900 , 900 ),   // 1:1
		new(1350 , 900 ),   // 3:2
	];
	[Export] public SubViewport Viewport  { get; set; }
	public Node2D _gameRoot 	= null;
	private float _resScale 	= 1;
	private int _currentRatio 	= 0;
	public Vector2I CurrentResolution => Resolutions[_currentRatio];

	public override void _Ready()
	{
		GetWindow().Unresizable = false;
		GetTree().Root.SizeChanged += OnWindowResized;
		Fit(Resolutions[_currentRatio]);
	}

	/// <summary>Called by LevelLoader before starting a level.</summary>
	public void SetRatio(int ratioIndex, Node2D GameRoot)
	{
		_currentRatio = ratioIndex;
		_gameRoot = GameRoot;
		if (IsInsideTree()) Fit(Resolutions[_currentRatio]);
	}
	private void Fit(Vector2I res)
	{
		if (_gameRoot == null) return;
        var win    = GetTree().Root.GetVisibleRect().Size;
		_resScale = Mathf.Min(win.X / res.X, win.Y / res.Y);
		Viewport.Size = (Vector2I)((Vector2)res*_resScale);
		_gameRoot.Scale = _resScale*Vector2.One;
	}

	private void OnWindowResized() => Fit(Resolutions[_currentRatio]);
}
