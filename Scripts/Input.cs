using Godot;
using System;

public partial class Input : Node2D
{
	private GameManager _manager;

	private Square selectedSquare;
	public override void _Ready(){
		_manager = GetNode<GameManager>("/root/Main");

	}

	public override void _Process(double delta)
	{
		if (Godot.Input.IsActionJustPressed("ui_right"))
		{
			MoveInput(1, 0);
		}else if (Godot.Input.IsActionJustPressed("ui_left"))
		{
			MoveInput(-1, 0);
		}else if (Godot.Input.IsActionJustPressed("ui_up"))
		{
			MoveInput(0, 1);
		}else if (Godot.Input.IsActionJustPressed("ui_down"))
		{
			MoveInput(0, -1);
		}
	}

	private void MoveInput(int dx, int dy)
	{
		dy = -dy;//flip for convenience
		if (selectedSquare == null)
		{
			SelectSquare(0,0);
		}
		int x = selectedSquare.GridPos.X + dx;
		int y = selectedSquare.GridPos.Y + dy;
		if (x < 0)
		{
			x = _manager.difficulty.size - 1;
		}
		if (y < 0)
		{
			y = _manager.difficulty.size - 1;
		}
		if (x > _manager.difficulty.size - 1)
		{
			x = 0;
		}
		if (y > _manager.difficulty.size - 1)
		{
			y = 0;
		}

		SelectSquare(x, y);
	}

	//Why am i doing input in two different ways?
	public override void _Input(InputEvent @event)
	{
		if (selectedSquare == null)
		{
			return;
		}
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Key0)
			{
				selectedSquare.SetValue(0);
			}
			if (keyEvent.Keycode == Key.Key1)
			{
				selectedSquare.SetValue(1);
			}
			if (keyEvent.Keycode == Key.Key2)
			{
				selectedSquare.SetValue(2);
			}
			if (keyEvent.Keycode == Key.Key3)
			{
				selectedSquare.SetValue(3);
			}
			if (keyEvent.Keycode == Key.Key4)
			{
				selectedSquare.SetValue(4);
			}
			if (keyEvent.Keycode == Key.Key5)
			{
				selectedSquare.SetValue(5);
			}
			if (keyEvent.Keycode == Key.Key6)
			{
				selectedSquare.SetValue(6);
			}
		}
	}
	
	public void SelectSquare(int x, int y)
	{
		SelectSquare(new Vector2I(x,y));
	}
	
	public void SelectSquare(Vector2I pos)
	{
		selectedSquare = _manager.GetSquare(pos);
		Position = selectedSquare.Position;
		
		//update Draw()
		QueueRedraw();
	}
	
	public override void _Draw()
	{
		float size = _manager.GridSize + _manager.Gutter / 4;
		DrawRect(new Rect2(new Vector2(_manager.GridSize/2-size/2,_manager.GridSize/2-size/2),size,size),Colors.White);
	}
}
