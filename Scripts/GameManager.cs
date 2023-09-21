using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using futo.Scripts;
using Matrix = System.Collections.Generic.List<System.Collections.Generic.List<int>>;

public partial class GameManager : Node2D
{
	//inspectors
	[Export] public Difficulty difficulty;
	
	//I think in this case the state machine resource is absolutely useless, since everything has a reference to this class.
	//this is a) unity-brain and b) i just wanted to figure resources out a bit.
	[Export] private State VictoryState;
	
	private PackedScene _square = ResourceLoader.Load<PackedScene>("res://Scenes/square_button.tscn");
	private PackedScene _hint = ResourceLoader.Load<PackedScene>("res://Scenes/hint.tscn");

	private List<Square> _allSquares = new List<Square>();
	private List<Hint> _allHints = new List<Hint>();
	private Dictionary<Vector2I, Square> _squareMap = new Dictionary<Vector2I, Square>();

	public Board Board => _board;
	private Board _board;

	public float GridSize => _gridSize;
	[Export]
	private float _gridSize = 100;
	
	public float Gutter => _gutter;
	[Export]
	private float _gutter = 40;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_board = Board.Generate(difficulty);
		for (int i = 0; i < difficulty.size; i++)
		{
			for (int j = 0; j < difficulty.size; j++)
			{
				var pos = new Vector2I(i, j);
				InstantiateSquare(pos);
			}
		}
		
		foreach (var c in _board.comparisons)
		{
			InstantiateHint(c);
		}
		
		CenterCamera();
	}

	private void InstantiateHint(Board.Comparison comp)
	{
		var hint = (Hint)_hint.Instantiate();
		AddChild(hint);
		_allHints.Add(hint);
		hint.Init(this,comp);
	}

	private void InstantiateSquare(Vector2I position)
	{
		var square = (Square)_square.Instantiate();
		AddChild(square);
		_allSquares.Add(square);
		_squareMap.Add(position, square);
		square.Init(position,this);
	}
	
	public void OnValueChanged(Square updatedSquare)
	{
		//do victory checks.
		if (_board.IsSolved())
		{
			GD.Print("You solved it!");
			VictoryState.EnterState();
		}
	}

	//move to camera?
	private void CenterCamera()
	{
		var cam = GetNode<Camera2D>("Camera2D");
		var s = _allSquares[0];
		float w = difficulty.size * GridSize + (difficulty.size - 1) * _gutter;
		float h = difficulty.size * GridSize + (difficulty.size - 1) * _gutter;
		cam.Position = new Vector2(w / 2, h / 2);

		//todo: calculate scale to fit.
	}

	public Vector2 GetSquarePosition(Vector2I gridPos)
	{
		return new Vector2(GridSize * gridPos.X + gridPos.X * Gutter, GridSize * gridPos.Y + gridPos.Y * Gutter);
	}

	// public Vector2I GetGridPosition(Vector2 childPos)
	// {
	// 	// pos = (GridSize * gridPos.X) + (gridPos.X * Gutter);
	// 	// pos / gridpos = GridSize + _gutter;
	// 	// 1/gridpos = (GridSize+_gutter)/pos
	//
	// 	
	// }

	public Square GetSquare(Vector2I pos)
	{
		if (_squareMap.TryGetValue(pos, out var square))
		{
			return square;
		}
		else
		{
			GD.PrintErr("Can't get square",pos);
			return null;
		}
	}

	public bool TryGetNodeAtViewPosition(Vector2 viewPos,out Square square)
	{
		square = null;
		//already i want to write a utility class/extension methods
		var childPos = GetViewportTransform().AffineInverse() * viewPos;
		var x = Mathf.FloorToInt(childPos.X / (GridSize + _gutter));
		var y = Mathf.FloorToInt(childPos.Y / (GridSize + _gutter));
		return _squareMap.TryGetValue(new Vector2I(x, y), out square);
	}
}
