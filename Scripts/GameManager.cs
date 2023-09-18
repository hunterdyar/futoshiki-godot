using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Matrix = System.Collections.Generic.List<System.Collections.Generic.List<int>>;

public partial class GameManager : Node2D
{
	//inspectors
	[Export] public Difficulty difficulty;
	
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
		}
	}

	//move to camera?
	private void CenterCamera()
	{
		var cam = GetNode<Camera2D>("Camera2D");
		var s = _allSquares[0];
		float w = difficulty.size * s.Size.X + (difficulty.size - 1) * _gutter;
		float h = difficulty.size * s.Size.Y + (difficulty.size - 1) * _gutter;
		cam.Position = new Vector2(w / 2, h / 2);

		//todo: calculate scale to fit.
	}

	public Vector2 GetSquarePosition(Vector2I gridPos)
	{
		return new Vector2(GridSize * gridPos.X + gridPos.X * Gutter, GridSize * gridPos.Y + gridPos.Y * Gutter);
	}

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
}
