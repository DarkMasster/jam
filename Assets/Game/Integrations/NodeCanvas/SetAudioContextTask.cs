using Jam.Core.Audio;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Jam.Integrations.NodeCanvas
{
    [Category("Jam/Audio")]
    [Name("Set Audio Context")]
    [Description("Sets or clears a named mix context. Paused has priority over Cutscene.")]
    public sealed class SetAudioContextTask : ActionTask
    {
        [RequiredField] public BBParameter<string> ownerId = "nodecanvas";
        public AudioMixContext context = AudioMixContext.Cutscene;
        public bool clear;
        public BBParameter<float> transitionSeconds = 0.15f;

        protected override string info => clear
            ? $"Clear Audio Context [{ownerId}]"
            : $"Audio Context {context} [{ownerId}]";

        protected override void OnExecute()
        {
            var service = AudioService.Instance;
            if (service == null || string.IsNullOrWhiteSpace(ownerId.value))
            {
                EndAction(false);
                return;
            }

            if (clear)
            {
                service.ClearContext(ownerId.value, transitionSeconds.value);
            }
            else
            {
                service.SetContext(ownerId.value, context, transitionSeconds.value);
            }
            EndAction(true);
        }
    }
}
