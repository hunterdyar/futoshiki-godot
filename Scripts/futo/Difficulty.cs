using Godot;

public partial class Difficulty : Resource
{
	[Export]
	public Board.GameType gameType = Board.GameType.Futoshiki;

	[Export]
	public int size;

	[Export]
	public float comparisonFactor;

	[Export]
	public float numberFactor;

	[Export]
	public int numberRemovalMaxBuffer;
}