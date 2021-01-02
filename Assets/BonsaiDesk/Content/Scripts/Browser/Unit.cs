using UnityEngine;
using UnityEngine.Serialization;

public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}

public class StateMachine
{
    private IState currentState;

    public void ChangeState(IState newState)
    {
        if (currentState != null)
            currentState.Exit();

        currentState = newState;
        currentState.Enter();
    }

    public void Update()
    {
        if (currentState != null) currentState.Execute();
    }
}

public class TestState : IState
{
    private Unit owner;

    private AutoBrowser _autoBrowser;

    public TestState(Unit owner, AutoBrowser autoBrowser)
    {
        this.owner = owner;
        this._autoBrowser = autoBrowser;
    }

    public void Enter()
    {
        Debug.Log("entering test state");
    }

    public void Execute()
    {
        Debug.Log("updating test state");
    }

    public void Exit()
    {
        Debug.Log("exiting test state");
    }
}

public class Unit : MonoBehaviour
{
    private AutoBrowser autoBrowser;
    
    private readonly StateMachine stateMachine = new StateMachine();

    private void Start()
    {
        stateMachine.ChangeState(new TestState(this, autoBrowser));
    }

    private void Update()
    {
        stateMachine.Update();
    }
}