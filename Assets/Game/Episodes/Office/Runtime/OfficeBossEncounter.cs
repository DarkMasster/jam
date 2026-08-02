using System;
using Jam.Core.Localization;
using TMPro;
using UnityEngine;

namespace Jam.Episodes.Office
{
    public enum OfficeBossPhase
    {
        Dormant,
        Assembling,
        Assembled,
        Encircling,
        RingFight,
        FinalTelegraph,
        Completed
    }

    /// <summary>
    /// Episode-local постановка финального серверного босса. Стойки используют
    /// заранее заданные позиции: сходятся в единый корпус, принимают несколько
    /// бросков, расходятся в физическое кольцо и проводят неизбежный сюжетный удар.
    /// Группового AI и отдельного input action здесь намеренно нет.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class OfficeBossEncounter : MonoBehaviour, IOfficeImpactTarget, IOfficeRunResettable
    {
        [Header("Сцена")]
        [SerializeField] private Transform player;
        [SerializeField] private OfficePlayerController playerController;
        [SerializeField] private OfficeCarryController carryController;
        [SerializeField] private OfficeRunController runController;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private OfficeMomentum momentum;
        [SerializeField] private Transform[] racks = Array.Empty<Transform>();
        [SerializeField] private Transform[] assemblyAnchors = Array.Empty<Transform>();
        [SerializeField] private Transform[] ringAnchors = Array.Empty<Transform>();
        [SerializeField] private Light[] rackLights = Array.Empty<Light>();
        [SerializeField] private GameObject ringLinks;
        [SerializeField] private GameObject arenaSeal;
        [SerializeField] private Transform lineTelegraph;
        [SerializeField] private Transform finalTelegraph;
        [SerializeField] private TextMeshPro hologram;

        [Header("Первая стадия")]
        [SerializeField, Min(0.1f)] private float assemblyDuration = 1.8f;
        [SerializeField, Min(1)] private int requiredHits = 3;
        [SerializeField, Min(0f)] private float minimumImpactSpeed = 6f;
        [SerializeField, Min(0.1f)] private float attackTelegraphDuration = 0.8f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 2.2f;
        [SerializeField, Min(0.1f)] private float attackHalfWidth = 0.8f;

        [Header("Кольцо")]
        [SerializeField, Min(0.1f)] private float encirclementDuration = 1.9f;
        [SerializeField, Min(0.5f)] private float ringFightDuration = 4f;
        [SerializeField, Min(0.5f)] private float finalTelegraphDuration = 1.6f;
        [SerializeField, Min(0.5f)] private float finalTelegraphDiameter = 3.65f;

        private Vector3[] _sourcePositions = Array.Empty<Vector3>();
        private Quaternion[] _sourceRotations = Array.Empty<Quaternion>();
        private Collider[][] _rackColliders = Array.Empty<Collider[]>();
        private AudioSource _audioSource;
        private AudioClip _humClip;
        private AudioClip _clickClip;
        private float _phaseTime;
        private float _attackTime;
        private float _nextImpactTime;
        private bool _attackCharging;
        private Vector3 _attackOrigin;
        private Vector3 _attackDirection;
        private float _attackLength;
        private Vector3 _encirclementPlayerStart;
        private bool _ringPhraseShown;
        private string _hologramKey;
        private string _hologramFallback;
        private float _hologramUntil;

        public OfficeBossPhase Phase { get; private set; } = OfficeBossPhase.Dormant;

        public int RegisteredHits { get; private set; }

        public int RequiredHits => requiredHits;

        public bool EncounterStarted => Phase != OfficeBossPhase.Dormant;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            CacheRackState();
            CreateProceduralAudio();
            ApplyDormantState();
        }

        private void OnEnable()
        {
            OfficeRunReset.Register(this);
            Loc.LocaleChanged += HandleLocaleChanged;
        }

        private void OnDisable()
        {
            OfficeRunReset.Unregister(this);
            Loc.LocaleChanged -= HandleLocaleChanged;
        }

