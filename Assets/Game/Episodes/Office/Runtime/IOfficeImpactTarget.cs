namespace Jam.Episodes.Office
{
    /// <summary>
    /// Объект офиса, который повреждается брошенным предметом. Через него петля
    /// «подбор → бросок» работает одинаково для техники окружения и для противника.
    /// </summary>
    public interface IOfficeImpactTarget
    {
        /// <summary>
        /// Возвращает <c>true</c>, только когда удар действительно сменил состояние.
        /// </summary>
        bool TryTakeImpact(float impactSpeed);
    }
}
