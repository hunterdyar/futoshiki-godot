using Godot;

namespace futo.Scripts;

public partial class StateMachine : Resource
{
	[Export]
	private State[] _states;

	private State current = null;
	public void Init()
	{
		current = null;
		foreach (var state in _states)
		{
			state.Init(this);
			if (state.IsDefault)
			{
				if (current != null)
				{
					GD.Print("WARNING More than one default state in sm",this);
					continue;
				}

				current = state;
				current.OnEnter();
			}
		}

		
		if (current == null)
		{
			GD.Print("WARNING no default state");
			//use first in list as default
			if (_states.Length > 0)
			{
				SwitchToState(_states[0]);
			}
		}
	}

	public void SwitchToState(State state)
	{
		if (current == state)
		{
			return;
		}
		
		if (current != null)
		{
			current.OnExit();
		}

		current = state;
		current.OnEnter();
	}
}