        private void OnDestroy()
        {
            if (_humClip != null)
            {
                Destroy(_humClip);
            }

            if (_clickClip != null)
            {
                Destroy(_clickClip);
            }
        }

        private void Update()
        {
            UpdateHologram();

            switch (Phase)
            {
                case OfficeBossPhase.Assembling:
                    TickAssembly();
                    break;
                case OfficeBossPhase.Assembled:
                    TickAssembledFight();
                    break;
                case OfficeBossPhase.Encircling:
                    TickEncirclement();
                    break;
                case OfficeBossPhase.RingFight:
                    TickRingFight();
                    break;
                case OfficeBossPhase.FinalTelegraph:
                    TickFinalTelegraph();
                    break;
            }
        }

        public void Configure(
            Transform playerTransform,
            OfficePlayerController movement,
            OfficeCarryController carry,
            OfficeRunController run,
            OfficeEpisodeController episode,
            OfficeMomentum momentumScale,
            Transform[] rackTransforms,
            Transform[] assembledTargets,
            Transform[] ringTargets,
            Light[] warningLights,
            GameObject links,
            GameObject seal,
            Transform attackLine,
            Transform strikeArea,
            TextMeshPro hologramText)
        {
            player = playerTransform;
            playerController = movement;
            carryController = carry;
            runController = run;
            episodeController = episode;
            momentum = momentumScale;
            racks = rackTransforms ?? Array.Empty<Transform>();
            assemblyAnchors = assembledTargets ?? Array.Empty<Transform>();
            ringAnchors = ringTargets ?? Array.Empty<Transform>();
            rackLights = warningLights ?? Array.Empty<Light>();
            ringLinks = links;
            arenaSeal = seal;
            lineTelegraph = attackLine;
            finalTelegraph = strikeArea;
            hologram = hologramText;
        }

        public bool TryStartEncounter()
        {
            if (Phase != OfficeBossPhase.Dormant || racks.Length == 0)
            {
                return false;
            }

            Phase = OfficeBossPhase.Assembling;
            _phaseTime = 0f;
            RegisteredHits = 0;
            _ringPhraseShown = false;
            SetPlayerControlLocked(true);
            SetRackColliders(false);
            SetActive(arenaSeal, true);
            SetActive(ringLinks, false);
            SetActive(lineTelegraph, false);
            SetActive(finalTelegraph, false);
            StartMechanicalHum();
            PlayMechanicalClick(0.8f);
            ShowHologram(
                "boss.hologram.assembled",
                "СОТРУДНИК БОЛЬШЕ НЕ ТРЕБУЕТСЯ",
                assemblyDuration + 1.2f);
            episodeController?.ReportBossAssemblyStarted();
            return true;
        }

        public bool TryTakeImpact(float impactSpeed)
        {
            if (Phase != OfficeBossPhase.Assembled
                || impactSpeed < minimumImpactSpeed
                || Time.time < _nextImpactTime)
            {
                return false;
            }

            _nextImpactTime = Time.time + 0.45f;
            RegisteredHits = Mathf.Min(requiredHits, RegisteredHits + 1);
            momentum?.AddBreak();
            SetRackLightIntensity(4.8f);
            PlayMechanicalClick(1.25f + (RegisteredHits * 0.08f));
            episodeController?.ReportBossHit(RegisteredHits, requiredHits);
            var feedbackPosition = GetRackCenter();
            OfficeFeedback.Instance?.ReportImpact(feedbackPosition, 1f);
            OfficeFeedback.Instance?.ReportDamage(feedbackPosition, null);

            if (RegisteredHits >= requiredHits)
            {
                BeginEncirclement();
            }

            return true;
        }

        public void ResetForRun()
        {
            Phase = OfficeBossPhase.Dormant;
            RegisteredHits = 0;
            _phaseTime = 0f;
            _attackTime = 0f;
            _nextImpactTime = 0f;
            _attackCharging = false;
            _ringPhraseShown = false;
            ApplyDormantState();
        }

