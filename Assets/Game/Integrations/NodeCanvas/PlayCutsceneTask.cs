using Jam.Core.Cutscenes;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Jam.Integrations.NodeCanvas
{
    [Category("Jam/Cutscenes")]
    [Name("Play Cutscene")]
    [Description("Starts a project cutscene by stable ID and waits for completion or skip.")]
    public sealed class PlayCutsceneTask : ActionTask
    {
        public BBParameter<string> cutsceneId;
        public BBParameter<string> characterId;
        public BBParameter<string> startCheckpointId;
        public BBParameter<string> completionCheckpointId;
        [BlackboardOnly] public BBParameter<string> endReason;
        [BlackboardOnly] public BBParameter<bool> wasSkipped;

        private CutsceneDirector _director;
        private bool _ending;

        protected override string info => $"Play Cutscene [{cutsceneId}]";

        protected override void OnExecute()
        {
            _ending = false;
            _director = CutsceneDirector.Instance;
            if (_director == null)
            {
                EndAction(false);
                return;
            }

            _director.Finished += HandleFinished;
            var context = new CutsceneContext
            {
                characterId = characterId.value,
                startCheckpointId = startCheckpointId.value,
                completionCheckpointId = completionCheckpointId.value
            };

            if (!_director.TryPlay(cutsceneId.value, context, out _))
            {
                _director.Finished -= HandleFinished;
                EndAction(false);
            }
        }

        protected override void OnStop()
        {
            if (_director == null)
            {
                return;
            }

            _director.Finished -= HandleFinished;
            if (!_ending)
            {
                _director.CancelCurrent(cutsceneId.value);
            }

            _director = null;
        }

        private void HandleFinished(CutsceneResult result)
        {
            if (result.CutsceneId != cutsceneId.value)
            {
                return;
            }

            _ending = true;
            endReason.value = result.Reason.ToString();
            wasSkipped.value = result.Reason == CutsceneEndReason.Skipped;
            _director.Finished -= HandleFinished;
            EndAction(result.Succeeded);
        }
    }
}
