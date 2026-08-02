using Jam.Core.Audio;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace Jam.Integrations.NodeCanvas
{
    [Category("Jam/Audio")]
    [Name("Play Audio Cue")]
    [Description("Plays a project AudioCue through the shared AudioService.")]
    public sealed class PlayAudioCueTask : ActionTask
    {
        [RequiredField] public BBParameter<AudioCue> cue;
        public BBParameter<Vector3> position;
        [BlackboardOnly] public BBParameter<int> handleId;

        protected override string info => $"Play Audio Cue [{cue}]";

        protected override void OnExecute()
        {
            var service = AudioService.Instance;
            if (service == null || cue.value == null)
            {
                EndAction(false);
                return;
            }

            var handle = service.Play(cue.value, position.value);
            handleId.value = handle.Id;
            EndAction(handle.IsValid);
        }
    }
}
