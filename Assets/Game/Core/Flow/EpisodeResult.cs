using System;
using System.Collections.Generic;
using Jam.Core.Save;

namespace Jam.Core.Flow
{
    /// <summary>
    /// Одна строка итога эпизода для общего экрана прибытия. Core не интерпретирует
    /// значение: эпизод сам решает, что показать игроку.
    /// </summary>
    [Serializable]
    public sealed class EpisodeResultLine
    {
        public string table = string.Empty;
        public string key = string.Empty;
        public string fallback = string.Empty;
        public string value = string.Empty;

        public EpisodeResultLine()
        {
        }

        public EpisodeResultLine(string tableName, string entryKey, string fallbackLabel, string displayValue)
        {
            table = tableName ?? string.Empty;
            key = entryKey ?? string.Empty;
            fallback = fallbackLabel ?? string.Empty;
            value = displayValue ?? string.Empty;
        }
    }

    /// <summary>
    /// Единственный результат, который эпизод возвращает общему flow. Эпизод не
    /// загружает чужие сцены: он заполняет результат и отдаёт его
    /// <see cref="GameFlowService"/>.
    /// </summary>
    [Serializable]
    public sealed class EpisodeResult
    {
        public CharacterId characterId = CharacterId.None;

        /// <summary>Сцена эпизода; используется для checkpoint и «Продолжить».</summary>
        public string sceneName = string.Empty;

        /// <summary>Устойчивый checkpoint эпизода на момент завершения.</summary>
        public string checkpointId = string.Empty;

        /// <summary>Versioned payload эпизода; Core его не читает.</summary>
        public string payloadJson = string.Empty;

        /// <summary>Эпизод дошёл до сюжетного финала, а не был брошен.</summary>
        public bool episodeCompleted;

        /// <summary>Ключ короткого текста прибытия; Core берёт его из таблицы.</summary>
        public string arrivalTable = string.Empty;
        public string arrivalKey = string.Empty;
        public string arrivalFallback = string.Empty;

        /// <summary>Читаемые строки итога: retries, собранные вещи и т. п.</summary>
        public List<EpisodeResultLine> lines = new();

        public EpisodeResult AddLine(string table, string key, string fallback, string value)
        {
            lines ??= new List<EpisodeResultLine>();
            lines.Add(new EpisodeResultLine(table, key, fallback, value));
            return this;
        }

        public bool IsValid =>
            characterId != CharacterId.None && !string.IsNullOrWhiteSpace(sceneName);
    }
}
