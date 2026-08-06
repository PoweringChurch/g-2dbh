using Godot;
using System;

public partial class PlayingField : Node
{
	public static readonly Vector2I[] Resolutions =
	[
		new(500	, 900),   // 9:16
		new(900	, 900),   // 1:1
		new(1350, 900),   // 3:2
	];
	public NodePath SubViewportPath = "/root/main/HUD/Sort/SubViewportContainer/SubViewport";
	private SubViewport _svp;
	public Node2D _gameRoot = null;
	private float _resScale = 1;
	private int _currentRatio = 0;
	public override void _Ready()
	{
		_svp = GetNode<SubViewport>(SubViewportPath);
		GetWindow().Unresizable = false;
		GetTree().Root.SizeChanged += OnWindowResized;
		Fit(Resolutions[_currentRatio]);
	}

	/// <summary>Called by GameSession before starting a level.</summary>
	public void SetRatio(int ratioIndex, Node2D GameRoot)
	{
		_currentRatio = ratioIndex;
		_gameRoot = GameRoot;
		if (IsInsideTree()) Fit(Resolutions[_currentRatio]);
	}
	private void Fit(Vector2I res)
	{
		if (!IsInstanceValid(_gameRoot) || _gameRoot == null) return;
		var win = GetTree().Root.GetVisibleRect().Size;
		_resScale = Mathf.Min(win.X / res.X, win.Y / res.Y);
		_svp.Size = (Vector2I)((Vector2)res * _resScale);
		_gameRoot.Scale = _resScale * Vector2.One;
	}

	private void OnWindowResized() => Fit(Resolutions[_currentRatio]);
}
