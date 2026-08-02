using UnityEngine;

namespace Jam.Integrations.DamageNumbersPro
{
    public interface IGameFeedback
    {
        void ShowDamage(Vector3 position, Transform target = null);
        void ShowInteraction(Vector3 position, string text, Transform target = null);
        void ShowMilestone(Vector3 position, string text, Transform target = null);
    }

    /// <summary>Project-owned presentation boundary for Damage Numbers Pro.</summary>
    public static class GameFeedbackService
    {
        private static IGameFeedback _provider;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _provider = null;
        }

        public static void Register(IGameFeedback provider)
        {
            _provider = provider;
        }

        public static void Unregister(IGameFeedback provider)
        {
            if (object.ReferenceEquals(_provider, provider))
            {
                _provider = null;
            }
        }

        public static void ShowDamage(Vector3 position, Transform target = null)
        {
            _provider?.ShowDamage(position, target);
        }

        public static void ShowInteraction(Vector3 position, string text, Transform target = null)
        {
            _provider?.ShowInteraction(position, text, target);
        }

        public static void ShowMilestone(Vector3 position, string text, Transform target = null)
        {
            _provider?.ShowMilestone(position, text, target);
        }
    }
}
