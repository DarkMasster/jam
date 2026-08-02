using Jam.Core.Save;
using UnityEngine;

namespace Jam.Core.Flow
{
    public static class EpilogueService
    {
        public const string Url = "https://seven-lights-production.up.railway.app";

        public static bool TryOpen()
        {
            if (!GameSaveService.EpilogueUnlocked)
            {
                Debug.LogWarning("Epilogue is locked until the Office and Photo story lines are complete.");
                return false;
            }

            Application.OpenURL(Url);
            return true;
        }
    }
}
