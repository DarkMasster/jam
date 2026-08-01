namespace Jam.Core.Save
{
    public interface IGameModeSaveProvider
    {
        bool CanSave { get; }
        string ModeName { get; }
        bool TrySave(out string message);
    }
}
