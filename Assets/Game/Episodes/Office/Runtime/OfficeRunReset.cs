using System.Collections.Generic;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Объект, который возвращается в стартовое состояние при перезапуске забега.
    /// </summary>
    public interface IOfficeRunResettable
    {
        void ResetForRun();
    }

    /// <summary>
    /// Реестр участников быстрого restart. Сцена ещё не добавлена в Build Settings,
    /// поэтому забег перезапускается без перезагрузки сцены: каждый участник сам
    /// возвращает своё стартовое состояние.
    /// </summary>
    public static class OfficeRunReset
    {
        private static readonly List<IOfficeRunResettable> Participants = new();

        public static int ParticipantCount => Participants.Count;

        public static void Register(IOfficeRunResettable participant)
        {
            if (participant != null && !Participants.Contains(participant))
            {
                Participants.Add(participant);
            }
        }

        public static void Unregister(IOfficeRunResettable participant)
        {
            Participants.Remove(participant);
        }

        public static void ResetAll()
        {
            for (var i = Participants.Count - 1; i >= 0; i--)
            {
                Participants[i]?.ResetForRun();
            }
        }

        // Отключённый domain reload сохраняет статический список между запусками.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearOnPlay()
        {
            Participants.Clear();
        }
    }
}
