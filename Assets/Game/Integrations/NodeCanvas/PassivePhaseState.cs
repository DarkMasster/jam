using NodeCanvas.StateMachines;
using ParadoxNotion.Design;

namespace Jam.Integrations.NodeCanvas
{
    [Name("Passive Phase")]
    [Description("A named FSM phase that remains active until another state is triggered by project code.")]
    public sealed class PassivePhaseState : FSMState
    {
        protected override void OnEnter()
        {
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnExit()
        {
        }
    }
}
