using System;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Устойчивые фазы офисного эпизода. Боевые состояния босса сюда не попадают:
    /// они не переживают загрузку и всегда откатываются к началу забега.
    /// </summary>
    public enum OfficeEpisodePhase
    {
        Setup = 0,
        Run = 1,
        Awakening = 2,
        Arrival = 3
    }

    /// <summary>Прогресс офисного пролога. Принадлежит эпизоду, Core его не читает.</summary>
    [Serializable]
    public sealed class OfficePrologueProgress
    {
        public OfficeEpisodePhase phase = OfficeEpisodePhase.Setup;

        /// <summary>Короткий Setup уже показан и не повторяется при возврате.</summary>
        public bool setupSeen;

        /// <summary>Число мягких перезапусков забега; первая попытка — 0 перезапусков.</summary>
        public int retries;

        public bool hasLaptop;
        public bool hasMug;
        public int destroyedEquipment;
        public int wreckedChasers;

        /// <summary>Сюжетный финал сна достигнут: пробуждение показано.</summary>
        public bool completed;
    }

    /// <summary>Versioned payload офисной линии для <c>GameSaveService</c>.</summary>
    [Serializable]
    public sealed class OfficeCharacterSaveData
    {
        public int schemaVersion = OfficeCheckpointAdapter.CurrentSchemaVersion;
        public OfficePrologueProgress prologue = new();
    }
}
