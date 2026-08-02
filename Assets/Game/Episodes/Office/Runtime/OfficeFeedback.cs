using Jam.Core.Localization;
using Jam.Integrations.DamageNumbersPro;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Локальная для эпизода служба подачи: на одно событие даёт процедурный звук,
    /// короткий всплеск частиц и тряску камеры. Экземпляр в сцене один и находится
    /// вызывающей стороной через <see cref="Instance"/>, чтобы разрушаемая техника,
    /// кресло и бросок не хранили собственных ссылок на презентацию.
    /// Любая недостающая ссылка означает «эффекта нет», а не исключение.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class OfficeFeedback : MonoBehaviour
    {
        private const int SampleRate = 22050;

        /// <summary>Шаги питча по кругу: повтор одного клипа не звучит одинаково.</summary>
        private static readonly float[] PitchSteps = { -0.07f, 0.06f, -0.03f, 0.11f, 0f, 0.14f };

        [Header("Сцена")]
        [SerializeField] private OfficeCameraShake cameraShake;
        [SerializeField] private OfficeMomentum momentum;

        [Header("Пул частиц")]
        [SerializeField, Min(1)] private int emitterCount = 6;
        [SerializeField, Min(1)] private int impactParticles = 10;
        [SerializeField, Min(1)] private int breakParticles = 24;
        [SerializeField, Min(1)] private int throwParticles = 6;
        [SerializeField, Min(1)] private int playerHitParticles = 16;
        [SerializeField, Min(0.05f)] private float particleLifetime = 0.42f;
        [SerializeField, Min(0.005f)] private float particleSize = 0.07f;
        [SerializeField, Min(0.1f)] private float particleSpeed = 3.6f;

        [Header("Тряска")]
        [SerializeField, Range(0f, 1f)] private float impactShake = 0.34f;
        [SerializeField, Range(0f, 1f)] private float breakShake = 0.6f;
        [SerializeField, Range(0f, 1f)] private float throwShake = 0.14f;
        [SerializeField, Range(0f, 1f)] private float playerHitShake = 0.85f;
        [SerializeField, Range(0f, 1f)] private float momentumShakeBonus = 0.28f;

        [Header("Палитра")]
        // Значения взяты из раздела «Палитра офисного эпизода» без пересчёта:
        // красный обозначает удар, горячий и критический оттенки живут доли секунды.
        [SerializeField] private Color impactColor = new Color32(0xD8, 0x24, 0x1D, 0xFF);
        [SerializeField] private Color hotColor = new Color32(0xFF, 0x5A, 0x3C, 0xFF);
        [SerializeField] private Color criticalColor = new Color32(0xFF, 0x3B, 0x30, 0xFF);
        [SerializeField] private Color warmColor = new Color32(0xFF, 0xF2, 0xD8, 0xFF);

        private AudioSource _audioSource;
        private AudioClip _impactClip;
        private AudioClip _breakClip;
        private AudioClip _throwClip;
        private AudioClip _playerHitClip;
        private Material _particleMaterial;
        private ParticleSystem[] _emitters;
        private int _emitterCursor;
        private int _pitchCursor;

        public static OfficeFeedback Instance { get; private set; }

        // Отключённый domain reload сохраняет статическую ссылку между запусками.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _audioSource = GetComponent<AudioSource>();
            CreateProceduralAudio();
            CreateParticlePool();
        }

        private void Start()
        {
            // Сцена собирается билдером, но компонент должен работать и при ручной
            // установке на пустой объект: недостающие ссылки ищем один раз.
            if (cameraShake == null)
            {
                cameraShake = FindAnyObjectByType<OfficeCameraShake>();
            }

            if (momentum == null)
            {
                momentum = FindAnyObjectByType<OfficeMomentum>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_impactClip != null)
            {
                Destroy(_impactClip);
            }

            if (_breakClip != null)
            {
                Destroy(_breakClip);
            }

            if (_throwClip != null)
            {
                Destroy(_throwClip);
            }

            if (_playerHitClip != null)
            {
                Destroy(_playerHitClip);
            }

            if (_particleMaterial != null)
            {
                Destroy(_particleMaterial);
            }
        }

        /// <summary>Editor-side wiring: тряска камеры и шкала Momentum.</summary>
        public void Configure(OfficeCameraShake shake, OfficeMomentum momentumScale)
        {
            cameraShake = shake;
            momentum = momentumScale;
        }

        /// <summary>
        /// Общий удар без смены состояния цели. <paramref name="strength"/> — 0..1,
        /// обычно нормализованная скорость столкновения.
        /// </summary>
        public void ReportImpact(Vector3 position, float strength)
        {
            var normalized = Mathf.Clamp01(strength);
            PlayClip(_impactClip, 1.05f, 0.55f);
            Burst(
                position,
                Color.Lerp(impactColor, hotColor, normalized),
                Mathf.RoundToInt(Mathf.Lerp(4f, impactParticles, normalized)));
            RequestShake(impactShake * Mathf.Lerp(0.55f, 1f, normalized), 0.16f);
        }

        /// <summary>Техника разрушена: самое заметное событие петли «подбор → бросок».</summary>
        public void ReportBreak(Vector3 position)
        {
            PlayClip(_breakClip, 0.92f, 0.75f);
            Burst(position, hotColor, breakParticles);
            RequestShake(breakShake, 0.28f);
        }

        public void ReportDestroyed(Vector3 position)
        {
            ReportBreak(position);
            GameFeedbackService.ShowInteraction(
                position,
                Loc.Get(LocalizationTables.Office, "feedback.broken", "СЛОМАНО"));
        }

        /// <summary>Бросок из рук: подача короткая, чтобы не перебивать попадание.</summary>
        public void ReportThrow(Vector3 position)
        {
            PlayClip(_throwClip, 1.18f, 0.4f);
            Burst(position, warmColor, throwParticles);
            RequestShake(throwShake, 0.1f);
        }

        /// <summary>Попадание по герою: критический оттенок и самая сильная тряска.</summary>
        public void ReportPlayerHit(Transform player)
        {
            if (player == null)
            {
                return;
            }

            var position = player.position;
            PlayClip(_playerHitClip, 0.86f, 0.8f);
            Burst(position, criticalColor, playerHitParticles);
            RequestShake(playerHitShake, 0.4f);
            GameFeedbackService.ShowDamage(position, player);
        }

        public void ReportDamage(Vector3 position, Transform target)
        {
            GameFeedbackService.ShowDamage(position, target);
        }

        public void ReportPickup(Transform player, string itemName)
        {
            if (player == null)
            {
                return;
            }

            GameFeedbackService.ShowInteraction(
                player.position,
                OfficeEpisodeController.LocalizeRuntimeName(itemName),
                player);
        }

        public void ReportCollectiblePickup(
            Transform player,
            OfficeCollectibleType collectibleType,
            bool allPersonalItemsCollected)
        {
            if (player == null)
            {
                return;
            }

            var item = collectibleType == OfficeCollectibleType.Laptop
                ? Loc.Get(LocalizationTables.Office, "feedback.laptop", "НОУТБУК")
                : Loc.Get(LocalizationTables.Office, "feedback.mug", "КРУЖКА");
            GameFeedbackService.ShowInteraction(player.position, item, player);

            if (allPersonalItemsCollected)
            {
                GameFeedbackService.ShowMilestone(
                    player.position,
                    Loc.Get(LocalizationTables.Office, "feedback.personal_items", "ЛИЧНЫЕ ВЕЩИ СОБРАНЫ"),
                    player);
            }
        }

        private float MomentumValue => momentum != null ? momentum.Value : 0f;

        private void RequestShake(float strength, float duration)
        {
            if (cameraShake == null)
            {
                return;
            }

            // Momentum усиливает подачу: на пике шкалы те же удары читаются жёстче.
            var scaled = strength * (1f + (MomentumValue * momentumShakeBonus));
            cameraShake.Shake(Mathf.Clamp01(scaled), duration);
        }

        private void Burst(Vector3 position, Color color, int count)
        {
            if (_emitters == null || _emitters.Length == 0 || count <= 0)
            {
                return;
            }

            var emitter = _emitters[_emitterCursor];
            _emitterCursor = (_emitterCursor + 1) % _emitters.Length;
            if (emitter == null)
            {
                return;
            }

            emitter.transform.position = position;

            var emitParams = new ParticleSystem.EmitParams
            {
                startColor = color,
                startLifetime = particleLifetime,
                startSize = particleSize,
                applyShapeToPosition = true
            };

            emitter.Emit(emitParams, Mathf.Clamp(count, 1, 64));
        }

        private void PlayClip(AudioClip clip, float basePitch, float volume)
        {
            if (_audioSource == null || clip == null)
            {
                return;
            }

            // Питч не возвращается к 1 сразу: PlayOneShot читает значение источника
            // уже на аудиопотоке, и мгновенный сброс убрал бы всю вариацию.
            _audioSource.pitch = NextPitch(basePitch);
            _audioSource.PlayOneShot(clip, volume);
        }

        private float NextPitch(float basePitch)
        {
            var step = PitchSteps[_pitchCursor];
            _pitchCursor = (_pitchCursor + 1) % PitchSteps.Length;
            return Mathf.Clamp(basePitch + step, 0.35f, 2.6f);
        }

        private void CreateProceduralAudio()
        {
            if (_audioSource == null)
            {
                return;
            }

            var impactData = new float[Mathf.RoundToInt(SampleRate * 0.11f)];
            for (var i = 0; i < impactData.Length; i++)
            {
                var time = i / (float)SampleRate;
                var envelope = Mathf.Exp(-34f * time);
                var body = Mathf.Sin(Mathf.PI * 2f * 210f * time);
                var grit = (Mathf.PerlinNoise(i * 0.41f, 0.5f) - 0.5f) * 2f;
                impactData[i] = ((body * 0.6f) + (grit * 0.4f)) * envelope * 0.8f;
            }

            _impactClip = AudioClip.Create("Office Feedback Impact", impactData.Length, 1, SampleRate, false);
            _impactClip.SetData(impactData, 0);

            var breakData = new float[Mathf.RoundToInt(SampleRate * 0.3f)];
            for (var i = 0; i < breakData.Length; i++)
            {
                var time = i / (float)SampleRate;
                var envelope = Mathf.Exp(-11f * time);
                var body = Mathf.Sin(Mathf.PI * 2f * 118f * time);
                var shell = Mathf.Sin(Mathf.PI * 2f * 305f * time) * 0.35f;
                var debris = (Mathf.PerlinNoise(i * 0.73f, 1.5f) - 0.5f) * 2f;
                breakData[i] = ((body * 0.5f) + shell + (debris * 0.45f)) * envelope * 0.75f;
            }

            _breakClip = AudioClip.Create("Office Feedback Break", breakData.Length, 1, SampleRate, false);
            _breakClip.SetData(breakData, 0);

            var throwData = new float[Mathf.RoundToInt(SampleRate * 0.16f)];
            for (var i = 0; i < throwData.Length; i++)
            {
                var time = i / (float)SampleRate;
                var progress = i / (float)throwData.Length;
                // Короткий свист: шум с подъёмом и мягким входом-выходом по огибающей.
                var envelope = Mathf.Sin(Mathf.PI * progress);
                var air = (Mathf.PerlinNoise(i * (0.22f + (progress * 0.5f)), 2.5f) - 0.5f) * 2f;
                var tone = Mathf.Sin(Mathf.PI * 2f * Mathf.Lerp(420f, 720f, progress) * time) * 0.25f;
                throwData[i] = (air + tone) * envelope * 0.5f;
            }

            _throwClip = AudioClip.Create("Office Feedback Throw", throwData.Length, 1, SampleRate, false);
            _throwClip.SetData(throwData, 0);

            var playerHitData = new float[Mathf.RoundToInt(SampleRate * 0.34f)];
            for (var i = 0; i < playerHitData.Length; i++)
            {
                var time = i / (float)SampleRate;
                var envelope = Mathf.Exp(-9f * time);
                // Две близкие частоты дают биение — сигнал «это ударили тебя».
                var low = Mathf.Sin(Mathf.PI * 2f * 92f * time);
                var beat = Mathf.Sin(Mathf.PI * 2f * 138f * time) * 0.6f;
                playerHitData[i] = (low + beat) * envelope * 0.62f;
            }

            _playerHitClip = AudioClip.Create("Office Feedback Player Hit", playerHitData.Length, 1, SampleRate, false);
            _playerHitClip.SetData(playerHitData, 0);

            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            // Служба живёт на одном объекте, а события происходят по всему этажу:
            // 2D-звук честнее, чем панорама из точки, где стоит сама служба.
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 0.85f;
        }

        private void CreateParticlePool()
        {
            _particleMaterial = CreateParticleMaterial();
            _emitters = new ParticleSystem[Mathf.Max(1, emitterCount)];

            for (var i = 0; i < _emitters.Length; i++)
            {
                var host = new GameObject($"Office Feedback Burst {i + 1:00}");
                host.transform.SetParent(transform, false);

                var system = host.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // Модули ParticleSystem — структуры-обёртки над нативной системой.
                // Свойства main/emission/shape доступны только на чтение, поэтому
                // модуль берётся в локальную переменную: правка его полей уходит
                // в систему сразу, а обратное присваивание не компилируется.
                var main = system.main;
                main.loop = true;
                main.playOnAwake = false;
                main.duration = 1f;
                main.startLifetime = particleLifetime;
                main.startSpeed = particleSpeed;
                main.startSize = particleSize;
                main.startColor = impactColor;
                main.gravityModifier = 0.65f;
                main.maxParticles = 128;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                var emission = system.emission;
                emission.enabled = false;
                emission.rateOverTime = 0f;

                var shape = system.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.16f;

                var systemRenderer = host.GetComponent<ParticleSystemRenderer>();
                if (systemRenderer != null)
                {
                    systemRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                    systemRenderer.alignment = ParticleSystemRenderSpace.View;
                    systemRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    systemRenderer.receiveShadows = false;

                    if (_particleMaterial != null)
                    {
                        systemRenderer.sharedMaterial = _particleMaterial;
                    }
                }

                // Система остаётся зацикленной и «играющей» без собственной эмиссии:
                // только так вручную вызванный Emit продолжает симулироваться.
                system.Play();
                _emitters[i] = system;
            }
        }

        /// <summary>
        /// Материал частиц собирается в коде: эпизод не тянет за собой ассет.
        /// URP-шейдер основной, <c>Sprites/Default</c> — запасной для проектов без URP.
        /// </summary>
        private static Material CreateParticleMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader) { name = "Office Feedback Particles" };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }
    }
}
