using Godot;

namespace futo.Scripts;

public partial class MachineInitializer : Node
{
	[Export]
	public StateMachine Machine;

	public override void _Ready()
	{
		base._Ready();
		Machine.Init();
	}
}