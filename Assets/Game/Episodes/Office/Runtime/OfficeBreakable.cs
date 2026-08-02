using System.Collections.Generic;
using UnityEngine;

namespace Jam.Episodes.Office
{
    /// <summary>
    /// Подготовленный офисный объект с двумя состояниями: <c>Intact</c> и <c>Broken</c>.
    /// Свободной фрагментации нет — меняется только заранее собранная геометрия.
    /// </summary>
    /// <remarks>
    /// Поддерживаются две схемы владения целым состоянием:
    /// <list type="bullet">
    /// <item>принтер отдаёт отдельный объект <see cref="intactState"/>, у которого
    /// нет ни коллайдеров, ни чужих компонентов, и он просто выключается;</item>
    /// <item>мебель маршрута отдаёт <see cref="intactVisualRoot"/>, и тогда отдельно
    /// гасятся принадлежащие ей рендереры и коллайдеры. Art-pass уже выключил часть
    /// greybox-рендереров, поэтому исходное состояние каждого элемента запоминается
    /// и восстанавливается.</item>
    /// </list>
    /// </remarks>
    public sealed class OfficeBreakable : MonoBehaviour, IOfficeImpactTarget, IOfficeRunResettable
    {
        [SerializeField] private string displayName = "ПРИНТЕР";
        [SerializeField] private GameObject intactState;
        [SerializeField] private GameObject brokenState;
        [SerializeField] private GameObject intactVisualRoot;
        [SerializeField] private List<GameObject> extraIntactVisuals = new();
        [SerializeField] private BoxCollider bodyCollider;
        [SerializeField] private Light impactFlash;
        [SerializeField] private OfficeEpisodeController episodeController;
        [SerializeField] private OfficeMomentum momentum;
        [SerializeField, Min(0f)] private float breakImpactSpeed = 6f;
        [SerializeField, Range(0f, 1f)] private float momentumGain;
        [SerializeField] private bool countsAsObjective = true;
        [SerializeField, Min(0f)] private float flashIntensity = 7f;
        [SerializeField, Min(0.05f)] private float flashDuration = 0.45f;

        private readonly List<Renderer> _intactRenderers = new();
        private readonly List<bool> _intactRendererStates = new();
        private readonly List<Collider> _blockingColliders = new();
        private readonly List<bool> _blockingColliderStates = new();
        private float _flashTimeLeft;

        public bool IsBroken { get; private set; }

        /// <summary>Объект попадает в счётчик целей HUD, а не только в Momentum.</summary>
        public bool CountsAsObjective => countsAsObjective;

        private void Awake()
        {
            CollectIntactRenderers();
            CollectBlockingColliders();
            ApplyState();

            if (impactFlash != null)
            {
                impactFlash.intensity = 0f;
            }
        }

        private void OnEnable()
        {
            OfficeRunReset.Register(this);
        }

        private void OnDisable()
        {
            OfficeRunReset.Unregister(this);
        }

        private void Start()
        {
            if (countsAsObjective)
            {
                episodeController?.RegisterBreakableTarget();
            }
        }

        private void Update()
        {
            if (impactFlash == null || _flashTimeLeft <= 0f)
            {
                return;
            }

            _flashTimeLeft -= Time.deltaTime;
            impactFlash.intensity = _flashTimeLeft <= 0f
                ? 0f
                : flashIntensity * (_flashTimeLeft / flashDuration);
        }

        /// <summary>
        /// Разрушает объект, если удар достаточно сильный. Возвращает <c>true</c>,
        /// только когда состояние действительно сменилось.
        /// </summary>
        public bool TryBreak(float impactSpeed)
        {
            if (IsBroken || impactSpeed < breakImpactSpeed)
            {
                return false;
            }

            IsBroken = true;
            ApplyState();
            _flashTimeLeft = flashDuration;

            if (impactFlash != null)
            {
                impactFlash.intensity = flashIntensity;
            }

            momentum?.AddBreak(momentumGain);

            if (countsAsObjective)
            {
                episodeController?.RegisterBreakableDestroyed(displayName);
            }
            else
            {
                episodeController?.ReportBreakableDestroyed(displayName);
            }

            OfficeFeedback.Instance?.ReportDestroyed(transform.position);
            return true;
        }

        public bool TryTakeImpact(float impactSpeed)
        {
            return TryBreak(impactSpeed);
        }

        public void ResetForRun()
        {
            if (!IsBroken)
            {
                return;
            }

            IsBroken = false;
            _flashTimeLeft = 0f;

            if (impactFlash != null)
            {
                impactFlash.intensity = 0f;
            }

            ApplyState();
        }

