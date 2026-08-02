using Jam.Core.Audio;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Jam.Integrations.NodeCanvas
{
    [Category("Jam/Audio")]
    [Name("Set Music")]
    [Description("Crossfades to a project AudioCue or stops the current music.")]
    public sealed class SetMusicTask : ActionTask
    {
        public BBParameter<AudioCue> cue;
        public BBParameter<float> fadeSeconds = 0.5f;
        public bool stop;

        protected override string info => stop ? "Stop Music" : $"Set Music [{cue}]";

        protected override void OnExecute()
        {
            var service = AudioService.Instance;
            if (service == null)
            {
                EndAction(false);
                return;
            }

            if (stop || cue.value == null)
            {
                service.StopMusic(fadeSeconds.value);
            }
            else
            {
                service.PlayMusic(cue.value, fadeSeconds.value);
            }
            EndAction(true);
        }
    }
}
