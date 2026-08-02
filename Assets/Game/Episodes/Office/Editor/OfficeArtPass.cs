#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Jam.Episodes.Office.Editor
{
    /// <summary>
    /// Проверочный срез art-pass POLYGON Office.
    ///
    /// Модели пака подключаются только как visual children существующих
    /// project-owned объектов. Gameplay-геометрия, коллайдеры, маршрут, триггеры и
    /// vendor-каталог `Assets/PolygonOffice/**` не меняются: greybox остаётся
    /// авторитетным, у него лишь выключаются рендереры.
    ///
    /// Каждая vendor-модель подгоняется под явно указанный greybox-объём, поэтому
    /// footprint под коллайдером и высота рабочей поверхности остаются теми же,
    /// под которые настраивались броски, проходы и top-down читаемость.
    /// </summary>
    internal static class OfficeArtPass
    {
        private const string VendorPath = "Assets/PolygonOffice/Prefabs";
        private const string DeskAsset = VendorPath + "/Props/Furniture/SM_Prop_Desk_03.prefab";
        private const string MonitorAsset = VendorPath + "/Props/Desk Props/SM_Prop_Computer_Monitor_02.prefab";
        private const string ChairAsset = VendorPath + "/Props/Furniture/SM_Prop_Chair_06.prefab";
        private const string CabinetAsset = VendorPath + "/Props/Furniture/SM_Prop_Cabinets_01.prefab";
        private const string LampAsset = VendorPath + "/Props/Desk Props/SM_Prop_DeskLamp_06.prefab";
        private const string GlassAsset = VendorPath + "/Buildings/SM_Bld_Wall_Glass_Large_01.prefab";
        private const string ServerRackAssetA = VendorPath + "/Props/Misc/SM_Prop_Server_Cabinet_01_Full.prefab";
        private const string ServerRackAssetB = VendorPath + "/Props/Misc/SM_Prop_Server_Cabinet_02_Full.prefab";

        private const string ArtRootName = "Synty Visual";
        private const string TintFolder = "Assets/Game/Episodes/Office/Art/Materials";
        private const string TintPrefix = "M_Synty_";

        // Пользователь сидит со стороны -Z, поэтому фронт мебели развёрнут туда же.
        private const float UserSideYaw = 180f;

        // Стеклянная стена переговорной идёт вдоль Z, а модуль пака — вдоль X.
        private const float GlassYaw = 90f;
        private const int GlassSegments = 3;

        // Панель с индикаторами у серверных шкафов пака смотрит по локальному -X;
        // greybox-стойка показывает фронт по -Z, к подходу игрока.
        private const float ServerRackYaw = -90f;

        // Альбедо пака заметно светлее офисной палитры: без приглушения vendor-меш
        // светится белым в почти чёрной сцене и перебивает красные телеграфы.
        private static readonly Color NightmareTint = new Color(0.42f, 0.44f, 0.50f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        /// <summary>
        /// Проверочный срез: стартовый кабинет, одна пара столов open space, одна
        /// секция стекла переговорной и две серверные стойки.
        /// </summary>
        internal static void ApplyVerificationSlice(OfficeArtPassTargets targets)
        {
            ApplyDesk(targets.StartDesk);
            ApplyChair(targets.StartChair);
            ApplyCabinet(targets.StartCabinetLeft, targets.FurnitureRoot);
            ApplyCabinet(targets.StartCabinetRight, targets.FurnitureRoot);
            ApplyDeskLamp(targets.StartLamp, targets.FurnitureRoot);

            ApplyDesk(targets.OpenDeskLeft);
            ApplyDesk(targets.OpenDeskRight);
            ApplyChair(targets.OpenChairLeft);
            ApplyChair(targets.OpenChairRight);

            ApplyMeetingGlass(targets.MeetingGlassLeft, targets.ArchitectureRoot);

            ApplyServerRack(targets.ServerRackLeft, ServerRackAssetA);
            ApplyServerRack(targets.ServerRackRight, ServerRackAssetB);
        }

        private static void ApplyDesk(GameObject desk)
        {
            if (!IsUsable(desk, nameof(ApplyDesk)))
            {
                return;
            }

            // Greybox: столешница `Top` 3.4 x 1.6 с поверхностью на 0.98 и монитор
            // на 0.99..1.71. Оба объёма сохраняются, поэтому клавиатуры на столах
            // не проваливаются и не висят в воздухе.
            AttachVisual(desk.transform, DeskAsset, new Vector3(0f, 0.49f, 0f), new Vector3(3.4f, 0.98f, 1.6f), UserSideYaw);
            AttachVisual(desk.transform, MonitorAsset, new Vector3(0f, 1.35f, 0.15f), new Vector3(1.25f, 0.72f, 0.34f), UserSideYaw);
            HideGreybox(desk);
        }

        private static void ApplyChair(GameObject chair)
        {
            if (!IsUsable(chair, nameof(ApplyChair)))
            {
                return;
            }

            // Footprint сиденья сохраняется полностью; спинка ниже greybox-габарита,
            // потому что её высота не влияет ни на коллайдер, ни на line of sight.
            AttachVisual(chair.transform, ChairAsset, new Vector3(0f, 0.61f, 0f), new Vector3(0.9f, 1.22f, 0.9f), UserSideYaw);
            HideGreybox(chair);
        }

        private static void ApplyCabinet(GameObject cabinet, Transform artParent)
        {
            if (!IsUsable(cabinet, nameof(ApplyCabinet)) || artParent == null)
            {
                return;
            }

            // Шкаф-куб 1.2 x 2.0 x 1.2 закрывается двумя узкими картотеками:
            // одна модель на весь объём выглядела бы неправдоподобно широкой.
            var box = cabinet.transform.localScale;
            var center = cabinet.transform.position;
            var half = new Vector3(box.x * 0.5f, box.y, box.z);
            AttachVisual(artParent, CabinetAsset, center + new Vector3(-box.x * 0.25f, 0f, 0f), half, UserSideYaw);
            AttachVisual(artParent, CabinetAsset, center + new Vector3(box.x * 0.25f, 0f, 0f), half, UserSideYaw);
            HideRenderer(cabinet);
        }

        private static void ApplyDeskLamp(GameObject lamp, Transform artParent)
        {
            if (!IsUsable(lamp, nameof(ApplyDeskLamp)) || artParent == null)
            {
                return;
            }

            // Тёплый Point Light остаётся на месте: заменяется только куб-заглушка.
            AttachVisual(artParent, LampAsset, lamp.transform.position, lamp.transform.localScale, UserSideYaw);
            HideRenderer(lamp);
        }

        private static void ApplyMeetingGlass(GameObject glass, Transform artParent)
        {
            if (!IsUsable(glass, nameof(ApplyMeetingGlass)) || artParent == null)
            {
                return;
            }

            // Непрерывный greybox-коллайдер длиной 13 закрывается тремя модулями
            // пака. Ширина переговорной и её дверные проёмы не меняются.
            var box = glass.transform.localScale;
            var center = glass.transform.position;
            var segment = box.z / GlassSegments;
            for (var i = 0; i < GlassSegments; i++)
            {
                var offset = (i - ((GlassSegments - 1) * 0.5f)) * segment;
                AttachVisual(
                    artParent,
                    GlassAsset,
                    center + new Vector3(0f, 0f, offset),
                    new Vector3(box.x, box.y, segment),
                    GlassYaw,
                    false);
            }

            HideRenderer(glass);
        }

        private static void ApplyServerRack(GameObject rack, string vendorAssetPath)
        {
            if (!IsUsable(rack, nameof(ApplyServerRack)))
            {
                return;
            }

            // Красные `Status LED` остаются greybox: они читаются сверху и держат
            // цветовой акцент серверной так же, как до art-pass.
            AttachVisual(rack.transform, vendorAssetPath, new Vector3(0f, 1.3f, 0f), new Vector3(1.4f, 2.6f, 1.4f), ServerRackYaw);
            HideGreybox(rack, "Status LED");
        }

        /// <summary>
        /// Подключает vendor-модель как visual child и подгоняет её под указанный
        /// greybox-объём в локальных координатах владельца.
        /// </summary>
        private static GameObject AttachVisual(
            Transform owner,
            string vendorAssetPath,
            Vector3 boxCenter,
            Vector3 boxSize,
            float yaw,
            bool castShadows = true)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(vendorAssetPath);
            if (prefab == null)
            {
                Debug.LogError($"POLYGON Office visual is missing: {vendorAssetPath}");
                return null;
            }

            var artRoot = EnsureArtRoot(owner);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, artRoot);
            var visual = instance.transform;
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            visual.localPosition = Vector3.zero;
            visual.localRotation = rotation;
            visual.localScale = Vector3.one;

            // Габариты берутся после поворота: pivot моделей пака не совпадает с
            // центром меша, а стеновые модули вообще начинаются с края.
            if (!TryGetBounds(instance, artRoot, out var bounds) || bounds.size.sqrMagnitude <= 0f)
            {
                Debug.LogError($"POLYGON Office visual has no renderer bounds: {vendorAssetPath}");
                Object.DestroyImmediate(instance);
                return null;
            }

            // Коэффициенты считаются вдоль осей владельца, а `localScale` работает
            // вдоль осей самой модели, поэтому для повёрнутых модулей их нужно
            // перенести обратно через inverse rotation. Все повороты среза кратны
            // 90°, поэтому подгонка остаётся точной.
            var fit = new Vector3(
                Ratio(boxSize.x, bounds.size.x),
                Ratio(boxSize.y, bounds.size.y),
                Ratio(boxSize.z, bounds.size.z));
            var local = Quaternion.Inverse(rotation) * fit;
            visual.localScale = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            visual.localPosition = boxCenter - Vector3.Scale(bounds.center, fit);

            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                // Столкновения остаются за greybox: vendor-меши только presentation.
                collider.enabled = false;
            }

            ApplyOfficePalette(instance);

            if (!castShadows)
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            return instance;
        }

        /// <summary>
        /// Переводит vendor-меш в офисную палитру через project-owned material
        /// variants. Сам каталог `Assets/PolygonOffice/**` остаётся read-only:
        /// вариант наследует материал пака и переопределяет только базовый цвет.
        /// </summary>
        private static void ApplyOfficePalette(GameObject instance)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var source = renderer.sharedMaterials;
                var tinted = new Material[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    tinted[i] = GetOrCreateTintedVariant(source[i]);
                }

                renderer.sharedMaterials = tinted;
            }
        }

        private static Material GetOrCreateTintedVariant(Material source)
        {
            if (source == null || source.name.StartsWith(TintPrefix))
            {
                return source;
            }

            var path = $"{TintFolder}/{TintPrefix}{source.name}.mat";
            var variant = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (variant == null)
            {
                variant = new Material(source) { name = TintPrefix + source.name };
                AssetDatabase.CreateAsset(variant, path);
            }

            variant.parent = source;
            if (source.HasProperty(BaseColorId))
            {
                var baseColor = source.GetColor(BaseColorId);
                variant.SetColor(BaseColorId, new Color(
                    baseColor.r * NightmareTint.r,
                    baseColor.g * NightmareTint.g,
                    baseColor.b * NightmareTint.b,
                    baseColor.a));
            }

            EditorUtility.SetDirty(variant);
            return variant;
        }

        private static Transform EnsureArtRoot(Transform owner)
        {
            var existing = owner.Find(ArtRootName);
            if (existing != null)
            {
                return existing;
            }

            var artRoot = new GameObject(ArtRootName).transform;
            artRoot.SetParent(owner, false);
            artRoot.localPosition = Vector3.zero;
            artRoot.localRotation = Quaternion.identity;
            artRoot.localScale = Vector3.one;
            return artRoot;
        }

        private static bool TryGetBounds(GameObject instance, Transform space, out Bounds bounds)
        {
            bounds = default;
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var toLocal = space.worldToLocalMatrix;
            var initialized = false;
            foreach (var renderer in renderers)
            {
                var world = renderer.bounds;
                for (var corner = 0; corner < 8; corner++)
                {
                    var point = toLocal.MultiplyPoint3x4(new Vector3(
                        (corner & 1) == 0 ? world.min.x : world.max.x,
                        (corner & 2) == 0 ? world.min.y : world.max.y,
                        (corner & 4) == 0 ? world.min.z : world.max.z));

                    if (initialized)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        initialized = true;
                    }
                }
            }

            return initialized;
        }

        /// <summary>
        /// Гасит greybox-рендереры владельца, не трогая коллайдеры, компоненты и
        /// перечисленные по имени акценты.
        /// </summary>
        private static void HideGreybox(GameObject owner, params string[] keepNames)
        {
            foreach (var renderer in owner.GetComponentsInChildren<Renderer>(true))
            {
                if (IsArtVisual(renderer.transform, owner.transform))
                {
                    continue;
                }

                if (keepNames != null && System.Array.IndexOf(keepNames, renderer.gameObject.name) >= 0)
                {
                    continue;
                }

                renderer.enabled = false;
            }
        }

        private static void HideRenderer(GameObject greybox)
        {
            var renderer = greybox.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }

        private static bool IsArtVisual(Transform candidate, Transform owner)
        {
            for (var current = candidate; current != null && current != owner.parent; current = current.parent)
            {
                if (current.name == ArtRootName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUsable(GameObject target, string step)
        {
            if (target != null)
            {
                return true;
            }

            Debug.LogError($"Office art-pass step {step} received no target.");
            return false;
        }

        private static float Ratio(float required, float actual)
        {
            return Mathf.Abs(actual) < 0.0001f ? 1f : required / actual;
        }
    }

    /// <summary>
    /// Объекты сцены, которые входят в проверочный срез art-pass. Builder заполняет
    /// их во время сборки, чтобы art-pass не искал greybox по именам.
    /// </summary>
    internal sealed class OfficeArtPassTargets
    {
        internal Transform ArchitectureRoot;
        internal Transform FurnitureRoot;
        internal GameObject StartDesk;
        internal GameObject StartChair;
        internal GameObject StartCabinetLeft;
        internal GameObject StartCabinetRight;
        internal GameObject StartLamp;
        internal GameObject OpenDeskLeft;
        internal GameObject OpenDeskRight;
        internal GameObject OpenChairLeft;
        internal GameObject OpenChairRight;
        internal GameObject MeetingGlassLeft;
        internal GameObject ServerRackLeft;
        internal GameObject ServerRackRight;
    }
}
#endif