        private void TickAssembly()
        {
            _phaseTime += Time.deltaTime;
            var progress = Mathf.Clamp01(_phaseTime / assemblyDuration);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            MoveRacks(_sourcePositions, _sourceRotations, assemblyAnchors, eased);
            PulseRackSequence(progress, 3.8f);

            if (progress < 1f)
            {
                return;
            }

            SnapRacksTo(assemblyAnchors);
            SetRackColliders(true);
            SetPlayerControlLocked(false);
            Phase = OfficeBossPhase.Assembled;
            _phaseTime = 0f;
            _attackTime = 1.1f;
            ShowHologram("boss.hologram.battle", "ИИ ДЕЛАЕТ ЭТО БЫСТРЕЕ", 2.2f);
            PlayMechanicalClick(1f);
            episodeController?.ReportBossAssembled(requiredHits);
        }

        private void TickAssembledFight()
        {
            if (_attackCharging)
            {
                _attackTime -= Time.deltaTime;
                PulseLineTelegraph();
                if (_attackTime <= 0f)
                {
                    ResolveLineAttack();
                }

                return;
            }

            _attackTime -= Time.deltaTime;
            SetRackLightIntensity(Mathf.Lerp(0.35f, 1.2f, (Mathf.Sin(Time.time * 5f) + 1f) * 0.5f));
            if (_attackTime <= 0f)
            {
                BeginLineAttack();
            }
        }

        private void BeginLineAttack()
        {
            if (player == null || lineTelegraph == null)
            {
                _attackTime = attackCooldown;
                return;
            }

            _attackOrigin = GetRackCenter();
            var offset = Flatten(player.position - _attackOrigin);
            if (offset.sqrMagnitude < 0.01f)
            {
                offset = Vector3.back;
            }

            _attackDirection = offset.normalized;
            _attackLength = Mathf.Clamp(offset.magnitude + 2f, 4f, 12f);
            lineTelegraph.position = _attackOrigin + (_attackDirection * (_attackLength * 0.5f)) + (Vector3.up * 0.04f);
            lineTelegraph.rotation = Quaternion.LookRotation(_attackDirection, Vector3.up);
            lineTelegraph.localScale = new Vector3(attackHalfWidth * 2f, 0.025f, _attackLength);
            SetActive(lineTelegraph, true);
            _attackCharging = true;
            _attackTime = attackTelegraphDuration;
            PlayMechanicalClick(1.45f);

            var key = RegisteredHits % 2 == 0
                ? "boss.hologram.mid.one"
                : "boss.hologram.mid.two";
            var fallback = RegisteredHits % 2 == 0
                ? "ТЫ ОБУЧИЛ СВОЮ ЗАМЕНУ"
                : "ТВОЯ РОЛЬ ОПТИМИЗИРОВАНА";
            ShowHologram(key, fallback, attackTelegraphDuration + 0.8f);
        }

        private void ResolveLineAttack()
        {
            _attackCharging = false;
            _attackTime = attackCooldown;
            SetActive(lineTelegraph, false);

            if (player == null || runController == null)
            {
                return;
            }

            var offset = Flatten(player.position - _attackOrigin);
            var along = Vector3.Dot(offset, _attackDirection);
            var lateral = (offset - (_attackDirection * along)).magnitude;
            if (along >= 0f && along <= _attackLength + 0.8f && lateral <= attackHalfWidth)
            {
                runController.TryDamagePlayer(Loc.Get(
                    LocalizationTables.Office,
                    "boss.source",
                    "СЕРВЕРНЫЙ БОСС"));
            }
        }

        private void BeginEncirclement()
        {
            Phase = OfficeBossPhase.Encircling;
            _phaseTime = 0f;
            _attackCharging = false;
            SetActive(lineTelegraph, false);
            SetRackColliders(false);
            SetPlayerControlLocked(true);
            if (player != null)
            {
                _encirclementPlayerStart = player.position;
            }

            ShowHologram(
                "boss.hologram.encirclement",
                "МАСШТАБИРОВАНИЕ ЗАВЕРШЕНО",
                encirclementDuration + 1f);
            PlayMechanicalClick(0.72f);
            episodeController?.ReportBossEncirclementStarted();
        }

