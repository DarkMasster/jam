using DamageNumbersPro;
using System.Collections.Generic;
using UnityEngine;

namespace Jam.Integrations.DamageNumbersPro
{
    public sealed class DamageNumbersFeedbackAdapter : MonoBehaviour, IGameFeedback
    {
        [SerializeField] private DamageNumberMesh damagePreset;
        [SerializeField] private DamageNumberMesh interactionPreset;
        [SerializeField] private DamageNumberMesh milestonePreset;
        [SerializeField] private Vector3 worldOffset = new(0f, 1.8f, 0f);
        [SerializeField, Min(0.1f)] private float targetLaneSpacing = 1.05f;
        [SerializeField, Min(0.1f)] private float targetBurstWindow = 1.1f;

        private readonly Dictionary<EntityId, TargetBurst> _targetBursts = new();

        private struct TargetBurst
        {
            public float LastSpawnTime;
            public int NextLane;
        }

        public int SpawnedCount { get; private set; }

        private void Awake()
        {
            GameFeedbackService.Register(this);
        }

        private void OnDestroy()
        {
            GameFeedbackService.Unregister(this);
        }

        public void Configure(
            DamageNumberMesh damage,
            DamageNumberMesh interaction,
            DamageNumberMesh milestone)
        {
            damagePreset = damage;
            interactionPreset = interaction;
            milestonePreset = milestone;
        }

        public void ShowDamage(Vector3 position, Transform target = null)
        {
            if (damagePreset == null)
            {
                return;
            }

            SpawnedCount++;
            if (target != null)
            {
                damagePreset.Spawn(position + GetTargetOffset(target), -1f, target);
                return;
            }

            damagePreset.Spawn(position + worldOffset, -1f);
        }

        public void ShowInteraction(Vector3 position, string text, Transform target = null)
        {
            SpawnText(interactionPreset, position, text, target);
        }

        public void ShowMilestone(Vector3 position, string text, Transform target = null)
        {
            SpawnText(milestonePreset, position, text, target);
        }

        private void SpawnText(DamageNumberMesh preset, Vector3 position, string text, Transform target)
        {
            if (preset == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            SpawnedCount++;
            if (target != null)
            {
                preset.Spawn(position + GetTargetOffset(target), text, target);
                return;
            }

            preset.Spawn(position + worldOffset, text);
        }

        private Vector3 GetTargetOffset(Transform target)
        {
            var id = target.GetEntityId();
            var now = Time.unscaledTime;
            _targetBursts.TryGetValue(id, out var burst);
            if (now - burst.LastSpawnTime > targetBurstWindow)
            {
                burst.NextLane = 0;
            }

            var lane = burst.NextLane;
            burst.NextLane = (lane + 1) % 4;
            burst.LastSpawnTime = now;
            _targetBursts[id] = burst;
            return worldOffset + (Vector3.up * targetLaneSpacing * lane);
        }
    }
}
