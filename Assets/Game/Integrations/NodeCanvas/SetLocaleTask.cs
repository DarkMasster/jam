using Jam.Core.Localization;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace Jam.Integrations.NodeCanvas
{
    [Category("Jam/Localization")]
    [Name("Set Locale")]
    [Description("Changes the UI and narrative locale without touching story progress.")]
    public sealed class SetLocaleTask : ActionTask
    {
        public BBParameter<string> localeCode = "ru";

        protected override string info => $"Locale [{localeCode}]";

        protected override void OnExecute()
        {
            EndAction(Loc.SetLocale(localeCode.value));
        }
    }
}