        private void TickEncirclement()
        {
            _phaseTime += Time.deltaTime;
            var progress = Mathf.Clamp01(_phaseTime / encirclementDuration);
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            MoveRacks(assemblyAnchors, ringAnchors, eased);
            MovePlayerIntoRing(eased);
            PulseRackSequence(progress, 5f);

            if (progress < 1f)
            {
                return;
            }

            SnapRacksTo(ringAnchors);
            SetRackColliders(true);
            SetActive(ringLinks, true);
            SetPlayerControlLocked(false);
            Phase = OfficeBossPhase.RingFight;
            _phaseTime = 0f;
            ShowHologram(
                "boss.hologram.ring",
                "ЧЕЛОВЕЧЕСКАЯ ОШИБКА ОБНАРУЖЕНА",
                2.2f);
            PlayMechanicalClick(1.05f);
            episodeController?.ReportBossRingClosed();
        }

        private void TickRingFight()
        {
            _phaseTime += Time.deltaTime;
            var progress = Mathf.Clamp01(_phaseTime / ringFightDuration);
            PulseRackSequence(progress, 4.2f);

            if (!_ringPhraseShown && progress >= 0.48f)
            {
                _ringPhraseShown = true;
                ShowHologram("boss.hologram.ring.beat", "ТЫ БЫЛ УЗКИМ МЕСТОМ", 1.8f);
                PlayMechanicalClick(1.32f);
            }

            if (progress >= 1f)
            {
                BeginFinalTelegraph();
            }
        }

        private void BeginFinalTelegraph()
        {
            Phase = OfficeBossPhase.FinalTelegraph;
            _phaseTime = 0f;
            SetActive(finalTelegraph, true);
            if (finalTelegraph != null)
            {
                finalTelegraph.position = GetRingCenter() + (Vector3.up * 0.05f);
                finalTelegraph.localScale = new Vector3(0.2f, 0.025f, 0.2f);
            }

            ShowHologram("boss.hologram.final", "ДОСТУП ОТОЗВАН", finalTelegraphDuration + 0.8f);
            PlayMechanicalClick(0.58f);
            episodeController?.ReportBossFinalTelegraph();
        }

        private void TickFinalTelegraph()
        {
            _phaseTime += Time.deltaTime;
            var progress = Mathf.Clamp01(_phaseTime / finalTelegraphDuration);
            var pulse = Mathf.Lerp(0.2f, finalTelegraphDiameter, Mathf.SmoothStep(0f, 1f, progress));
            if (finalTelegraph != null)
            {
                finalTelegraph.localScale = new Vector3(pulse, 0.025f, pulse);
            }

            SetRackLightIntensity(Mathf.Lerp(1.5f, 7f, progress));
            if (progress < 1f)
            {
                return;
            }

            Phase = OfficeBossPhase.Completed;
            SetPlayerControlLocked(true);
            ShowHologram("boss.hologram.complete", "OFFBOARDING ЗАВЕРШЁН", 12f);
            PlayMechanicalClick(0.42f);

            if (runController != null)
            {
                runController.CompleteFromFinalStrike(Loc.Get(
                    LocalizationTables.Office,
                    "boss.final_source",
                    "СЕРВЕРНОЕ КОЛЬЦО"));
            }
            else
            {
                episodeController?.ReportBossFinalStrike();
            }
        }

        private void ApplyDormantState()
        {
            SetPlayerControlLocked(false);
            SetRackColliders(false);
            SetActive(ringLinks, false);
            SetActive(arenaSeal, false);
            SetActive(lineTelegraph, false);
            SetActive(finalTelegraph, false);
            SetRackLightIntensity(0f);

            if (_sourcePositions.Length == racks.Length)
            {
                for (var i = 0; i < racks.Length; i++)
                {
                    if (racks[i] != null)
                    {
                        racks[i].SetPositionAndRotation(_sourcePositions[i], _sourceRotations[i]);
                    }
                }
            }

            if (hologram != null)
            {
                hologram.gameObject.SetActive(false);
            }

            _hologramKey = string.Empty;
            _hologramFallback = string.Empty;
            _hologramUntil = 0f;
            _audioSource?.Stop();
        }

