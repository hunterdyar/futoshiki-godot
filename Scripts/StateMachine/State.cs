using Godot;

namespace futo.Scripts;

public partial class State : Resource
{
	public bool IsDefault => _isDefault;
	[Export] private bool _isDefault;

	public bool IsActive => _isActive;
	private bool _isActive;

	public StateMachine Machine => _machine;
	private StateMachine _machine;

	public void EnterState()
	{
		_machine.SwitchToState(this);
	}

	public void Init(StateMachine stateMachine)
	{
		_machine = stateMachine;
	}

	public void OnExit()
	{
		_isActive = false;
	}

	public void OnEnter()
	{
		_isActive = true;
	}
}