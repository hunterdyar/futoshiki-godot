using Godot;
using System;

public partial class Square : Button
{
	
	private GameManager _manager;
	public Vector2I GridPos => _gridPos;
	private Vector2I _gridPos;

	
	public int Answer => _answer;
	private int _answer = 0;
	private int Max => _manager.Board.size;//largest size, inclusive.
	private bool given;

	public int GetValue() => _manager.Board.GetValue(_gridPos.X, _gridPos.Y);
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//UpdateDisplay();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void UpdateDisplay()
	{
		//update text
		if (GetValue() == 0)
		{
			Text = "";
		}
		else
		{
			Text = GetValue().ToString();
		}
	}
	private void AfterValueChanged()
	{
		UpdateDisplay();
		//inform manager
		_manager.OnValueChanged(this);
	}
	public void Init(Vector2I gridPos, GameManager gameManager)
	{
		_gridPos = gridPos;
		_manager = gameManager;
		Position = _manager.GetSquarePosition(_gridPos);
		given = _manager.Board.GetGiven(_gridPos.X, gridPos.Y);
		Disabled = given;

		UpdateDisplay();
	}

	private void IncrementValue()
	{
		int v = GetValue()+1;
		if (v > Max)
		{
			v = 0;
		}
		_manager.Board.SetValue(_gridPos.X,_gridPos.Y,v,false);
		AfterValueChanged();
	}
	public void _on_pressed()
	{
		IncrementValue();
	}
}