        private void CacheRackState()
        {
            _sourcePositions = new Vector3[racks.Length];
            _sourceRotations = new Quaternion[racks.Length];
            _rackColliders = new Collider[racks.Length][];

            for (var i = 0; i < racks.Length; i++)
            {
                var rack = racks[i];
                if (rack == null)
                {
                    _rackColliders[i] = Array.Empty<Collider>();
                    continue;
                }

                _sourcePositions[i] = rack.position;
                _sourceRotations[i] = rack.rotation;
                _rackColliders[i] = rack.GetComponentsInChildren<Collider>(true);
            }
        }

        private void SetRackColliders(bool value)
        {
            for (var i = 0; i < _rackColliders.Length; i++)
            {
                var colliders = _rackColliders[i];
                for (var j = 0; j < colliders.Length; j++)
                {
                    if (colliders[j] != null)
                    {
                        colliders[j].enabled = value;
                    }
                }
            }
        }

        private void MoveRacks(
            Vector3[] fromPositions,
            Quaternion[] fromRotations,
            Transform[] targets,
            float progress)
        {
            var count = Mathf.Min(racks.Length, targets.Length);
            for (var i = 0; i < count; i++)
            {
                if (racks[i] == null || targets[i] == null)
                {
                    continue;
                }

                racks[i].SetPositionAndRotation(
                    Vector3.Lerp(fromPositions[i], targets[i].position, progress),
                    Quaternion.Slerp(fromRotations[i], targets[i].rotation, progress));
            }
        }

        private void MoveRacks(Transform[] from, Transform[] targets, float progress)
        {
            var count = Mathf.Min(racks.Length, Mathf.Min(from.Length, targets.Length));
            for (var i = 0; i < count; i++)
            {
                if (racks[i] == null || from[i] == null || targets[i] == null)
                {
                    continue;
                }

                racks[i].SetPositionAndRotation(
                    Vector3.Lerp(from[i].position, targets[i].position, progress),
                    Quaternion.Slerp(from[i].rotation, targets[i].rotation, progress));
            }
        }

        private void SnapRacksTo(Transform[] targets)
        {
            var count = Mathf.Min(racks.Length, targets.Length);
            for (var i = 0; i < count; i++)
            {
                if (racks[i] != null && targets[i] != null)
                {
                    racks[i].SetPositionAndRotation(targets[i].position, targets[i].rotation);
                }
            }
        }

        private void MovePlayerIntoRing(float progress)
        {
            if (player == null)
            {
                return;
            }

            var center = GetRingCenter();
            center.y = _encirclementPlayerStart.y;
            player.position = Vector3.Lerp(_encirclementPlayerStart, center, progress);
        }

        private void PulseRackSequence(float progress, float intensity)
        {
            if (rackLights.Length == 0)
            {
                return;
            }

            var active = Mathf.Min(rackLights.Length - 1, Mathf.FloorToInt(progress * rackLights.Length));
            for (var i = 0; i < rackLights.Length; i++)
            {
                if (rackLights[i] != null)
                {
                    rackLights[i].intensity = i == active ? intensity : 0.4f;
                }
            }
        }

        private void SetRackLightIntensity(float intensity)
        {
            for (var i = 0; i < rackLights.Length; i++)
            {
                if (rackLights[i] != null)
                {
                    rackLights[i].intensity = intensity;
                }
            }
        }

        private void PulseLineTelegraph()
        {
            if (lineTelegraph == null)
            {
                return;
            }

            var pulse = 0.82f + (Mathf.Sin(Time.time * 22f) * 0.18f);
            var scale = lineTelegraph.localScale;
            scale.x = attackHalfWidth * 2f * pulse;
            lineTelegraph.localScale = scale;
        }

        private Vector3 GetRackCenter()
        {
            var center = Vector3.zero;
            var count = 0;
            for (var i = 0; i < racks.Length; i++)
            {
                if (racks[i] == null)
                {
                    continue;
                }

                center += racks[i].position;
                count++;
            }

            if (count == 0)
            {
                return transform.position;
            }

            center /= count;
            center.y = 0f;
            return center;
        }

