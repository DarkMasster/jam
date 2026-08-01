using Jam.Core.Localization;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Jam.Integrations.NodeCanvas
{
    [Category("Jam/Localization")]
    [Name("Get Localized String")]
    [Description("Resolves a stable localization key and writes the result to the blackboard.")]
    public sealed class GetLocalizedStringTask : ActionTask
    {
        public BBParameter<string> table = LocalizationTables.Common;
        public BBParameter<string> key;
        public BBParameter<string> fallback;
        [BlackboardOnly] public BBParameter<string> result;

        protected override string info => $"Localize [{table}/{key}]";

        protected override void OnExecute()
        {
            result.value = Loc.Get(table.value, key.value, fallback.value);
            EndAction(true);
        }
    }
}
