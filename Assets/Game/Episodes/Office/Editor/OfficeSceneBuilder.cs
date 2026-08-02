#if UNITY_EDITOR
using DamageNumbersPro;
using Jam.Core.Cutscenes;
using Jam.Core.Localization;
using Jam.Episodes.Office;
using Jam.Integrations.DamageNumbersPro;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam.Episodes.Office.Editor
{
    public static class OfficeSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Scenes/Prologue_Office.unity";
        private const string ArtPath = "Assets/Game/Episodes/Office/Art";
        private const string MaterialPath = ArtPath + "/Materials";
        private const string PrefabPath = "Assets/Game/Episodes/Office/Prefabs";
        private const string CutscenePath = "Assets/Game/Episodes/Office/Cutscenes";
        private const string SetupCutscenePath = CutscenePath + "/OfficeSetupStoryboard.asset";
        private const string AwakeningCutscenePath = CutscenePath + "/OfficeAwakeningStoryboard.asset";
        private const string FeedbackPath = "Assets/Game/Integrations/DamageNumbersPro/Presets";
        private const string FeedbackSourcePath = "Assets/DamageNumbersPro/Demo/Prefabs/3D/Clear.prefab";
        private const string FeedbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string DamagePresetPath = FeedbackPath + "/OfficeDamage.prefab";
        private const string InteractionPresetPath = FeedbackPath + "/OfficeInteraction.prefab";
        private const string MilestonePresetPath = FeedbackPath + "/OfficeMilestone.prefab";
        private const string SetupCutsceneId = "office.prologue.setup";
        private const string AwakeningCutsceneId = "office.prologue.awakening";

        [MenuItem("Jam/Office/Rebuild Prologue Office")]
        public static void Build()
        {
            EnsureFolder(ArtPath);
            EnsureFolder(MaterialPath);
            EnsureFolder(PrefabPath);
            EnsureFolder(CutscenePath);
            EnsureDamageNumberPresets();
            EnsureBossLocalization();

            var palette = CreatePalette();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sceneRoot = new GameObject("Prologue_Office").transform;
            var environmentRoot = CreateGroup("Environment", sceneRoot);
            var architectureRoot = CreateGroup("Architecture", environmentRoot);
            var furnitureRoot = CreateGroup("Furniture", environmentRoot);
            var backgroundRoot = CreateGroup("BackgroundScale", environmentRoot);
            var gameplayRoot = CreateGroup("Gameplay", sceneRoot);
            var lightingRoot = CreateGroup("Lighting", sceneRoot);

            var artTargets = new OfficeArtPassTargets
            {
                ArchitectureRoot = architectureRoot,
                FurnitureRoot = furnitureRoot,
                BackgroundRoot = backgroundRoot
            };

            var keyLight = ConfigureEnvironment(palette, lightingRoot);
            BuildArchitecture(palette, architectureRoot, artTargets);
            BuildFurniture(palette, furnitureRoot, artTargets);
            BuildBackgroundScale(palette, backgroundRoot, artTargets);
            OfficeArtPass.ApplyVerificationSlice(artTargets);
            OfficeArtPass.ApplyOpenSpaceSlice(artTargets);

            var episodeObject = new GameObject("OfficeEpisodeController", typeof(OfficeEpisodeController), typeof(OfficeMomentum), typeof(OfficeRunController));
            episodeObject.transform.SetParent(gameplayRoot, false);
            var episodeController = episodeObject.GetComponent<OfficeEpisodeController>();
            var momentum = episodeObject.GetComponent<OfficeMomentum>();
            var runController = episodeObject.GetComponent<OfficeRunController>();

            var player = CreatePlayer(palette, gameplayRoot);
            var camera = CreateCamera(player.transform, lightingRoot);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var playerController = player.GetComponent<OfficePlayerController>();
            playerController.Configure(inputActions, camera, "Office", "Move", momentum);

            var handAnchor = player.transform.Find("Hand Anchor");
            var carryController = player.GetComponent<OfficeCarryController>();
            carryController.Configure(inputActions, "Office", "Primary", handAnchor, episodeController, momentum);

            runController.Configure(
                player.transform,
                player.GetComponent<CharacterController>(),
                playerController,
                carryController,
                episodeController);

            var laptop = CreateLaptop(palette, gameplayRoot, new Vector3(0f, 0.78f, -6f), episodeController);
            var mug = CreateMug(palette, gameplayRoot, new Vector3(0f, 0.72f, 28.8f), episodeController);

            var exitGate = CreateExitGate(palette, gameplayRoot, episodeController);
            CreateZoneTriggers(gameplayRoot, episodeController);

            var propsRoot = CreateGroup("Props", gameplayRoot);
            BuildCarryables(palette, propsRoot);
            BuildBreakables(palette, propsRoot, episodeController, momentum);

            var enemyRoot = CreateGroup("Enemies", gameplayRoot);
            BuildChasers(palette, enemyRoot, player.transform, runController, momentum, episodeController);
            var chasers = enemyRoot.GetComponentsInChildren<OfficeChaser>(true);

            var bossEncounter = CreateBossEncounter(
                palette,
                gameplayRoot,
                player.transform,
                playerController,
                carryController,
                runController,
                episodeController,
                momentum,
                artTargets);

            // Стойки босса создаются позже архитектуры и мебели, поэтому оставшаяся
            // статичная мебель переводится на модели пака уже после них.
            OfficeArtPass.ApplyRemainingFurnitureSlice(artTargets);

            var hudPrefab = CreateOrUpdatePrefab("OfficeHud", BuildHudTemplate);
            var hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, sceneRoot);
            var hudBinding = hudInstance.GetComponent<OfficeHudBinding>();
            episodeController.Configure(
                hudBinding.Zone,
                hudBinding.Objective,
                hudBinding.Carry,
                hudBinding.Status,
                hudBinding.Integrity,
                hudBinding.Momentum,
                hudBinding.MomentumFill,
                hudBinding.DownPanel,
                hudBinding.DownText,
                exitGate,
                momentum,
                bossEncounter);

            var coach = episodeObject.AddComponent<OfficeCoach>();
            coach.Configure(
                hudBinding.Coach,
                player.transform,
                carryController,
                runController,
                episodeController,
                chasers);

            CreateReflectionBeat(palette, gameplayRoot, player.transform, episodeController, coach);
            CreateItemGuarantee(palette, gameplayRoot, laptop, mug, episodeController, coach);

            CreateStoryFrame(sceneRoot, episodeObject, runController, episodeController, playerController, carryController);
            CreateFeedbackLayer(gameplayRoot, lightingRoot, camera, keyLight, momentum);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;

            Debug.Log($"Built office vertical slice at {ScenePath}");
        }

        /// <summary>
        /// Сюжетная рамка эпизода: короткий Setup сна, пробуждение после финального
        /// удара и передача результата в общий flow.
        /// </summary>
        private static void CreateStoryFrame(
            Transform sceneRoot,
            GameObject episodeObject,
            OfficeRunController runController,
            OfficeEpisodeController episodeController,
            OfficePlayerController playerController,
            OfficeCarryController carryController)
        {
            var setupSequence = EnsureStoryboard(SetupCutscenePath, "OfficeSetupStoryboard", "setup", SetupFrames);
            var awakeningSequence = EnsureStoryboard(AwakeningCutscenePath, "OfficeAwakeningStoryboard", "awakening", AwakeningFrames);

            var storyRoot = CreateGroup("Story", sceneRoot);
            ConfigurePresentation(storyRoot, "Setup Cutscene", SetupCutsceneId, setupSequence);
            ConfigurePresentation(storyRoot, "Awakening Cutscene", AwakeningCutsceneId, awakeningSequence);

            var storyDirector = episodeObject.AddComponent<OfficeStoryDirector>();
            storyDirector.Configure(
                runController,
                episodeController,
                playerController,
                carryController,
                SetupCutsceneId,
                AwakeningCutsceneId);
        }

        /// <summary>
        /// Слой обратной связи `M6`: дрожь камеры, процедурные SFX с частицами и
        /// свет, который следует Momentum, не пряча пол и красные телеграфы.
        /// </summary>
        private static void CreateFeedbackLayer(
            Transform gameplayRoot,
            Transform lightingRoot,
            Camera camera,
            Light keyLight,
            OfficeMomentum momentum)
        {
            var shake = camera.gameObject.AddComponent<OfficeCameraShake>();
            shake.Configure(camera);

            var feedbackObject = new GameObject("Office Feedback", typeof(AudioSource), typeof(OfficeFeedback));
            feedbackObject.transform.SetParent(gameplayRoot, false);
            feedbackObject.GetComponent<OfficeFeedback>().Configure(shake, momentum);
            feedbackObject.AddComponent<DamageNumbersFeedbackAdapter>().Configure(
                LoadDamageNumberPreset(DamagePresetPath),
                LoadDamageNumberPreset(InteractionPresetPath),
                LoadDamageNumberPreset(MilestonePresetPath));

            // Красные акценты маршрута: только они реагируют на Momentum.
            var accentLights = new System.Collections.Generic.List<Light>();
            foreach (var light in lightingRoot.GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Point && light.name.Contains("Red"))
                {
                    accentLights.Add(light);
                }
            }

            var ambienceObject = new GameObject("Momentum Ambience", typeof(OfficeMomentumAmbience));
            ambienceObject.transform.SetParent(lightingRoot, false);
            ambienceObject.GetComponent<OfficeMomentumAmbience>()
                .Configure(momentum, accentLights.ToArray(), keyLight);
        }

        private static StoryboardCutsceneAsset EnsureStoryboard(
            string assetPath,
            string assetName,
            string keyPrefix,
            (string speaker, string text)[] frames)
        {
            var asset = AssetDatabase.LoadAssetAtPath<StoryboardCutsceneAsset>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StoryboardCutsceneAsset>();
                asset.name = assetName;
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var serialized = new SerializedObject(asset);
            serialized.FindProperty("skippable").boolValue = true;
            var serializedFrames = serialized.FindProperty("frames");
            serializedFrames.arraySize = frames.Length;
            for (var index = 0; index < frames.Length; index++)
            {
                var frame = serializedFrames.GetArrayElementAtIndex(index);
                frame.FindPropertyRelative("localizationTable").stringValue = LocalizationTables.Office;
                frame.FindPropertyRelative("speakerKey").stringValue = $"{keyPrefix}.{index + 1:00}.speaker";
                frame.FindPropertyRelative("textKey").stringValue = $"{keyPrefix}.{index + 1:00}.text";
                frame.FindPropertyRelative("speaker").stringValue = frames[index].speaker;
                frame.FindPropertyRelative("text").stringValue = frames[index].text;
                frame.FindPropertyRelative("autoAdvanceSeconds").floatValue = 0f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void EnsureDamageNumberPresets()
        {
            EnsureFolder(FeedbackPath);
            EnsureDamageNumberPreset(
                DamagePresetPath,
                "OfficeDamage",
                Hex("FF5A3C"),
                false,
                1.1f,
                12,
                "OfficeDamage");
            EnsureDamageNumberPreset(
                InteractionPresetPath,
                "OfficeInteraction",
                Hex("EDE9DF"),
                true,
                1.35f,
                8,
                "OfficeInteraction");
            EnsureDamageNumberPreset(
                MilestonePresetPath,
                "OfficeMilestone",
                Hex("FFF2D8"),
                true,
                1.7f,
                4,
                "OfficeMilestone");
        }

        private static void EnsureDamageNumberPreset(
            string assetPath,
            string assetName,
            Color color,
            bool textOnly,
            float lifetime,
            int maxActiveInstances,
            string spamGroup)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null
                && !AssetDatabase.CopyAsset(FeedbackSourcePath, assetPath))
            {
                Debug.LogError($"Could not create office DNP preset at {assetPath}.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                root.name = assetName;
                root.transform.localPosition = Vector3.zero;
                var preset = root.GetComponent<DamageNumberMesh>();
                if (preset == null)
                {
                    Debug.LogError($"Office DNP preset has no {nameof(DamageNumberMesh)}: {assetPath}");
                    return;
                }

                preset.lifetime = lifetime;
                preset.enable3DGame = true;
                preset.faceCameraView = true;
                preset.lookAtCamera = true;
                preset.renderThroughWalls = false;
                preset.consistentScreenSize = true;
                preset.enableOrthographicScaling = true;
                preset.defaultOrthographicSize = 10.5f;
                preset.enableNumber = !textOnly;
                preset.enableLeftText = textOnly;
                preset.numberSettings.customColor = true;
                preset.numberSettings.color = color;
                preset.numberSettings.size = -1.2f;
                preset.leftTextSettings.customColor = true;
                preset.leftTextSettings.color = color;
                preset.leftTextSettings.size = textOnly ? -2.1f : 0f;
                preset.enableFollowing = false;
                preset.enableCombination = !textOnly;
                preset.enableDestruction = false;
                preset.enableCollision = false;
                preset.enablePush = false;
                preset.spamGroup = spamGroup;
                preset.enablePooling = true;
                preset.poolSize = Mathf.Max(8, maxActiveInstances);
                preset.disableOnSceneLoad = true;
                preset.limitActiveInstances = true;
                preset.maxActiveInstances = maxActiveInstances;

                var feedbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FeedbackFontPath);
                foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (feedbackFont != null)
                    {
                        label.font = feedbackFont;
                    }

                    label.textWrappingMode = TextWrappingModes.NoWrap;
                }

                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static DamageNumberMesh LoadDamageNumberPreset(string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            return prefab != null ? prefab.GetComponent<DamageNumberMesh>() : null;
        }

        private static void ConfigurePresentation(
            Transform parent,
            string name,
            string cutsceneId,
            StoryboardCutsceneAsset sequence)
        {
            var presentationObject = new GameObject(name, typeof(UiStoryboardPresentation));
            presentationObject.transform.SetParent(parent, false);

            var presentation = presentationObject.GetComponent<UiStoryboardPresentation>();
            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("cutsceneId").stringValue = cutsceneId;
            serialized.FindProperty("sequence").objectReferenceValue = sequence;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presentation);
        }

        private static Palette CreatePalette()
        {
            return new Palette
            {
                shadow = CreateMaterial("M_Shadow", Hex("08080A"), 0f, 0.08f),
                floor = CreateMaterial("M_Floor", Hex("17171A"), 0f, 0.18f),
                path = CreateMaterial("M_Path", Hex("212126"), 0f, 0.22f),
                wall = CreateMaterial("M_Wall", Hex("141416"), 0f, 0.12f),
                panel = CreateMaterial("M_Panel", Hex("35353B"), 0.45f, 0.22f),
                metal = CreateMaterial("M_LightMetal", Hex("45454E"), 0.65f, 0.35f),
                glass = CreateMaterial("M_Glass", WithAlpha(Hex("2C3A3D"), 0.32f), 0.05f, 0.8f, true),
                paper = CreateMaterial("M_Paper", Hex("CFCABC"), 0f, 0.1f),
                redDim = CreateMaterial("M_RedDim", Hex("6E1512"), 0.1f, 0.28f),
                red = CreateMaterial("M_Red", Hex("D8241D"), 0.15f, 0.32f, false, Hex("D8241D") * 2.2f),
                text = CreateMaterial("M_Text", Hex("EDE9DF"), 0f, 0.15f),
                warm = CreateMaterial("M_WarmLight", Hex("FFF2D8"), 0f, 0.25f, false, Hex("FFF2D8") * 1.4f),
                player = CreateMaterial("M_Player", Hex("0E0E10"), 0.35f, 0.28f),
                playerRim = CreateMaterial("M_PlayerRim", Hex("CFE0FF"), 0.05f, 0.4f, false, Hex("CFE0FF") * 1.4f)
            };
        }

        private static Light ConfigureEnvironment(Palette palette, Transform lightingRoot)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("5C6A84");
            RenderSettings.ambientEquatorColor = Hex("212126");
            RenderSettings.ambientGroundColor = Hex("08080A");
            RenderSettings.ambientIntensity = 0.35f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("08080A");
            RenderSettings.fogStartDistance = 38f;
            RenderSettings.fogEndDistance = 96f;

            var key = new GameObject("Cold Directional Light", typeof(Light));
            key.transform.SetParent(lightingRoot, false);
            key.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            var keyLight = key.GetComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = Hex("9FB0C8");
            keyLight.intensity = 0.5f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.65f;

            CreatePointLight("Warm Start Light", new Vector3(2f, 3f, -29f), Hex("FFF2D8"), 3f, 8f, lightingRoot);
            CreatePointLight("Server Red Light L", new Vector3(-7f, 2.2f, 14.5f), Hex("D8241D"), 1.2f, 7f, lightingRoot);
            CreatePointLight("Server Red Light R", new Vector3(7f, 2.2f, 14.5f), Hex("D8241D"), 1.2f, 7f, lightingRoot);
            CreatePointLight("Exit Red Light", new Vector3(0f, 2.5f, 39f), Hex("D8241D"), 2.5f, 9f, lightingRoot);

            // Ключевой свет возвращается наружу: Momentum усиливает красные акценты,
            // но не имеет права опускать его ниже базовой читаемости пола.
            return keyLight;
        }

        private static void BuildArchitecture(Palette p, Transform root, OfficeArtPassTargets art)
        {
            // Подложка уходит далеко за офис, чтобы горизонт не обрывался краем
            // этажа: линейный туман гасит её в цвет фона задолго до края, и пол
            // читается бесконечным. Коллайдера у подложки нет, поэтому граница
            // игровой зоны по-прежнему задаётся `Playable Floor` и стенами, а
            // упавший предмет возвращается по высоте, а не по этой геометрии.
            CreateCube("Void Floor", new Vector3(0f, -0.28f, 4f), new Vector3(400f, 0.2f, 400f), p.shadow, root, false);
            CreateCube("Playable Floor", new Vector3(0f, -0.1f, 4f), new Vector3(24f, 0.2f, 76f), p.floor, root);
            CreateCube("Navigation Strip", new Vector3(0f, 0.015f, 4f), new Vector3(4f, 0.025f, 74f), p.path, root, false);

            CreateCube("Start Wall Left", new Vector3(-6.15f, 1.5f, -29f), new Vector3(0.3f, 3f, 10f), p.wall, root);
            CreateCube("Start Wall Right", new Vector3(6.15f, 1.5f, -29f), new Vector3(0.3f, 3f, 10f), p.wall, root);
            CreateCube("Start Rear Wall", new Vector3(0f, 0.6f, -34.15f), new Vector3(12.6f, 1.2f, 0.3f), p.wall, root);
            CreateSplitWall("Start Threshold", -24f, 12f, 3.6f, 2.8f, p.glass, root);

            CreateCube("Hall Wall Left", new Vector3(-12.15f, 1.4f, 9f), new Vector3(0.3f, 2.8f, 66f), p.wall, root);
            CreateCube("Hall Wall Right", new Vector3(12.15f, 1.4f, 9f), new Vector3(0.3f, 2.8f, 66f), p.wall, root);

            art.MeetingGlassLeft = CreateCube("Meeting Glass Left", new Vector3(-8.05f, 1.25f, 0.5f), new Vector3(0.18f, 2.5f, 13f), p.glass, root);
            CreateCube("Meeting Glass Right", new Vector3(8.05f, 1.25f, 0.5f), new Vector3(0.18f, 2.5f, 13f), p.glass, root);
            CreateSplitWall("Meeting Entry", -6f, 16f, 3.8f, 2.5f, p.glass, root);
            CreateSplitWall("Meeting Exit", 7f, 16f, 3.8f, 2.5f, p.glass, root);

            CreateSplitWall("Reception Threshold", 22.3f, 24f, 6f, 2.7f, p.glass, root);

            CreateCube("Exit Wall Left", new Vector3(-7.25f, 1.5f, 41.8f), new Vector3(9.5f, 3f, 0.35f), p.wall, root);
            CreateCube("Exit Wall Right", new Vector3(7.25f, 1.5f, 41.8f), new Vector3(9.5f, 3f, 0.35f), p.wall, root);
            CreateCube("Exit Door", new Vector3(0f, 1.45f, 41.65f), new Vector3(5f, 2.9f, 0.3f), p.panel, root);
            CreateCube("Exit Pillar Left", new Vector3(-2.7f, 1.7f, 41.4f), new Vector3(0.35f, 3.4f, 0.45f), p.redDim, root);
            CreateCube("Exit Pillar Right", new Vector3(2.7f, 1.7f, 41.4f), new Vector3(0.35f, 3.4f, 0.45f), p.redDim, root);
            CreateCube("Exit Lintel", new Vector3(0f, 3.3f, 41.4f), new Vector3(5.75f, 0.4f, 0.45f), p.redDim, root);

            CreateWorldLabel("Start Label", "OFFBOARDING", new Vector3(0f, 2.25f, -33.96f), 0.09f, Hex("EDE9DF"), root);
            CreateWorldLabel("Meeting Label", "REFLECTION", new Vector3(0f, 2.2f, 6.85f), 0.075f, Hex("9FB0C8"), root);
            CreateWorldLabel("Exit Label", "EXIT", new Vector3(0f, 3.25f, 41.15f), 0.12f, Hex("EDE9DF"), root);
        }

        private static void BuildFurniture(Palette p, Transform root, OfficeArtPassTargets art)
        {
            art.StartDesk = CreateDesk("Start Desk", new Vector3(1.6f, 0f, -29f), p, root, true);
            art.StartChair = CreateChair("Start Chair", new Vector3(1.6f, 0f, -31f), 180f, p, root);
            art.StartCabinetLeft = CreateCube("Start Cabinet L", new Vector3(-4.8f, 1f, -31.6f), new Vector3(1.2f, 2f, 1.2f), p.panel, root);
            art.StartCabinetRight = CreateCube("Start Cabinet R", new Vector3(-4.8f, 1f, -27.8f), new Vector3(1.2f, 2f, 1.2f), p.panel, root);
            art.StartLamp = CreateCube("Warm Desk Lamp", new Vector3(2.6f, 1.25f, -29f), new Vector3(0.25f, 0.55f, 0.25f), p.warm, root, false);

            // Под на `z = -15` пришёл из проверочного среза `A1`, два остальных
            // переводит слайс `A2`: набор моделей и подгонка у них одинаковые.
            const float artPassPodZ = -15f;
            var podZ = new[] { -20.5f, artPassPodZ, -9.5f };
            foreach (var z in podZ)
            {
                var deskLeft = CreateDesk($"Open Desk L {z}", new Vector3(-7.4f, 0f, z), p, root, true);
                var deskRight = CreateDesk($"Open Desk R {z}", new Vector3(7.4f, 0f, z), p, root, true);
                var chairLeft = CreateChair($"Open Chair L {z}", new Vector3(-7.4f, 0f, z - 1.5f), 180f, p, root);
                var chairRight = CreateChair($"Open Chair R {z}", new Vector3(7.4f, 0f, z - 1.5f), 180f, p, root);

                if (!Mathf.Approximately(z, artPassPodZ))
                {
                    art.OpenSpaceDesks.Add(deskLeft);
                    art.OpenSpaceDesks.Add(deskRight);
                    art.OpenSpaceChairs.Add(chairLeft);
                    art.OpenSpaceChairs.Add(chairRight);
                    continue;
                }

                art.OpenDeskLeft = deskLeft;
                art.OpenDeskRight = deskRight;
                art.OpenChairLeft = chairLeft;
                art.OpenChairRight = chairRight;
            }

            art.MeetingTable = CreateCube("Meeting Table", new Vector3(0f, 0.7f, 0.5f), new Vector3(5.6f, 0.18f, 1.6f), p.panel, root);
            art.MeetingTableBase = CreateCube("Meeting Table Base", new Vector3(0f, 0.35f, 0.5f), new Vector3(0.5f, 0.7f, 1.1f), p.metal, root);
            art.MeetingChairs.Add(CreateChair("Meeting Chair N1", new Vector3(-1.8f, 0f, 2f), 0f, p, root));
            art.MeetingChairs.Add(CreateChair("Meeting Chair N2", new Vector3(1.8f, 0f, 2f), 0f, p, root));
            art.MeetingChairs.Add(CreateChair("Meeting Chair S1", new Vector3(-1.8f, 0f, -1f), 180f, p, root));
            art.MeetingChairs.Add(CreateChair("Meeting Chair S2", new Vector3(1.8f, 0f, -1f), 180f, p, root));
            CreateCube("Reflection Panel", new Vector3(-7.84f, 1.35f, 0.5f), new Vector3(0.08f, 1.6f, 4.6f), p.playerRim, root, false);

            // Пара стоек у самого прохода: они ближе всего к маршруту и лучше всего
            // показывают масштаб vendor-модели сверху.
            const float artPassRackZ = 9.5f;
            var rackX = new[] { -8.4f, -6f, -3.6f, 3.6f, 6f, 8.4f };
            foreach (var z in new[] { 9.5f, 19.5f })
            {
                foreach (var x in rackX)
                {
                    var rack = CreateServerRack($"Server Rack {x} {z}", new Vector3(x, 0f, z), p, root);
                    if (Mathf.Approximately(z, artPassRackZ) && Mathf.Approximately(x, -3.6f))
                    {
                        art.ServerRackLeft = rack;
                    }
                    else if (Mathf.Approximately(z, artPassRackZ) && Mathf.Approximately(x, 3.6f))
                    {
                        art.ServerRackRight = rack;
                    }
                    else
                    {
                        art.ServerRacks.Add(rack);
                    }
                }
            }

            art.ReceptionDesks.Add(CreateReceptionDesk("Reception Desk L", new Vector3(-4f, 0f, 26.5f), p, root));
            art.ReceptionDesks.Add(CreateReceptionDesk("Reception Desk R", new Vector3(4f, 0f, 26.5f), p, root));

            foreach (var x in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
            {
                CreateTurnstile($"Turnstile {x}", new Vector3(x, 0f, 32.5f), p, root);
            }

            // Напольного кольца арены больше нет: красный круг радиуса 8 читался
            // как отдельный объект на полу и спорил с телеграфами боя. Границу
            // арены держат сами стойки, `Boss Arena Seal` и общий телеграф кольца,
            // поэтому маркер был чистым декором и ни на что не ссылался.
        }

        private static void BuildBackgroundScale(Palette p, Transform root, OfficeArtPassTargets art)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 6; i++)
                {
                    var z = -25f + (i * 12f);
                    art.DistantDesks.Add(
                        CreateDesk($"Distant Desk {side} {i}", new Vector3(side * 15.5f, 0f, z), p, root, false));
                    art.DistantColumns.Add(
                        CreateCube($"Distant Column {side} {i}", new Vector3(side * 19f, 1.8f, z + 4f), new Vector3(0.7f, 3.6f, 0.7f), p.wall, root, false));
                }
            }
        }

        private static GameObject CreatePlayer(Palette p, Transform parent)
        {
            var player = new GameObject("Player", typeof(CharacterController), typeof(OfficePlayerController), typeof(OfficeCarryController));
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(0f, 0.04f, -30.5f);

            var controller = player.GetComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.48f;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.06f;

            var body = CreatePrimitive("Body", PrimitiveType.Capsule, new Vector3(0f, 0.92f, 0f), new Vector3(0.78f, 0.88f, 0.78f), p.player, player.transform, false);
            var rim = CreatePrimitive("Rim Light", PrimitiveType.Sphere, new Vector3(0f, 1.85f, -0.1f), new Vector3(0.34f, 0.2f, 0.34f), p.playerRim, player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            rim.transform.localPosition = new Vector3(0f, 1.82f, -0.08f);

            var hand = new GameObject("Hand Anchor").transform;
            hand.SetParent(player.transform, false);
            hand.localPosition = new Vector3(0f, 1.05f, 0.72f);

            return player;
        }

        private static Camera CreateCamera(Transform player, Transform lightingRoot)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(OfficeCameraFollow));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(lightingRoot, false);

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("08080A");
            camera.allowHDR = true;

            var follow = cameraObject.GetComponent<OfficeCameraFollow>();
            follow.Configure(player, new Vector3(0f, 18f, -14f), new Vector3(0f, 0f, 3f));

            var rimLight = cameraObject.AddComponent<Light>();
            rimLight.type = LightType.Spot;
            rimLight.color = Hex("CFE0FF");
            rimLight.intensity = 2.5f;
            rimLight.range = 36f;
            rimLight.spotAngle = 55f;
            rimLight.shadows = LightShadows.None;

            return camera;
        }

        private static GameObject CreateLaptop(
            Palette p,
            Transform parent,
            Vector3 position,
            OfficeEpisodeController controller)
        {
            var root = CreateCollectibleRoot("Laptop Pickup", position, parent);
            CreateCube("Laptop Base", Vector3.zero, new Vector3(1.25f, 0.12f, 0.82f), p.panel, root.transform, false).transform.localPosition = Vector3.zero;
            var screen = CreateCube("Laptop Screen", Vector3.zero, new Vector3(1.25f, 0.72f, 0.08f), p.playerRim, root.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.42f, 0.36f);
            screen.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            var marker = CreateCylinder("Laptop Marker", new Vector3(position.x, 0.04f, position.z), new Vector3(1.5f, 0.025f, 1.5f), p.redDim, parent, false)
                .GetComponent<Renderer>();
            root.GetComponent<OfficeCollectible>().Configure(controller, OfficeCollectibleType.Laptop, marker);
            return root;
        }

        private static GameObject CreateMug(
            Palette p,
            Transform parent,
            Vector3 position,
            OfficeEpisodeController controller)
        {
            var root = CreateCollectibleRoot("Mug Pickup", position, parent);
            var body = CreatePrimitive("Mug Body", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.5f, 0.42f, 0.5f), p.paper, root.transform, false);
            body.transform.localPosition = Vector3.zero;
            var handle = CreateCube("Mug Handle", Vector3.zero, new Vector3(0.16f, 0.42f, 0.42f), p.paper, root.transform, false);
            handle.transform.localPosition = new Vector3(0.42f, 0f, 0f);
            var marker = CreateCylinder("Mug Marker", new Vector3(position.x, 0.04f, position.z), new Vector3(1.5f, 0.025f, 1.5f), p.redDim, parent, false)
                .GetComponent<Renderer>();
            root.GetComponent<OfficeCollectible>().Configure(controller, OfficeCollectibleType.Mug, marker);
            return root;
        }

        private static GameObject CreateCollectibleRoot(string name, Vector3 position, Transform parent)
        {
            var root = new GameObject(name, typeof(SphereCollider), typeof(OfficeCollectible));
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            var trigger = root.GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.3f;
            return root;
        }

        private static OfficeExitGate CreateExitGate(Palette p, Transform parent, OfficeEpisodeController controller)
        {
            var triggerObject = new GameObject("Exit Interaction Zone", typeof(BoxCollider), typeof(OfficeExitGate));
            triggerObject.transform.SetParent(parent, false);
            // Порог расположен перед ареной: стойки должны собраться впереди игрока,
            // а не за его спиной у самой двери.
            triggerObject.transform.position = new Vector3(0f, 1f, 35.9f);
            var trigger = triggerObject.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(5.5f, 2f, 2.5f);

            var indicator = CreateCube("Exit Indicator", new Vector3(0f, 3.3f, 41.1f), new Vector3(4.4f, 0.28f, 0.16f), p.red, parent, false)
                .GetComponent<Renderer>();
            var gate = triggerObject.GetComponent<OfficeExitGate>();
            gate.Configure(controller, indicator);
            return gate;
        }

        private static void CreateZoneTriggers(Transform parent, OfficeEpisodeController controller)
        {
            CreateZoneTrigger("Open Space Zone", new Vector3(0f, 1f, -22.8f), new Vector3(22f, 2f, 2f), "OPEN SPACE", parent, controller);
            CreateZoneTrigger("Meeting Zone", new Vector3(0f, 1f, -5.2f), new Vector3(14f, 2f, 2f), "СТЕКЛЯННАЯ ПЕРЕГОВОРНАЯ", parent, controller);
            CreateZoneTrigger("Server Zone", new Vector3(0f, 1f, 7.8f), new Vector3(22f, 2f, 2f), "СЕРВЕРНАЯ", parent, controller);
            CreateZoneTrigger("Reception Zone", new Vector3(0f, 1f, 23f), new Vector3(22f, 2f, 2f), "РЕЦЕПЦИЯ • EXIT", parent, controller);
        }

        private static void CreateZoneTrigger(string name, Vector3 position, Vector3 size, string label, Transform parent, OfficeEpisodeController controller)
        {
            var zone = new GameObject(name, typeof(BoxCollider), typeof(OfficeZoneTrigger));
            zone.transform.SetParent(parent, false);
            zone.transform.position = position;
            var collider = zone.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            zone.GetComponent<OfficeZoneTrigger>().Configure(controller, label);
        }

        private static void CreateReflectionBeat(
            Palette p,
            Transform parent,
            Transform player,
            OfficeEpisodeController controller,
            OfficeCoach coach)
        {
            var triggerObject = new GameObject("Meeting Reflection Beat", typeof(BoxCollider), typeof(OfficeReflectionBeat));
            triggerObject.transform.SetParent(parent, false);
            triggerObject.transform.position = new Vector3(0f, 1f, -3.8f);

            var trigger = triggerObject.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(14f, 2f, 1.5f);

            var echo = new GameObject("Delayed Reflection").transform;
            echo.SetParent(parent, false);
            echo.position = new Vector3(8.7f, 0.04f, -3.8f);
            CreatePrimitive("Echo Body", PrimitiveType.Capsule, Vector3.zero, new Vector3(0.62f, 0.78f, 0.62f), p.playerRim, echo, false)
                .transform.localPosition = new Vector3(0f, 0.92f, 0f);
            CreatePrimitive("Echo Rim", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.3f, 0.18f, 0.3f), p.glass, echo, false)
                .transform.localPosition = new Vector3(0f, 1.8f, -0.08f);

            triggerObject.GetComponent<OfficeReflectionBeat>()
                .Configure(echo, player, controller, coach);
        }

        private static void CreateItemGuarantee(
            Palette p,
            Transform parent,
            GameObject laptop,
            GameObject mug,
            OfficeEpisodeController controller,
            OfficeCoach coach)
        {
            var triggerObject = new GameObject("Personal Item Guarantee", typeof(BoxCollider), typeof(OfficeItemGuarantee));
            triggerObject.transform.SetParent(parent, false);
            triggerObject.transform.position = new Vector3(0f, 1f, 31f);

            var trigger = triggerObject.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(23f, 2f, 1.4f);

            var laptopAnchor = new GameObject("Laptop Fallback").transform;
            laptopAnchor.SetParent(parent, false);
            laptopAnchor.position = new Vector3(-0.72f, 0.78f, 33.8f);

            var mugAnchor = new GameObject("Mug Fallback").transform;
            mugAnchor.SetParent(parent, false);
            mugAnchor.position = new Vector3(0.72f, 0.72f, 33.8f);

            var accessHold = CreateGroup("Personal Item Access Hold", parent).gameObject;
            CreateCube("Access Hold Barrier", new Vector3(0f, 1f, 35.4f), new Vector3(23.5f, 2f, 0.24f), p.redDim, accessHold.transform);
            CreateWorldLabel(
                "Access Hold Label",
                "ЗАБЕРИ ЛИЧНЫЕ ВЕЩИ",
                new Vector3(0f, 2.25f, 35.2f),
                0.075f,
                Hex("EDE9DF"),
                accessHold.transform);
            accessHold.SetActive(false);

            triggerObject.GetComponent<OfficeItemGuarantee>().Configure(
                laptop.GetComponent<OfficeCollectible>(),
                mug.GetComponent<OfficeCollectible>(),
                laptopAnchor,
                mugAnchor,
                accessHold,
                controller,
                coach);
        }

        private static OfficeBossEncounter CreateBossEncounter(
            Palette p,
            Transform parent,
            Transform player,
            OfficePlayerController movement,
            OfficeCarryController carry,
            OfficeRunController runController,
            OfficeEpisodeController episodeController,
            OfficeMomentum momentum,
            OfficeArtPassTargets art)
        {
            const int rackCount = 12;
            var bossObject = new GameObject("Server Boss Encounter", typeof(AudioSource), typeof(OfficeBossEncounter));
            bossObject.transform.SetParent(parent, false);
            var bossRoot = bossObject.transform;

            var rackPrefab = GetOrCreatePrefab("ServerRack", () => BuildServerRackTemplate(p));
            var rackRoot = CreateGroup("Boss Racks", bossRoot);
            var assemblyRoot = CreateGroup("Assembly Anchors", bossRoot);
            var ringRoot = CreateGroup("Ring Anchors", bossRoot);
            var racks = new Transform[rackCount];
            var assemblyAnchors = new Transform[rackCount];
            var ringAnchors = new Transform[rackCount];
            var warningLights = new Light[rackCount];
            var ringCenter = new Vector3(0f, 0f, 37f);

            for (var i = 0; i < rackCount; i++)
            {
                var side = i < rackCount / 2 ? -1f : 1f;
                var row = i % (rackCount / 2);
                var source = new Vector3(side * 10.25f, 0f, 28.6f + (row * 2.25f));
                var rack = InstantiatePrefab(rackPrefab, $"Boss Rack {i + 1:00}", source, rackRoot);
                racks[i] = rack.transform;
                art.BossRacks.Add(rack);

                var warningObject = new GameObject($"Rack Warning {i + 1:00}", typeof(Light));
                warningObject.transform.SetParent(rack.transform, false);
                warningObject.transform.localPosition = new Vector3(0f, 2.35f, -0.9f);
                var warning = warningObject.GetComponent<Light>();
                warning.type = LightType.Point;
                warning.color = Hex("D8241D");
                warning.range = 5.5f;
                warning.intensity = 0f;
                warning.shadows = LightShadows.None;
                warningLights[i] = warning;

                var assemblyAnchor = new GameObject($"Assembly {i + 1:00}").transform;
                assemblyAnchor.SetParent(assemblyRoot, false);
                assemblyAnchor.position = new Vector3(
                    ((i % 4) - 1.5f) * 1.48f,
                    0f,
                    37.1f + ((i / 4) * 1.42f));
                assemblyAnchor.rotation = Quaternion.identity;
                assemblyAnchors[i] = assemblyAnchor;

                var angle = (Mathf.PI * 2f * i) / rackCount;
                var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 3.65f;
                var ringAnchor = new GameObject($"Ring {i + 1:00}").transform;
                ringAnchor.SetParent(ringRoot, false);
                ringAnchor.position = ringCenter + offset;
                // Передняя панель prefab смотрит по -Z, поэтому forward направлен наружу.
                ringAnchor.rotation = Quaternion.LookRotation(offset.normalized, Vector3.up);
                ringAnchors[i] = ringAnchor;
            }

            var linkRoot = CreateGroup("Closed Ring Links", bossRoot).gameObject;
            for (var i = 0; i < rackCount; i++)
            {
                var next = (i + 1) % rackCount;
                var from = ringAnchors[i].position;
                var to = ringAnchors[next].position;
                var delta = to - from;
                var link = CreateCube(
                    $"Ring Link {i + 1:00}",
                    (from + to) * 0.5f + (Vector3.up * 0.72f),
                    new Vector3(0.18f, 1.44f, delta.magnitude),
                    p.redDim,
                    linkRoot.transform);
                link.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            }
            linkRoot.SetActive(false);

            var seal = CreateGroup("Boss Arena Seal", bossRoot).gameObject;
            CreateCube(
                "Arena Seal Barrier",
                new Vector3(0f, 0.82f, 34.55f),
                new Vector3(23.5f, 1.64f, 0.2f),
                p.redDim,
                seal.transform);
            CreateWorldLabel(
                "Arena Seal Label",
                "ДОСТУП ОТОЗВАН",
                new Vector3(0f, 2.05f, 34.4f),
                0.062f,
                Hex("EDE9DF"),
                seal.transform);
            seal.SetActive(false);

            var lineTelegraph = CreateCube(
                "Boss Line Telegraph",
                Vector3.zero,
                new Vector3(1f, 0.025f, 1f),
                p.red,
                bossRoot,
                false).transform;
            lineTelegraph.gameObject.SetActive(false);

            var finalTelegraph = CreateCylinder(
                "Synchronized Strike Telegraph",
                ringCenter + (Vector3.up * 0.05f),
                new Vector3(0.2f, 0.025f, 0.2f),
                p.red,
                bossRoot,
                false).transform;
            finalTelegraph.gameObject.SetActive(false);

            var hologram = CreateWorldLabel(
                "Boss Hologram",
                "СОТРУДНИК БОЛЬШЕ НЕ ТРЕБУЕТСЯ",
                new Vector3(0f, 4.25f, 38.4f),
                0.052f,
                Hex("EDE9DF"),
                bossRoot);
            hologram.fontStyle = FontStyles.Bold;
            hologram.gameObject.SetActive(false);

            var boss = bossObject.GetComponent<OfficeBossEncounter>();
            boss.Configure(
                player,
                movement,
                carry,
                runController,
                episodeController,
                momentum,
                racks,
                assemblyAnchors,
                ringAnchors,
                warningLights,
                linkRoot,
                seal,
                lineTelegraph,
                finalTelegraph,
                hologram);
            return boss;
        }

        private static GameObject BuildHudTemplate()
        {
            var canvasObject = new GameObject("Office HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(OfficeHudBinding));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var topPanel = CreateUiPanel("Objective Panel", canvasObject.transform, new Color(0.03f, 0.03f, 0.04f, 0.94f));
            SetRect(topPanel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(720f, 250f), new Vector2(0f, 1f));
            var accent = CreateUiPanel("Accent", topPanel.transform, Hex("D8241D"));
            SetRect(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f), new Vector2(0f, 0.5f));

            var zone = CreateUiText("Zone", topPanel.transform, 22, FontStyles.Bold, Hex("D8241D"), TextAlignmentOptions.MidlineLeft);
            SetRect(zone.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -18f), new Vector2(-50f, 34f), new Vector2(0f, 1f));
            var objective = CreateUiText("Objective", topPanel.transform, 20, FontStyles.Bold, Hex("EDE9DF"), TextAlignmentOptions.MidlineLeft);
            SetRect(objective.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -64f), new Vector2(-50f, 48f), new Vector2(0f, 1f));
            var carry = CreateUiText("Carry", topPanel.transform, 18, FontStyles.Normal, Hex("9FB0C8"), TextAlignmentOptions.MidlineLeft);
            SetRect(carry.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -118f), new Vector2(-50f, 44f), new Vector2(0f, 1f));
            var integrity = CreateUiText("Integrity", topPanel.transform, 19, FontStyles.Bold, Hex("CFCABC"), TextAlignmentOptions.MidlineLeft);
            SetRect(integrity.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -168f), new Vector2(-50f, 44f), new Vector2(0f, 1f));
            integrity.text = "РАБОТОСПОСОБНОСТЬ ■■■   ПОПЫТКА 1";

            var statusPanel = CreateUiPanel("Status Panel", canvasObject.transform, new Color(0.03f, 0.03f, 0.04f, 0.94f));
            SetRect(statusPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1180f, 58f), new Vector2(0.5f, 0f));
            var status = CreateUiText("Status", statusPanel.transform, 18, FontStyles.Bold, Hex("CFCABC"), TextAlignmentOptions.Center);
            SetRect(status.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, -10f), new Vector2(0.5f, 0.5f));
            status.text = "WASD / СТРЕЛКИ • ДВИГАЙСЯ К EXIT";

            var coachPanel = CreateUiPanel("Coach Panel", canvasObject.transform, new Color(0.03f, 0.03f, 0.04f, 0.94f));
            SetRect(coachPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1180f, 62f), new Vector2(0.5f, 0f));
            var coach = CreateUiText("Coach", coachPanel.transform, 21, FontStyles.Bold, Hex("EDE9DF"), TextAlignmentOptions.Center);
            SetRect(coach.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, -10f), new Vector2(0.5f, 0.5f));
            coach.text = "WASD / СТРЕЛКИ — ИДИ ВПЕРЁД";

            var momentumPanel = CreateUiPanel("Momentum Panel", canvasObject.transform, new Color(0.03f, 0.03f, 0.04f, 0.94f));
            SetRect(momentumPanel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 102f), new Vector2(1180f, 56f), new Vector2(0.5f, 0f));
            var momentumLabel = CreateUiText("Momentum", momentumPanel.transform, 18, FontStyles.Bold, Hex("FF5A3C"), TextAlignmentOptions.MidlineLeft);
            SetRect(momentumLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(20f, 0f), new Vector2(320f, -14f), new Vector2(0f, 0.5f));
            momentumLabel.text = "ТЕМП 0%   ПРОСТОЙ";

            var momentumTrack = CreateUiPanel("Momentum Track", momentumPanel.transform, new Color(0.08f, 0.08f, 0.1f, 1f));
            SetRect(momentumTrack.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(360f, 0f), new Vector2(-380f, 18f), new Vector2(0f, 0.5f));
            var momentumFill = CreateUiPanel("Momentum Fill", momentumTrack.transform, Hex("6E1512"));
            // Ширину задаёт якорь, поэтому полоса работает без sprite и Image.Type.Filled.
            SetRect(momentumFill.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));

            var downPanel = CreateUiPanel("Down Panel", canvasObject.transform, new Color(0.02f, 0.02f, 0.03f, 0.86f));
            SetRect(downPanel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var downText = CreateUiText("Down Text", downPanel.transform, 34, FontStyles.Bold, Hex("D8241D"), TextAlignmentOptions.Center);
            SetRect(downText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 220f), new Vector2(0.5f, 0.5f));
            downText.text = "ПРОИЗВОДИТЕЛЬНОСТЬ НЕУДОВЛЕТВОРИТЕЛЬНА";
            downPanel.gameObject.SetActive(false);

            canvasObject.GetComponent<OfficeHudBinding>().Configure(
                zone,
                objective,
                carry,
                status,
                integrity,
                momentumLabel,
                momentumFill,
                downPanel.gameObject,
                downText,
                coach);
            return canvasObject;
        }

        private static void BuildCarryables(Palette p, Transform root)
        {
            var prefab = CreateOrUpdatePrefab("Keyboard", () => BuildKeyboardTemplate(p));

            // Клавиатуры лежат на столах маршрута и на полу боевых зон,
            // чтобы петля «подбор → бросок» не запиралась после закрытия арены.
            InstantiatePrefab(prefab, "Keyboard Start", new Vector3(1.6f, 1.08f, -29.2f), root);
            InstantiatePrefab(prefab, "Keyboard Open L", new Vector3(-7.4f, 1.08f, -15f), root);
            InstantiatePrefab(prefab, "Keyboard Open R", new Vector3(7.4f, 1.08f, -9.7f), root);
            InstantiatePrefab(prefab, "Keyboard Meeting", new Vector3(1.4f, 0.9f, 0.5f), root);
            InstantiatePrefab(prefab, "Keyboard Server", new Vector3(-2.6f, 0.14f, 11.5f), root);
            InstantiatePrefab(prefab, "Keyboard Boss L", new Vector3(-4.4f, 0.14f, 35.2f), root);
            InstantiatePrefab(prefab, "Keyboard Boss C", new Vector3(0f, 0.14f, 34.8f), root);
            InstantiatePrefab(prefab, "Keyboard Boss R", new Vector3(4.4f, 0.14f, 35.2f), root);
        }

        private static GameObject BuildKeyboardTemplate(Palette p)
        {
            var root = new GameObject("Keyboard", typeof(BoxCollider), typeof(Rigidbody), typeof(OfficeCarryable));
            var t = root.transform;

            // Светлый корпус нужен, чтобы предмет читался на тёмной столешнице.
            CreateCube("Body", Vector3.zero, new Vector3(0.86f, 0.07f, 0.34f), p.metal, t, false)
                .transform.localPosition = Vector3.zero;
            CreateCube("Keys", Vector3.zero, new Vector3(0.76f, 0.03f, 0.26f), p.paper, t, false)
                .transform.localPosition = new Vector3(0f, 0.045f, 0f);

            var highlight = CreateCylinder("Pickup Highlight", Vector3.zero, new Vector3(1.1f, 0.008f, 1.1f), p.red, t, false);
            highlight.transform.localPosition = new Vector3(0f, -0.055f, 0f);
            // Обводка включается только на выбранной цели, поэтому выключена по умолчанию.
            var highlightRenderer = highlight.GetComponent<Renderer>();
            highlightRenderer.enabled = false;

            var collider = root.GetComponent<BoxCollider>();
            collider.size = new Vector3(0.9f, 0.16f, 0.38f);

            var body = root.GetComponent<Rigidbody>();
            body.mass = 1.1f;
            body.linearDamping = 0.4f;
            body.angularDamping = 1.4f;

            root.GetComponent<OfficeCarryable>().Configure("КЛАВИАТУРА", highlightRenderer, collider);
            return root;
        }

        private static void BuildBreakables(
            Palette p,
            Transform root,
            OfficeEpisodeController controller,
            OfficeMomentum momentum)
        {
            var prefab = CreateOrUpdatePrefab("Printer", () => BuildPrinterTemplate(p));

            CreatePrinter(prefab, "Printer Open L", new Vector3(-3.3f, 0f, -19f), root, controller, momentum);
            CreatePrinter(prefab, "Printer Open R", new Vector3(3.3f, 0f, -13f), root, controller, momentum);
            CreatePrinter(prefab, "Printer Server", new Vector3(0f, 0f, 14.5f), root, controller, momentum);
            CreatePrinter(prefab, "Printer Reception", new Vector3(-3.4f, 0f, 24.6f), root, controller, momentum);
        }

        private static void CreatePrinter(
            GameObject prefab,
            string name,
            Vector3 position,
            Transform parent,
            OfficeEpisodeController controller,
            OfficeMomentum momentum)
        {
            var instance = InstantiatePrefab(prefab, name, position, parent);
            instance.GetComponent<OfficeBreakable>().SetSceneReferences(controller, momentum);
        }

        private static void BuildChasers(
            Palette p,
            Transform root,
            Transform player,
            OfficeRunController runController,
            OfficeMomentum momentum,
            OfficeEpisodeController controller)
        {
            var prefab = CreateOrUpdatePrefab("HostileChair", () => BuildChaserTemplate(p));

            // Один тип противника на весь срез. Кресла стоят в открытых частях маршрута,
            // где у героя есть место для уклонения и лежит предмет для ответного броска.
            CreateChaser(prefab, "Hostile Chair Open N", new Vector3(-2.6f, 0f, -22.5f), 20f, root, player, runController, momentum, controller);
            CreateChaser(prefab, "Hostile Chair Open S", new Vector3(3.1f, 0f, -16.5f), -150f, root, player, runController, momentum, controller);
            CreateChaser(prefab, "Hostile Chair Server", new Vector3(-1.8f, 0f, 12.5f), 90f, root, player, runController, momentum, controller);
            CreateChaser(prefab, "Hostile Chair Reception", new Vector3(2.4f, 0f, 27.5f), 180f, root, player, runController, momentum, controller);
        }

        private static void CreateChaser(
            GameObject prefab,
            string name,
            Vector3 position,
            float yRotation,
            Transform parent,
            Transform player,
            OfficeRunController runController,
            OfficeMomentum momentum,
            OfficeEpisodeController controller)
        {
            var instance = InstantiatePrefab(prefab, name, position, parent, Quaternion.Euler(0f, yRotation, 0f));
            instance.GetComponent<OfficeChaser>().SetSceneReferences(player, runController, momentum, controller);
        }

        private static GameObject BuildChaserTemplate(Palette p)
        {
            var root = new GameObject("Hostile Chair", typeof(BoxCollider), typeof(Rigidbody), typeof(OfficeChaser));
            var t = root.transform;

            // Силуэт остаётся офисным креслом, но крупнее и с красной боевой линией,
            // чтобы противник читался с одного взгляда среди обычной мебели.
            var intact = CreateGroup("Intact", t);
            CreateCube("Seat", Vector3.zero, new Vector3(1.05f, 0.22f, 1.05f), p.panel, intact, false)
                .transform.localPosition = new Vector3(0f, 0.62f, 0f);
            CreateCube("Back", Vector3.zero, new Vector3(1.05f, 1.25f, 0.18f), p.panel, intact, false)
                .transform.localPosition = new Vector3(0f, 1.25f, -0.44f);
            CreateCube("Back Glow", Vector3.zero, new Vector3(0.85f, 0.12f, 0.06f), p.red, intact, false)
                .transform.localPosition = new Vector3(0f, 1.62f, -0.53f);
            // Камера смотрит сверху, поэтому красный акцент дублируется на сиденье:
            // иначе противник не отличается от обычного кресла в кадре.
            CreateCube("Seat Glow", Vector3.zero, new Vector3(0.9f, 0.05f, 0.2f), p.red, intact, false)
                .transform.localPosition = new Vector3(0f, 0.74f, 0f);
            CreateCylinder("Post", Vector3.zero, new Vector3(0.16f, 0.3f, 0.16f), p.metal, intact, false)
                .transform.localPosition = new Vector3(0f, 0.3f, 0f);
            CreateCylinder("Base", Vector3.zero, new Vector3(1.15f, 0.06f, 1.15f), p.metal, intact, false)
                .transform.localPosition = new Vector3(0f, 0.08f, 0f);
            foreach (var angle in new[] { 0f, 72f, 144f, 216f, 288f })
            {
                var wheel = CreateCylinder("Wheel", Vector3.zero, new Vector3(0.2f, 0.06f, 0.2f), p.player, intact, false).transform;
                var radians = angle * Mathf.Deg2Rad;
                wheel.localPosition = new Vector3(Mathf.Sin(radians) * 0.55f, 0.09f, Mathf.Cos(radians) * 0.55f);
                wheel.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            var wrecked = CreateGroup("Wrecked", t);
            CreateCube("Collapsed Seat", Vector3.zero, new Vector3(1.05f, 0.2f, 1.05f), p.wall, wrecked, false)
                .transform.SetLocalPositionAndRotation(new Vector3(0.1f, 0.14f, 0f), Quaternion.Euler(0f, 18f, 24f));
            CreateCube("Torn Back", Vector3.zero, new Vector3(1f, 0.16f, 1.1f), p.wall, wrecked, false)
                .transform.SetLocalPositionAndRotation(new Vector3(-0.55f, 0.1f, -0.55f), Quaternion.Euler(6f, -32f, 0f));
            CreateCylinder("Bent Base", Vector3.zero, new Vector3(1.05f, 0.05f, 1.05f), p.metal, wrecked, false)
                .transform.SetLocalPositionAndRotation(new Vector3(0.35f, 0.05f, 0.3f), Quaternion.Euler(14f, 0f, 9f));
            CreateCube("Dead Ember", Vector3.zero, new Vector3(0.4f, 0.05f, 0.35f), p.redDim, wrecked, false)
                .transform.localPosition = new Vector3(0f, 0.26f, 0f);
            wrecked.gameObject.SetActive(false);

            // Телеграф — красная полоса на полу по будущей траектории рывка.
            var telegraph = CreateCube("Telegraph", Vector3.zero, new Vector3(1.1f, 0.02f, 1f), p.red, t, false).transform;
            telegraph.localPosition = new Vector3(0f, 0.03f, 1f);
            telegraph.gameObject.SetActive(false);

            var warningObject = new GameObject("Warning Light", typeof(Light));
            warningObject.transform.SetParent(t, false);
            warningObject.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var warning = warningObject.GetComponent<Light>();
            warning.type = LightType.Point;
            warning.color = Hex("D8241D");
            warning.range = 8f;
            warning.intensity = 0f;
            warning.shadows = LightShadows.None;

            // Тело — триггер: таран не должен выталкивать героя наверх, а урон и
            // попадание броском считаются собственной логикой среза.
            var collider = root.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.2f, 1.7f, 1.2f);
            collider.center = new Vector3(0f, 0.85f, 0f);

            var body = root.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            root.GetComponent<OfficeChaser>().Configure(
                null,
                null,
                null,
                null,
                intact.gameObject,
                wrecked.gameObject,
                telegraph,
                warning);
            return root;
        }

        private static GameObject BuildPrinterTemplate(Palette p)
        {
            var root = new GameObject("Printer", typeof(BoxCollider), typeof(OfficeBreakable));
            var t = root.transform;

            var intact = CreateGroup("Intact", t);
            CreateCube("Body", Vector3.zero, new Vector3(1.5f, 0.85f, 1.05f), p.panel, intact, false)
                .transform.localPosition = new Vector3(0f, 0.42f, 0f);
            CreateCube("Lid", Vector3.zero, new Vector3(1.35f, 0.12f, 0.9f), p.metal, intact, false)
                .transform.localPosition = new Vector3(0f, 0.91f, 0f);
            CreateCube("Paper Tray", Vector3.zero, new Vector3(1.1f, 0.05f, 0.5f), p.paper, intact, false)
                .transform.localPosition = new Vector3(0f, 0.7f, -0.62f);
            CreateCube("Status LED", Vector3.zero, new Vector3(0.1f, 0.08f, 0.04f), p.red, intact, false)
                .transform.localPosition = new Vector3(0.5f, 0.62f, -0.54f);

            var broken = CreateGroup("Broken", t);
            var shell = CreateCube("Collapsed Shell", Vector3.zero, new Vector3(1.5f, 0.34f, 1.05f), p.wall, broken, false);
            shell.transform.localPosition = new Vector3(0f, 0.17f, 0f);
            shell.transform.localRotation = Quaternion.Euler(0f, 0f, 7f);
            CreateCube("Torn Panel", Vector3.zero, new Vector3(0.9f, 0.06f, 0.7f), p.metal, broken, false)
                .transform.SetLocalPositionAndRotation(new Vector3(0.72f, 0.14f, 0.28f), Quaternion.Euler(0f, 24f, 62f));
            CreateCube("Paper Debris L", Vector3.zero, new Vector3(0.5f, 0.03f, 0.4f), p.paper, broken, false)
                .transform.SetLocalPositionAndRotation(new Vector3(-0.85f, 0.03f, -0.4f), Quaternion.Euler(0f, 35f, 0f));
            CreateCube("Paper Debris R", Vector3.zero, new Vector3(0.42f, 0.03f, 0.36f), p.paper, broken, false)
                .transform.SetLocalPositionAndRotation(new Vector3(0.55f, 0.03f, -0.75f), Quaternion.Euler(0f, -18f, 0f));
            CreateCube("Ember", Vector3.zero, new Vector3(0.55f, 0.05f, 0.45f), p.red, broken, false)
                .transform.localPosition = new Vector3(0f, 0.35f, 0f);
            broken.gameObject.SetActive(false);

            var flashObject = new GameObject("Impact Flash", typeof(Light));
            flashObject.transform.SetParent(t, false);
            flashObject.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            var flash = flashObject.GetComponent<Light>();
            flash.type = LightType.Point;
            flash.color = Hex("D8241D");
            flash.range = 7f;
            flash.intensity = 0f;
            flash.shadows = LightShadows.None;

            var collider = root.GetComponent<BoxCollider>();
            collider.size = new Vector3(1.6f, 1f, 1.15f);
            collider.center = new Vector3(0f, 0.5f, 0f);

            root.GetComponent<OfficeBreakable>()
                .Configure("ПРИНТЕР", intact.gameObject, broken.gameObject, collider, flash, null);
            return root;
        }

        private static GameObject CreateDesk(string name, Vector3 position, Palette p, Transform parent, bool colliders)
        {
            var prefab = GetOrCreatePrefab(colliders ? "Desk" : "Desk_Background", () => BuildDeskTemplate(p, colliders));
            return InstantiatePrefab(prefab, name, position, parent);
        }

        private static GameObject BuildDeskTemplate(Palette p, bool colliders)
        {
            var root = new GameObject("Desk");
            var t = root.transform;
            CreateCube("Top", Vector3.zero, new Vector3(3.4f, 0.16f, 1.6f), p.panel, t, colliders).transform.localPosition = new Vector3(0f, 0.9f, 0f);
            foreach (var x in new[] { -1.45f, 1.45f })
            {
                foreach (var z in new[] { -0.6f, 0.6f })
                {
                    CreateCube("Leg", Vector3.zero, new Vector3(0.16f, 0.82f, 0.16f), p.metal, t, colliders).transform.localPosition = new Vector3(x, 0.42f, z);
                }
            }

            var monitor = CreateCube("Monitor", Vector3.zero, new Vector3(1.25f, 0.72f, 0.08f), p.player, t, colliders);
            monitor.transform.localPosition = new Vector3(0f, 1.35f, 0.15f);
            CreateCube("Monitor Glow", Vector3.zero, new Vector3(1.08f, 0.54f, 0.025f), p.playerRim, monitor.transform, false).transform.localPosition = new Vector3(0f, 0f, -0.055f);
            return root;
        }

        private static GameObject CreateChair(string name, Vector3 position, float yRotation, Palette p, Transform parent)
        {
            var prefab = GetOrCreatePrefab("Chair", () => BuildChairTemplate(p));
            return InstantiatePrefab(prefab, name, position, parent, Quaternion.Euler(0f, yRotation, 0f));
        }

        private static GameObject BuildChairTemplate(Palette p)
        {
            var root = new GameObject("Chair");
            var t = root.transform;
            CreateCube("Seat", Vector3.zero, new Vector3(0.9f, 0.18f, 0.9f), p.panel, t).transform.localPosition = new Vector3(0f, 0.55f, 0f);
            CreateCube("Back", Vector3.zero, new Vector3(0.9f, 1.05f, 0.16f), p.panel, t).transform.localPosition = new Vector3(0f, 1.08f, 0.38f);
            CreateCube("Post", Vector3.zero, new Vector3(0.12f, 0.5f, 0.12f), p.metal, t).transform.localPosition = new Vector3(0f, 0.27f, 0f);
            return root;
        }

        private static GameObject CreateServerRack(string name, Vector3 position, Palette p, Transform parent)
        {
            var prefab = GetOrCreatePrefab("ServerRack", () => BuildServerRackTemplate(p));
            return InstantiatePrefab(prefab, name, position, parent);
        }

        private static GameObject BuildServerRackTemplate(Palette p)
        {
            var root = new GameObject("Server Rack");
            var t = root.transform;
            CreateCube("Rack", Vector3.zero, new Vector3(1.4f, 2.6f, 1.4f), p.panel, t).transform.localPosition = new Vector3(0f, 1.3f, 0f);
            for (var i = 0; i < 4; i++)
            {
                CreateCube("Server Slot", Vector3.zero, new Vector3(1.12f, 0.32f, 0.06f), p.player, t, false).transform.localPosition = new Vector3(0f, 0.55f + (i * 0.48f), -0.73f);
                CreateCube("Status LED", Vector3.zero, new Vector3(0.08f, 0.08f, 0.035f), p.red, t, false).transform.localPosition = new Vector3(0.43f, 0.55f + (i * 0.48f), -0.775f);
            }

            return root;
        }

        private static GameObject CreateReceptionDesk(string name, Vector3 position, Palette p, Transform parent)
        {
            var prefab = GetOrCreatePrefab("ReceptionDesk", () => BuildReceptionDeskTemplate(p));
            return InstantiatePrefab(prefab, name, position, parent);
        }

        private static GameObject BuildReceptionDeskTemplate(Palette p)
        {
            var root = new GameObject("Reception Desk");
            var t = root.transform;
            CreateCube("Desk", Vector3.zero, new Vector3(5f, 1.3f, 1.4f), p.panel, t).transform.localPosition = new Vector3(0f, 0.65f, 0f);
            CreateCube("Light", Vector3.zero, new Vector3(4.6f, 0.08f, 0.05f), p.redDim, t, false).transform.localPosition = new Vector3(0f, 1.15f, -0.72f);
            return root;
        }

        private static void CreateTurnstile(string name, Vector3 position, Palette p, Transform parent)
        {
            var prefab = GetOrCreatePrefab("Turnstile", () => BuildTurnstileTemplate(p));
            InstantiatePrefab(prefab, name, position, parent);
        }

        private static GameObject BuildTurnstileTemplate(Palette p)
        {
            var root = new GameObject("Turnstile");
            var t = root.transform;
            CreateCylinder("Post", Vector3.zero, new Vector3(0.32f, 0.5f, 0.32f), p.metal, t).transform.localPosition = new Vector3(0f, 0.5f, 0f);
            CreateCylinder("Cap", Vector3.zero, new Vector3(0.38f, 0.05f, 0.38f), p.redDim, t, false).transform.localPosition = new Vector3(0f, 0.98f, 0f);
            for (var i = 0; i < 3; i++)
            {
                var spoke = new GameObject("Spoke").transform;
                spoke.SetParent(t, false);
                spoke.localPosition = new Vector3(0f, 0.85f, 0f);
                spoke.localRotation = Quaternion.Euler(0f, i * 120f, 0f);
                CreateCube("Arm", Vector3.zero, new Vector3(0.85f, 0.08f, 0.09f), p.redDim, spoke, false).transform.localPosition = new Vector3(0.45f, 0f, 0f);
            }

            return root;
        }

        private static GameObject GetOrCreatePrefab(string name, System.Func<GameObject> buildTemplate)
        {
            var path = $"{PrefabPath}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return existing != null ? existing : CreateOrUpdatePrefab(name, buildTemplate);
        }

        /// <summary>
        /// Пересобирает prefab из кода и сохраняет его по тому же пути, сохраняя GUID.
        /// Используется для ассетов, содержимое которых полностью принадлежит builder.
        /// </summary>
        private static GameObject CreateOrUpdatePrefab(string name, System.Func<GameObject> buildTemplate)
        {
            var path = $"{PrefabPath}/{name}.prefab";
            var template = buildTemplate();
            var prefab = PrefabUtility.SaveAsPrefabAsset(template, path);
            Object.DestroyImmediate(template);
            return prefab;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, string name, Vector3 position, Transform parent, Quaternion? rotation = null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = rotation ?? Quaternion.identity;
            return instance;
        }

        private static void CreateSplitWall(string name, float z, float totalWidth, float gapWidth, float height, Material material, Transform parent)
        {
            var segmentWidth = (totalWidth - gapWidth) * 0.5f;
            var center = (gapWidth * 0.5f) + (segmentWidth * 0.5f);
            CreateCube(name + " Left", new Vector3(-center, height * 0.5f, z), new Vector3(segmentWidth, height, 0.18f), material, parent);
            CreateCube(name + " Right", new Vector3(center, height * 0.5f, z), new Vector3(segmentWidth, height, 0.18f), material, parent);
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool collider = true)
        {
            return CreatePrimitive(name, PrimitiveType.Cube, position, scale, material, parent, collider);
        }

        private static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool collider = true)
        {
            return CreatePrimitive(name, PrimitiveType.Cylinder, position, scale, material, parent, collider);
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent, bool collider)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;

            if (!collider)
            {
                Object.DestroyImmediate(gameObject.GetComponent<Collider>());
            }

            if (material.name == "M_Glass")
            {
                gameObject.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            return gameObject;
        }

        private static TextMeshPro CreateWorldLabel(string name, string text, Vector3 position, float size, Color color, Transform parent)
        {
            var label = new GameObject(name, typeof(TextMeshPro));
            label.transform.SetParent(parent, false);
            label.transform.position = position;
            label.transform.localScale = Vector3.one * size;
            var mesh = label.GetComponent<TextMeshPro>();
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.alignment = TextAlignmentOptions.Center;
            mesh.color = color;
            return mesh;
        }

        private static Image CreateUiPanel(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateUiText(
            string name,
            Transform parent,
            int fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Shadow));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            var shadow = gameObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
        {
            var lightObject = new GameObject(name, typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, bool transparent = false, Color? emission = null)
        {
            var path = $"{MaterialPath}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = -1;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = path[..path.LastIndexOf('/')];
            var folder = path[(path.LastIndexOf('/') + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void EnsureBossLocalization()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationTables.Office);
            if (collection == null)
            {
                Debug.LogWarning("Office localization collection is missing; boss strings keep runtime fallbacks.");
                return;
            }

            var russian = LocalizationEditorSettings.GetLocale(new LocaleIdentifier("ru"));
            var english = LocalizationEditorSettings.GetLocale(new LocaleIdentifier("en"));
            var pseudo = LocalizationEditorSettings.GetLocale(new LocaleIdentifier("qps-ploc"));
            UpdateOfficeTable(russian != null ? collection.GetTable(russian.Identifier) as StringTable : null, BossLocalization, true);
            UpdateOfficeTable(english != null ? collection.GetTable(english.Identifier) as StringTable : null, BossLocalization, false);
            UpdateOfficeTable(pseudo != null ? collection.GetTable(pseudo.Identifier) as StringTable : null, BossLocalization, true);
            EditorUtility.SetDirty(collection.SharedData);
        }

        private static void UpdateOfficeTable(
            StringTable table,
            OfficeLocalizationEntry[] entries,
            bool russian)
        {
            if (table == null)
            {
                return;
            }

            for (var i = 0; i < entries.Length; i++)
            {
                var source = entries[i];
                var value = russian ? source.Russian : source.English;
                var entry = table.GetEntry(source.Key) ?? table.AddEntry(source.Key, value);
                entry.Value = value;
            }

            EditorUtility.SetDirty(table);
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private sealed class Palette
        {
            public Material shadow;
            public Material floor;
            public Material path;
            public Material wall;
            public Material panel;
            public Material metal;
            public Material glass;
            public Material paper;
            public Material redDim;
            public Material red;
            public Material text;
            public Material warm;
            public Material player;
            public Material playerRim;
        }

        private readonly struct OfficeLocalizationEntry
        {
            public OfficeLocalizationEntry(string key, string russian, string english)
            {
                Key = key;
                Russian = russian;
                English = english;
            }

            public string Key { get; }
            public string Russian { get; }
            public string English { get; }
        }

        /// <summary>
        /// Короткий Setup: сон в машине объясняется тремя пропускаемыми кадрами, а не
        /// длинной непрерываемой cutscene. Текст здесь — только RU-fallback.
        /// </summary>
        private static readonly (string speaker, string text)[] SetupFrames =
        {
            ("ДРУГ ЗА РУЛЁМ", "Очередь стоит четвёртый час. Спи, разбужу на границе."),
            ("ОН", "В рюкзаке ноутбук и кружка из офиса — всё, что он успел забрать с последней работы. Глаза закрываются сами."),
            ("СОН", "Лампы дневного света, ряды столов до темноты и красная табличка EXIT в конце этажа.")
        };

        private static readonly (string speaker, string text)[] AwakeningFrames =
        {
            ("СЕРВЕРНОЕ КОЛЬЦО", "Стойки смыкаются. Свет уходит, и офис наконец отпускает."),
            ("ОН", "Он просыпается в чужой машине от стука по крыше. Ладони мокрые, во рту привкус тонера."),
            ("ДРУГ ЗА РУЛЁМ", "Очередь пошла. Готовь паспорт — граница через двадцать минут.")
        };

        private static readonly OfficeLocalizationEntry[] BossLocalization =
        {
            new("boss.source", "СЕРВЕРНЫЙ БОСС", "SERVER BOSS"),
            new("boss.final_source", "СЕРВЕРНОЕ КОЛЬЦО", "SERVER RING"),
            new("boss.hologram.assembled", "СОТРУДНИК БОЛЬШЕ НЕ ТРЕБУЕТСЯ", "EMPLOYEE NO LONGER REQUIRED"),
            new("boss.hologram.battle", "ИИ ДЕЛАЕТ ЭТО БЫСТРЕЕ", "AI DOES THIS FASTER"),
            new("boss.hologram.mid.one", "ТЫ ОБУЧИЛ СВОЮ ЗАМЕНУ", "YOU TRAINED YOUR REPLACEMENT"),
            new("boss.hologram.mid.two", "ТВОЯ РОЛЬ ОПТИМИЗИРОВАНА", "YOUR ROLE HAS BEEN OPTIMIZED"),
            new("boss.hologram.encirclement", "МАСШТАБИРОВАНИЕ ЗАВЕРШЕНО", "SCALING COMPLETE"),
            new("boss.hologram.ring", "ЧЕЛОВЕЧЕСКАЯ ОШИБКА ОБНАРУЖЕНА", "HUMAN ERROR DETECTED"),
            new("boss.hologram.ring.beat", "ТЫ БЫЛ УЗКИМ МЕСТОМ", "YOU WERE THE BOTTLENECK"),
            new("boss.hologram.final", "ДОСТУП ОТОЗВАН", "ACCESS REVOKED"),
            new("boss.hologram.complete", "OFFBOARDING ЗАВЕРШЁН", "OFFBOARDING COMPLETE"),
            new("boss.status.assembly", "СТОЙКИ СХОДЯТСЯ • УПРАВЛЕНИЕ ЗАБЛОКИРОВАНО", "RACKS CONVERGING • CONTROL LOCKED"),
            new("boss.status.assembled", "ЕДИНЫЙ КОРПУС СОБРАН • БРОСАЙ ПРЕДМЕТЫ", "ASSEMBLED BODY ONLINE • THROW OBJECTS"),
            new("boss.status.hit", "СБОЙ КОРПУСА • {0}/{1}", "CHASSIS FAILURE • {0}/{1}"),
            new("boss.status.encircling", "ЛОЖНАЯ ПОБЕДА • СТОЙКИ ПЕРЕСТРАИВАЮТСЯ", "FALSE VICTORY • RACKS RECONFIGURING"),
            new("boss.status.ring", "КОЛЬЦО ЗАМКНУТО • ДВИГАЙСЯ, ПОКА ЕЩЁ МОЖЕШЬ", "RING CLOSED • MOVE WHILE YOU STILL CAN"),
            new("boss.status.final", "СИНХРОНИЗАЦИЯ СТОЕК • БЕЗОПАСНОЙ ЗОНЫ НЕТ", "RACKS SYNCHRONIZING • NO SAFE ZONE"),
            new("boss.status.complete", "СЮЖЕТНОЕ ПОРАЖЕНИЕ • ЭПИЗОД ЗАВЕРШЁН", "SCRIPTED DEFEAT • EPISODE COMPLETE"),
            new("boss.overlay.complete", "OFFBOARDING ЗАВЕРШЁН\nСОН ОБРЫВАЕТСЯ", "OFFBOARDING COMPLETE\nTHE DREAM CUTS OUT"),
            new("boss.objective.assembly", "СБОРКА КОРПУСА • НЕ ДВИГАТЬСЯ", "ASSEMBLING CHASSIS • HOLD POSITION"),
            new("boss.objective.assembled", "РАЗРУШЬ ЕДИНЫЙ КОРПУС   {0}/{1}", "BREAK THE ASSEMBLED BODY   {0}/{1}"),
            new("boss.objective.encircling", "ЛОЖНАЯ ПОБЕДА • СТОЙКИ ОКРУЖАЮТ ТЕБЯ", "FALSE VICTORY • RACKS ARE SURROUNDING YOU"),
            new("boss.objective.ring", "СЕРВЕРНОЕ КОЛЬЦО • ВЫХОДА НЕТ", "SERVER RING • NO EXIT"),
            new("boss.objective.final", "ФИНАЛЬНЫЙ УДАР • ОБЩИЙ ТЕЛЕГРАФ", "FINAL STRIKE • SHARED TELEGRAPH"),
            new("boss.objective.complete", "OFFBOARDING ЗАВЕРШЁН • СОН ОБОРВАН", "OFFBOARDING COMPLETE • DREAM ENDED"),
            new("feedback.broken", "СЛОМАНО", "BROKEN"),
            new("feedback.laptop", "НОУТБУК", "LAPTOP"),
            new("feedback.mug", "КРУЖКА", "MUG"),
            new("feedback.personal_items", "ЛИЧНЫЕ ВЕЩИ СОБРАНЫ", "PERSONAL ITEMS COLLECTED")
        };
    }
}
#endif
