using Godot;
using System;

public partial class Hint : Label
{
	[Export] private Color _color;
	private Square _a;
	private Square _b;
	private bool _vertical;
	private GameManager _manager;
	private Board.Comparison _comparison;
	//we expect a to be above or left of b.
	public void Init(GameManager manager, Board.Comparison comp)
	{
		_manager = manager;
		_a = manager.GetSquare(new Vector2I(comp.pos1.Item1,comp.pos1.Item2));
		_b = manager.GetSquare(new Vector2I(comp.pos2.Item1, comp.pos2.Item2));

		_comparison = comp;
		
		Size = new Vector2(manager.Gutter, manager.Gutter);
		Position = (_a.Position + _b.Position)/2 + new Vector2(manager.GridSize/2, manager.GridSize /2)-new Vector2(manager.Gutter/2, manager.Gutter /2);

		Vector2I dir = new Vector2I(comp.pos1.Item1 - comp.pos2.Item1, comp.pos1.Item2 - comp.pos2.Item2);

		if (manager.Board.gameType == Board.GameType.Futoshiki)
		{
			if (comp.comparator == Board.Comparator.LessThan)
			{
				dir = -dir;
			}

			if (dir.X == 0)
			{
				if (dir.Y > 0)
				{
					Text = "^";
				}
				else
				{
					Text = "v";
				}
			}
			else //y = 0
			{
				if (dir.X > 0)
				{
					Text = "<";
				}
				else
				{
					Text = ">";
				}
			}
		}else if (manager.Board.gameType == Board.GameType.Renzoku)
		{
			//handled in _Draw()
			Text = "";
		}
	}

	public override void _Draw()
	{
		base._Draw();
		switch (_manager.Board.gameType)
		{
			case Board.GameType.Renzoku:
				if (_comparison.comparator == Board.Comparator.Adjacent)
				{
					DrawCircle(new Vector2(_manager.Gutter / 2, _manager.Gutter / 2), _manager.Gutter / 3,
						_color);
				}
				break;
		}
	}
}