        private Vector3 GetRingCenter()
        {
            if (ringAnchors.Length == 0)
            {
                return GetRackCenter();
            }

            var center = Vector3.zero;
            var count = 0;
            for (var i = 0; i < ringAnchors.Length; i++)
            {
                if (ringAnchors[i] == null)
                {
                    continue;
                }

                center += ringAnchors[i].position;
                count++;
            }

            if (count > 0)
            {
                center /= count;
            }

            center.y = 0f;
            return center;
        }

        private void SetPlayerControlLocked(bool value)
        {
            playerController?.SetControlLocked(value);
            carryController?.SetControlLocked(value);
        }

        private void ShowHologram(string key, string fallback, float duration)
        {
            _hologramKey = key;
            _hologramFallback = fallback;
            _hologramUntil = Time.time + duration;
            RefreshHologram();
            if (hologram != null)
            {
                hologram.gameObject.SetActive(true);
            }
        }

        private void UpdateHologram()
        {
            if (hologram == null || !hologram.gameObject.activeSelf)
            {
                return;
            }

            if (Time.time >= _hologramUntil && Phase != OfficeBossPhase.Completed)
            {
                hologram.gameObject.SetActive(false);
                return;
            }

            var camera = Camera.main;
            if (camera != null)
            {
                var awayFromCamera = hologram.transform.position - camera.transform.position;
                if (awayFromCamera.sqrMagnitude > 0.01f)
                {
                    hologram.transform.rotation = Quaternion.LookRotation(awayFromCamera, Vector3.up);
                }
            }
        }

        private void HandleLocaleChanged()
        {
            RefreshHologram();
        }

        private void RefreshHologram()
        {
            if (hologram != null && !string.IsNullOrEmpty(_hologramKey))
            {
                hologram.text = Loc.Get(LocalizationTables.Office, _hologramKey, _hologramFallback);
            }
        }

        private void CreateProceduralAudio()
        {
            if (_audioSource == null)
            {
                return;
            }

            const int sampleRate = 22050;
            var humData = new float[sampleRate];
            for (var i = 0; i < humData.Length; i++)
            {
                var time = i / (float)sampleRate;
                humData[i] = (Mathf.Sin(Mathf.PI * 2f * 55f * time) * 0.08f)
                             + (Mathf.Sin(Mathf.PI * 2f * 110f * time) * 0.035f);
            }

            _humClip = AudioClip.Create("Office Boss Hum", humData.Length, 1, sampleRate, false);
            _humClip.SetData(humData, 0);

            var clickData = new float[Mathf.RoundToInt(sampleRate * 0.09f)];
            for (var i = 0; i < clickData.Length; i++)
            {
                var time = i / (float)sampleRate;
                var envelope = Mathf.Exp(-42f * time);
                clickData[i] = Mathf.Sin(Mathf.PI * 2f * 380f * time) * envelope * 0.7f;
            }

            _clickClip = AudioClip.Create("Office Boss Drive Click", clickData.Length, 1, sampleRate, false);
            _clickClip.SetData(clickData, 0);

            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0.65f;
            _audioSource.minDistance = 3f;
            _audioSource.maxDistance = 28f;
            _audioSource.volume = 0.28f;
        }

        private void StartMechanicalHum()
        {
            if (_audioSource == null || _humClip == null)
            {
                return;
            }

            _audioSource.clip = _humClip;
            _audioSource.pitch = 1f;
            _audioSource.Play();
        }

        private void PlayMechanicalClick(float pitch)
        {
            if (_audioSource == null || _clickClip == null)
            {
                return;
            }

            _audioSource.pitch = pitch;
            _audioSource.PlayOneShot(_clickClip, 0.8f);
            _audioSource.pitch = 1f;
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
            {
                target.SetActive(value);
            }
        }

        private static void SetActive(Transform target, bool value)
        {
            if (target != null)
            {
                target.gameObject.SetActive(value);
            }
        }
    }
}