        public void Configure(
            string itemName,
            GameObject intact,
            GameObject broken,
            BoxCollider body,
            Light flash,
            OfficeEpisodeController controller,
            float impactSpeed = 6f)
        {
            displayName = itemName;
            intactState = intact;
            brokenState = broken;
            bodyCollider = body;
            impactFlash = flash;
            episodeController = controller;
            breakImpactSpeed = impactSpeed;
        }

        /// <summary>
        /// Настройка мебели маршрута: целое состояние живёт рендерерами, принадлежащие
        /// объекту коллайдеры отключаются после разрушения, а вклад в Momentum и
        /// участие в счётчике задаются на объекте.
        /// </summary>
        public void ConfigureVisualState(
            string itemName,
            GameObject visualRoot,
            GameObject broken,
            float impactSpeed,
            float gain,
            bool isObjective)
        {
            displayName = itemName;
            intactVisualRoot = visualRoot;
            brokenState = broken;
            breakImpactSpeed = impactSpeed;
            momentumGain = gain;
            countsAsObjective = isObjective;
            bodyCollider = null;
        }

        public void SetImpactFlash(Light flash)
        {
            impactFlash = flash;
        }

        /// <summary>
        /// Регистрирует visual, который лежит вне иерархии объекта. Так art-pass
        /// отдаёт модели шкафов и стекла: их нельзя вешать детьми масштабированного
        /// примитива, поэтому они стоят на корне зоны.
        /// </summary>
        public void RegisterExtraVisual(GameObject visual)
        {
            if (visual != null && !extraIntactVisuals.Contains(visual))
            {
                extraIntactVisuals.Add(visual);
            }
        }

        public void SetSceneReferences(OfficeEpisodeController controller, OfficeMomentum momentumScale)
        {
            episodeController = controller;
            momentum = momentumScale;
        }

        private void CollectIntactRenderers()
        {
            _intactRenderers.Clear();
            _intactRendererStates.Clear();

            if (intactVisualRoot != null)
            {
                foreach (var renderer in intactVisualRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (OwnsRenderer(renderer))
                    {
                        AddIntactRenderer(renderer);
                    }
                }
            }

            for (var i = 0; i < extraIntactVisuals.Count; i++)
            {
                var visual = extraIntactVisuals[i];
                if (visual == null)
                {
                    continue;
                }

                foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    AddIntactRenderer(renderer);
                }
            }
        }

        private void AddIntactRenderer(Renderer renderer)
        {
            _intactRenderers.Add(renderer);

            // Art-pass гасит greybox-рендереры ещё в редакторе, поэтому целым
            // состоянием считается то, что было включено на старте сцены.
            _intactRendererStates.Add(renderer.enabled);
        }

        private bool OwnsRenderer(Renderer renderer)
        {
            if (brokenState != null && renderer.transform.IsChildOf(brokenState.transform))
            {
                return false;
            }

            // Монитор — отдельный разрушаемый объект внутри стола: его рендереры
            // принадлежат ему, а не столу, иначе одно попадание гасило бы оба.
            return renderer.GetComponentInParent<OfficeBreakable>() == this;
        }

        private void CollectBlockingColliders()
        {
            _blockingColliders.Clear();
            _blockingColliderStates.Clear();

            if (bodyCollider != null)
            {
                AddBlockingCollider(bodyCollider);
            }

            if (intactVisualRoot == null)
            {
                return;
            }

            foreach (var collider in intactVisualRoot.GetComponentsInChildren<Collider>(true))
            {
                if (collider.GetComponentInParent<OfficeBreakable>() == this)
                {
                    AddBlockingCollider(collider);
                }
            }
        }

        private void AddBlockingCollider(Collider collider)
        {
            if (collider == null || _blockingColliders.Contains(collider))
            {
                return;
            }

            _blockingColliders.Add(collider);
            _blockingColliderStates.Add(collider.enabled);
        }

        private void ApplyState()
        {
            if (intactState != null)
            {
                intactState.SetActive(!IsBroken);
            }

            for (var i = 0; i < _intactRenderers.Count; i++)
            {
                var renderer = _intactRenderers[i];
                if (renderer != null)
                {
                    renderer.enabled = !IsBroken && _intactRendererStates[i];
                }
            }

            if (brokenState != null)
            {
                brokenState.SetActive(IsBroken);
            }

            for (var i = 0; i < _blockingColliders.Count; i++)
            {
                var collider = _blockingColliders[i];
                if (collider != null)
                {
                    collider.enabled = !IsBroken && _blockingColliderStates[i];
                }
            }
        }
    }
}
