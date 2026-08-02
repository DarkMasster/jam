using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using Jam.Episodes.Photo;

namespace Jam.Episodes.Photo.Editor
{
    public static class PhotoPolygonRoomSetup
    {
        private const string ScenePath = "Assets/Game/Scenes/Prologue_Photo.unity";
        private const string PrefabFolder = "Assets/Game/Episodes/Photo/Art/Prefabs";
        private const string MaterialFolder = "Assets/Game/Episodes/Photo/Art/Materials";
        private const string PrefabPath = PrefabFolder + "/PhotoRoomDiorama.prefab";
        private const string VendorRoot = "Assets/PolygonOffice/Prefabs/";

        [MenuItem("Jam/Photo/Create Polygon Room Diorama")]
        public static void Build()
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            var source = CreateRoomSource();
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
            Object.DestroyImmediate(source);
            if (savedPrefab == null)
            {
                Debug.LogError("Photo room prefab could not be saved; scene was left unchanged.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var old = GameObject.Find("PhotoRoomDiorama");
            if (old != null) Object.DestroyImmediate(old);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (instance == null)
            {
                Debug.LogError("Photo room prefab could not be instantiated; scene was left unchanged.");
                return;
            }
            instance.name = "PhotoRoomDiorama";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = instance;
            Debug.Log("Photo Polygon room diorama created and connected to Prologue_Photo.");
        }

        private static GameObject CreateRoomSource()
        {
            var root = new GameObject("PhotoRoomDiorama");
            var presenter = root.AddComponent<PhotoRoomDioramaPresenter>();
            var visualRoot = Child(root.transform, "VisualRoot").gameObject;

            AddVendor("Buildings/SM_Bld_Floor_Wood_01.prefab", visualRoot.transform, new Vector3(0f, 0f, -2.5f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Floor_Wood_01.prefab", visualRoot.transform, new Vector3(2.5f, 0f, -2.5f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Floor_Wood_01.prefab", visualRoot.transform, Vector3.zero, Vector3.zero);
            AddVendor("Buildings/SM_Bld_Floor_Wood_01.prefab", visualRoot.transform, new Vector3(2.5f, 0f, 0f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Wall_Blank_01.prefab", visualRoot.transform, new Vector3(0f, 0f, 2.5f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Wall_Blank_01.prefab", visualRoot.transform, new Vector3(2.5f, 0f, 2.5f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Wall_Blank_01.prefab", visualRoot.transform, new Vector3(-2.5f, 0f, -1.25f), new Vector3(0f, 90f, 0f));
            AddVendor("Buildings/SM_Bld_Wall_Trim_Door_01.prefab", visualRoot.transform, new Vector3(-2.5f, 0f, -3.75f), new Vector3(0f, 90f, 0f));

            AddVendor("Props/Furniture/SM_Prop_Couch_01.prefab", visualRoot.transform, new Vector3(1.25f, 0f, 1.75f), new Vector3(0f, 180f, 0f));
            AddVendor("Props/Furniture/SM_Prop_Desk_01.prefab", visualRoot.transform, new Vector3(-0.2f, 0f, 1.15f), new Vector3(0f, 180f, 0f));
            AddVendor("Props/Furniture/SM_Prop_Cabinets_01.prefab", visualRoot.transform, new Vector3(2.15f, 0f, 1.8f), new Vector3(0f, 180f, 0f));
            AddVendor("Props/Desk Props/SM_Prop_Desklamp_01.prefab", visualRoot.transform, new Vector3(-0.85f, 1.05f, 1.1f), new Vector3(0f, 180f, 0f));
            AddVendor("Props/Desk Props/SM_Prop_Laptop_01.prefab", visualRoot.transform, new Vector3(0f, 1.05f, 1.1f), new Vector3(0f, 180f, 0f));
            AddVendor("Props/Desk Props/SM_Prop_Cellphone_01.prefab", visualRoot.transform, new Vector3(0.55f, 1.03f, 1f), Vector3.zero);
            AddVendor("Props/Desk Props/SM_Prop_Cup_Red_01.prefab", visualRoot.transform, new Vector3(-1.25f, 1.03f, 1.05f), Vector3.zero);
            AddVendor("Props/Misc/SM_Prop_CardboardBox_01.prefab", visualRoot.transform, new Vector3(1.9f, 0f, 0.25f), new Vector3(0f, 15f, 0f));
            AddVendor("Props/Misc/SM_Prop_Briefcase_01.prefab", visualRoot.transform, new Vector3(1.35f, 0.15f, -0.7f), new Vector3(0f, -20f, 0f));
            CreateSuitcase(visualRoot.transform);

            var entranceRoot = CreateEntrance(root.transform);
            var airportRoot = CreateAirport(root.transform);

            var lighting = Child(visualRoot.transform, "Lighting");
            CreateLight(lighting, "WindowBlue", LightType.Directional, new Color(0.38f, 0.48f, 0.72f), 0.9f, new Vector3(42f, -32f, 0f));
            var warm = CreateLight(lighting, "LampWarm", LightType.Point, new Color(1f, 0.52f, 0.28f), 4.2f, Vector3.zero);
            warm.transform.position = new Vector3(-0.85f, 1.85f, 1f);
            warm.range = 5f;

            var cameraObject = new GameObject("StageCamera", typeof(Camera));
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.04f, 0.06f, 1f);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 80f;
            var stageBrain = cameraObject.AddComponent<CinemachineBrain>();
            stageBrain.ChannelMask = OutputChannels.Channel01;
            stageBrain.DefaultBlend = new CinemachineBlendDefinition
            {
                Style = CinemachineBlendDefinition.Styles.EaseInOut,
                Time = 0.2f
            };

            var portraitRoot = Child(root.transform, "PortraitRig").gameObject;
            portraitRoot.transform.localPosition = new Vector3(0f, -20f, 0f);
            var heroinePortrait = Child(portraitRoot.transform, "HeroinePortrait").gameObject;
            heroinePortrait.transform.localPosition = new Vector3(-3f, 0f, 0f);
            AddVendor("Characters/SM_Chr_Developer_Female_01.prefab", heroinePortrait.transform, Vector3.zero, new Vector3(0f, 180f, 0f));
            heroinePortrait.AddComponent<PhotoPortraitPose>().Configure(PhotoPortraitPose.PoseStyle.Heroine);
            var heroineKey = CreateLight(heroinePortrait.transform, "HeroineKey", LightType.Point, new Color(1f, 0.66f, 0.56f), 3.6f, Vector3.zero);
            heroineKey.transform.localPosition = new Vector3(-0.55f, 2f, -0.75f);
            heroineKey.range = 3f;
            var heroineFill = CreateLight(heroinePortrait.transform, "HeroineFill", LightType.Point, new Color(0.35f, 0.48f, 0.8f), 1.15f, Vector3.zero);
            heroineFill.transform.localPosition = new Vector3(0.65f, 1.65f, -0.45f);
            heroineFill.range = 2.5f;

            var motherPortrait = Child(portraitRoot.transform, "MotherPortrait").gameObject;
            motherPortrait.transform.localPosition = new Vector3(3f, 0f, 0f);
            AddVendor("Characters/SM_Chr_Boss_Female_01.prefab", motherPortrait.transform, Vector3.zero, new Vector3(0f, 180f, 0f));
            motherPortrait.AddComponent<PhotoPortraitPose>().Configure(PhotoPortraitPose.PoseStyle.Mother);
            var motherKey = CreateLight(motherPortrait.transform, "MotherKey", LightType.Point, new Color(0.95f, 0.72f, 0.58f), 3.25f, Vector3.zero);
            motherKey.transform.localPosition = new Vector3(0.5f, 2.05f, -0.75f);
            motherKey.range = 3f;
            var motherFill = CreateLight(motherPortrait.transform, "MotherFill", LightType.Point, new Color(0.3f, 0.42f, 0.72f), 0.9f, Vector3.zero);
            motherFill.transform.localPosition = new Vector3(-0.65f, 1.65f, -0.4f);
            motherFill.range = 2.5f;
            var officerPortrait = Child(portraitRoot.transform, "OfficerPortrait").gameObject;
            officerPortrait.transform.localPosition = new Vector3(6f, 0f, 0f);
            AddVendor("Characters/SM_Chr_Security_Male_01.prefab", officerPortrait.transform, Vector3.zero, new Vector3(0f, 180f, 0f));
            officerPortrait.AddComponent<PhotoPortraitPose>().Configure(PhotoPortraitPose.PoseStyle.Officer);
            var officerKey = CreateLight(officerPortrait.transform, "OfficerKey", LightType.Point, new Color(0.58f, 0.72f, 0.95f), 3.4f, Vector3.zero);
            officerKey.transform.localPosition = new Vector3(-0.45f, 2.05f, -0.75f);
            officerKey.range = 3f;
            var officerFill = CreateLight(officerPortrait.transform, "OfficerFill", LightType.Point, new Color(0.35f, 0.42f, 0.58f), 1f, Vector3.zero);
            officerFill.transform.localPosition = new Vector3(0.65f, 1.65f, -0.45f);
            officerFill.range = 2.5f;
            var portraitCameraObject = new GameObject("PortraitCamera", typeof(Camera));
            portraitCameraObject.transform.SetParent(portraitRoot.transform, false);
            portraitCameraObject.transform.localPosition = new Vector3(0f, 1.68f, -1.05f);
            portraitCameraObject.transform.LookAt(portraitRoot.transform.TransformPoint(new Vector3(0f, 1.68f, 0f)));
            var portraitCamera = portraitCameraObject.GetComponent<Camera>();
            portraitCamera.clearFlags = CameraClearFlags.SolidColor;
            portraitCamera.backgroundColor = new Color(0.055f, 0.06f, 0.08f, 1f);
            portraitCamera.fieldOfView = 28f;
            portraitCamera.nearClipPlane = 0.1f;
            portraitCamera.farClipPlane = 10f;
            var portraitBrain = portraitCameraObject.AddComponent<CinemachineBrain>();
            portraitBrain.ChannelMask = OutputChannels.Channel02;
            portraitBrain.DefaultBlend = new CinemachineBlendDefinition
            {
                Style = CinemachineBlendDefinition.Styles.Cut,
                Time = 0f
            };

            var wideTarget = Anchor(root.transform, "Look_Wide", new Vector3(0f, 1.05f, 0f));
            var photoTarget = Anchor(root.transform, "Look_Photo", new Vector3(-0.45f, 1.1f, 1.05f));
            var dialogueTarget = Anchor(root.transform, "Look_Dialogue", new Vector3(-1f, 1.3f, -0.8f));
            var wideCamera = CreateCinemachineCamera(root.transform, "CM_RoomWide", new Vector3(5f, 3.8f, -6f), wideTarget, 42f, OutputChannels.Channel01);
            var photoCamera = CreateCinemachineCamera(root.transform, "CM_RoomPhoto", new Vector3(2.2f, 2.1f, -1.8f), photoTarget, 36f, OutputChannels.Channel01);
            var dialogueCamera = CreateCinemachineCamera(root.transform, "CM_RoomDialogue", new Vector3(3.8f, 2.5f, -4.2f), dialogueTarget, 40f, OutputChannels.Channel01);
            var entranceWideTarget = Anchor(entranceRoot.transform, "Look_EntranceWide", new Vector3(0f, 1.25f, 1.8f));
            var entrancePhotoTarget = Anchor(entranceRoot.transform, "Look_EntrancePhoto", new Vector3(0.35f, 1.25f, 2f));
            var entranceReactionTarget = Anchor(entranceRoot.transform, "Look_EntranceReaction", new Vector3(0f, 1.3f, 1.8f));
            var entranceWideCamera = CreateCinemachineCamera(entranceRoot.transform, "CM_EntranceWide", new Vector3(0f, 2.4f, -5.8f), entranceWideTarget, 38f, OutputChannels.Channel01);
            var entrancePhotoCamera = CreateCinemachineCamera(entranceRoot.transform, "CM_EntrancePhoto", new Vector3(0.4f, 1.65f, -1.9f), entrancePhotoTarget, 28f, OutputChannels.Channel01);
            var entranceReactionCamera = CreateCinemachineCamera(entranceRoot.transform, "CM_EntranceReaction", new Vector3(3f, 2.25f, -3.8f), entranceReactionTarget, 36f, OutputChannels.Channel01);
            var airportPhotoTarget = Anchor(airportRoot.transform, "Look_AirportPhoto", new Vector3(0f, 1.45f, 1.6f));
            var borderControlTarget = Anchor(airportRoot.transform, "Look_BorderControl", new Vector3(0.55f, 1.45f, 2.05f));
            var airportSummaryTarget = Anchor(airportRoot.transform, "Look_AirportSummary", new Vector3(0.45f, 1.2f, 2.05f));
            var airportPhotoCamera = CreateCinemachineCamera(airportRoot.transform, "CM_AirportPhoto", new Vector3(-3.8f, 2.25f, -4.7f), airportPhotoTarget, 39f, OutputChannels.Channel01);
            var borderControlCamera = CreateCinemachineCamera(airportRoot.transform, "CM_BorderControl", new Vector3(0.55f, 1.72f, -1.7f), borderControlTarget, 30f, OutputChannels.Channel01);
            var airportSummaryCamera = CreateCinemachineCamera(airportRoot.transform, "CM_AirportSummary", new Vector3(2.8f, 2.1f, -3.2f), airportSummaryTarget, 34f, OutputChannels.Channel01);
            var heroinePortraitCamera = CreateCinemachineCamera(portraitRoot.transform, "CM_HeroinePortrait", new Vector3(-3f, 1.7f, -1.02f), null, 27f, OutputChannels.Channel02);
            heroinePortraitCamera.transform.LookAt(portraitRoot.transform.TransformPoint(new Vector3(-3f, 1.7f, 0f)));
            var motherPortraitCamera = CreateCinemachineCamera(portraitRoot.transform, "CM_MotherPortrait", new Vector3(3f, 1.72f, -1.08f), null, 29f, OutputChannels.Channel02);
            motherPortraitCamera.transform.LookAt(portraitRoot.transform.TransformPoint(new Vector3(3f, 1.72f, 0f)));
            var officerPortraitCamera = CreateCinemachineCamera(portraitRoot.transform, "CM_OfficerPortrait", new Vector3(6f, 1.74f, -1.08f), null, 29f, OutputChannels.Channel02);
            officerPortraitCamera.transform.LookAt(portraitRoot.transform.TransformPoint(new Vector3(6f, 1.72f, 0f)));

            var serialized = new SerializedObject(presenter);
            SetReference(serialized, "visualRoot", visualRoot);
            SetReference(serialized, "entranceRoot", entranceRoot);
            SetReference(serialized, "airportRoot", airportRoot);
            SetReference(serialized, "stageCamera", camera);
            SetReference(serialized, "portraitRoot", portraitRoot);
            SetReference(serialized, "portraitCamera", portraitCamera);
            SetReference(serialized, "heroinePortrait", heroinePortrait);
            SetReference(serialized, "motherPortrait", motherPortrait);
            SetReference(serialized, "officerPortrait", officerPortrait);
            SetReference(serialized, "wideCamera", wideCamera);
            SetReference(serialized, "photoCamera", photoCamera);
            SetReference(serialized, "dialogueCamera", dialogueCamera);
            SetReference(serialized, "entranceWideCamera", entranceWideCamera);
            SetReference(serialized, "entrancePhotoCamera", entrancePhotoCamera);
            SetReference(serialized, "entranceReactionCamera", entranceReactionCamera);
            SetReference(serialized, "airportPhotoCamera", airportPhotoCamera);
            SetReference(serialized, "borderControlCamera", borderControlCamera);
            SetReference(serialized, "airportSummaryCamera", airportSummaryCamera);
            SetReference(serialized, "heroinePortraitCamera", heroinePortraitCamera);
            SetReference(serialized, "motherPortraitCamera", motherPortraitCamera);
            SetReference(serialized, "officerPortraitCamera", officerPortraitCamera);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            motherPortrait.SetActive(false);
            motherPortraitCamera.gameObject.SetActive(false);
            officerPortrait.SetActive(false);
            officerPortraitCamera.gameObject.SetActive(false);
            portraitRoot.SetActive(false);
            entranceRoot.SetActive(false);
            airportRoot.SetActive(false);
            visualRoot.SetActive(false);
            return root;
        }

        private static GameObject CreateEntrance(Transform parent)
        {
            var root = Child(parent, "EntranceDiorama").gameObject;
            AddVendor("Buildings/SM_Bld_Floor_Tiles_01.prefab", root.transform, Vector3.zero, Vector3.zero);
            AddVendor("Buildings/SM_Bld_Floor_Tiles_01.prefab", root.transform, new Vector3(2.5f, 0f, 0f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Wall_Blank_01.prefab", root.transform, new Vector3(0f, 0f, 2.5f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Wall_Blank_01.prefab", root.transform, new Vector3(2.5f, 0f, 2.5f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Wall_Trim_Door_01.prefab", root.transform, new Vector3(-2.5f, 0f, -1.25f), new Vector3(0f, 90f, 0f));

            var mailboxMaterial = EnsureMaterial(MaterialFolder + "/MailboxDark.mat", new Color(0.16f, 0.19f, 0.23f));
            var summonsMaterial = EnsureMaterial(MaterialFolder + "/SummonsPaper.mat", new Color(0.78f, 0.74f, 0.64f));
            var summonsMarkMaterial = EnsureMaterial(MaterialFolder + "/SummonsMark.mat", new Color(0.62f, 0.08f, 0.1f));
            var butterflyMaterial = EnsureMaterial(MaterialFolder + "/ButterflyPink.mat", new Color(0.92f, 0.34f, 0.62f));
            var mailboxes = Child(root.transform, "Mailboxes");
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    var box = CreatePrimitive(
                        mailboxes,
                        $"Mailbox_{row}_{column}",
                        PrimitiveType.Cube,
                        new Vector3(-1.35f + column * 0.9f, 0.75f + row * 0.62f, 2.16f),
                        new Vector3(0.78f, 0.52f, 0.28f),
                        mailboxMaterial);
                    var slot = CreatePrimitive(
                        box.transform,
                        "Slot",
                        PrimitiveType.Cube,
                        new Vector3(0f, 0.1f, -0.15f),
                        new Vector3(0.48f, 0.035f, 0.025f),
                        summonsMaterial);
                    slot.transform.localPosition = new Vector3(0f, 0.1f, -0.15f);
                }
            }

            var summons = CreatePrimitive(mailboxes, "Summons", PrimitiveType.Cube, new Vector3(0.45f, 1.48f, 1.93f), new Vector3(0.42f, 0.58f, 0.035f), summonsMaterial);
            summons.transform.localEulerAngles = new Vector3(-8f, 0f, 4f);
            var summonsMark = CreatePrimitive(summons.transform, "OfficialMark", PrimitiveType.Cube, Vector3.zero, new Vector3(0.58f, 0.13f, 1.2f), summonsMarkMaterial);
            summonsMark.transform.localPosition = new Vector3(0f, 0.18f, -0.62f);
            var butterfly = Child(mailboxes, "Butterfly");
            butterfly.localPosition = new Vector3(0.9f, 1.65f, 1.78f);
            CreatePrimitive(butterfly, "Body", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.08f, 0.24f, 0.06f), mailboxMaterial);
            var leftWing = CreatePrimitive(butterfly, "LeftWing", PrimitiveType.Sphere, new Vector3(-0.13f, 0.03f, 0f), new Vector3(0.25f, 0.3f, 0.035f), butterflyMaterial);
            leftWing.transform.localEulerAngles = new Vector3(0f, 0f, -24f);
            var rightWing = CreatePrimitive(butterfly, "RightWing", PrimitiveType.Sphere, new Vector3(0.13f, 0.03f, 0f), new Vector3(0.25f, 0.3f, 0.035f), butterflyMaterial);
            rightWing.transform.localEulerAngles = new Vector3(0f, 0f, 24f);

            var cold = CreateLight(root.transform, "EntranceCold", LightType.Directional, new Color(0.42f, 0.52f, 0.72f), 1f, new Vector3(48f, -28f, 0f));
            cold.shadows = LightShadows.Soft;
            var practical = CreateLight(root.transform, "EntrancePractical", LightType.Point, new Color(1f, 0.73f, 0.46f), 2.2f, Vector3.zero);
            practical.transform.localPosition = new Vector3(0f, 2.4f, 0.2f);
            practical.range = 6f;
            return root;
        }

        private static GameObject CreateAirport(Transform parent)
        {
            var root = Child(parent, "AirportDiorama").gameObject;
            for (var x = 0; x < 3; x++)
            {
                AddVendor("Buildings/SM_Bld_Floor_Tiles_01.prefab", root.transform, new Vector3(-2.5f + x * 2.5f, 0f, 0f), Vector3.zero);
            }
            AddVendor("Buildings/SM_Bld_Wall_Blank_01.prefab", root.transform, new Vector3(-2.5f, 0f, 2.5f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Wall_Blank_01.prefab", root.transform, new Vector3(0f, 0f, 2.5f), Vector3.zero);
            AddVendor("Buildings/SM_Bld_Wall_Blank_01.prefab", root.transform, new Vector3(2.5f, 0f, 2.5f), Vector3.zero);
            AddVendor("Props/Wall Props/SM_Prop_TV_Wall_01.prefab", root.transform, new Vector3(-1.7f, 2.05f, 2.25f), new Vector3(0f, 180f, 0f));
            AddVendor("Props/Wall Props/SM_Prop_Clock_01.prefab", root.transform, new Vector3(1.8f, 2.1f, 2.25f), new Vector3(0f, 180f, 0f));
            AddVendor("Props/Furniture/SM_Prop_Desk_01.prefab", root.transform, new Vector3(0.55f, 0f, 1.7f), new Vector3(0f, 180f, 0f));
            AddVendor("Props/Furniture/SM_Prop_Chair_01.prefab", root.transform, new Vector3(-1.8f, 0f, 0.6f), new Vector3(0f, 25f, 0f));
            AddVendor("Props/Furniture/SM_Prop_Chair_01.prefab", root.transform, new Vector3(-0.9f, 0f, 0.6f), new Vector3(0f, -20f, 0f));

            var metal = EnsureMaterial(MaterialFolder + "/AirportMetal.mat", new Color(0.16f, 0.2f, 0.27f));
            var glass = EnsureMaterial(MaterialFolder + "/AirportGlass.mat", new Color(0.25f, 0.52f, 0.65f));
            var passport = EnsureMaterial(MaterialFolder + "/PassportDarkRed.mat", new Color(0.32f, 0.04f, 0.08f));
            var stamp = EnsureMaterial(MaterialFolder + "/PassportStamp.mat", new Color(0.72f, 0.16f, 0.2f));

            var booth = Child(root.transform, "PassportBooth");
            CreatePrimitive(booth, "Glass", PrimitiveType.Cube, new Vector3(0.55f, 1.7f, 1.83f), new Vector3(2.5f, 1.35f, 0.045f), glass);
            CreatePrimitive(booth, "WindowFrameTop", PrimitiveType.Cube, new Vector3(0.55f, 2.4f, 1.78f), new Vector3(2.7f, 0.1f, 0.1f), metal);
            CreatePrimitive(booth, "WindowFrameLeft", PrimitiveType.Cube, new Vector3(-0.78f, 1.7f, 1.78f), new Vector3(0.1f, 1.5f, 0.1f), metal);
            CreatePrimitive(booth, "WindowFrameRight", PrimitiveType.Cube, new Vector3(1.88f, 1.7f, 1.78f), new Vector3(0.1f, 1.5f, 0.1f), metal);
            var passportProp = CreatePrimitive(booth, "Passport", PrimitiveType.Cube, new Vector3(0.35f, 1.08f, 0.95f), new Vector3(0.42f, 0.045f, 0.58f), passport);
            passportProp.transform.localEulerAngles = new Vector3(0f, -12f, 0f);
            CreatePrimitive(passportProp.transform, "Stamp", PrimitiveType.Cylinder, new Vector3(0.12f, 0.62f, 0.06f), new Vector3(0.22f, 0.015f, 0.22f), stamp);

            var rails = Child(root.transform, "QueueRails");
            for (var i = 0; i < 3; i++)
            {
                CreatePrimitive(rails, "Post_" + i, PrimitiveType.Cylinder, new Vector3(-2.2f + i * 1.2f, 0.55f, -0.4f), new Vector3(0.06f, 0.55f, 0.06f), metal);
            }
            CreatePrimitive(rails, "Belt", PrimitiveType.Cube, new Vector3(-1f, 0.92f, -0.4f), new Vector3(2.4f, 0.08f, 0.06f), passport);

            var cold = CreateLight(root.transform, "AirportCold", LightType.Directional, new Color(0.52f, 0.65f, 0.88f), 1.15f, new Vector3(52f, -28f, 0f));
            cold.shadows = LightShadows.Soft;
            var counterLight = CreateLight(root.transform, "CounterLight", LightType.Point, new Color(0.72f, 0.86f, 1f), 2.8f, Vector3.zero);
            counterLight.transform.localPosition = new Vector3(0.5f, 2.6f, 0.3f);
            counterLight.range = 6f;
            return root;
        }

        private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static CinemachineCamera CreateCinemachineCamera(
            Transform parent,
            string name,
            Vector3 position,
            Transform lookTarget,
            float fieldOfView,
            OutputChannels channel)
        {
            var go = new GameObject(name, typeof(CinemachineCamera));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            if (lookTarget != null) go.transform.LookAt(lookTarget);
            var camera = go.GetComponent<CinemachineCamera>();
            camera.OutputChannel = channel;
            var lens = camera.Lens;
            lens.FieldOfView = fieldOfView;
            camera.Lens = lens;
            return camera;
        }

        private static GameObject AddVendor(string relativePath, Transform parent, Vector3 position, Vector3 rotation)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VendorRoot + relativePath);
            if (prefab == null) { Debug.LogWarning($"PolygonOffice prefab not found: {relativePath}"); return null; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            RemoveMissingScripts(instance);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = position;
            instance.transform.localEulerAngles = rotation;
            return instance;
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            }
        }

        private static void CreateSuitcase(Transform parent)
        {
            var root = Child(parent, "ProjectOwned_Suitcase");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root, false);
            body.transform.localScale = new Vector3(1.15f, 0.65f, 0.32f);
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Handle";
            handle.transform.SetParent(root, false);
            handle.transform.localPosition = new Vector3(0f, 0.43f, 0f);
            handle.transform.localScale = new Vector3(0.38f, 0.18f, 0.12f);
            root.localPosition = new Vector3(1.8f, 0.35f, -1f);
            root.localEulerAngles = new Vector3(0f, -12f, 0f);
        }

        private static Light CreateLight(Transform parent, string name, LightType type, Color color, float intensity, Vector3 rotation)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.SetParent(parent, false);
            go.transform.localEulerAngles = rotation;
            var light = go.GetComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            return light;
        }

        private static Transform Anchor(Transform parent, string name, Vector3 position)
        {
            var anchor = Child(parent, name);
            anchor.localPosition = position;
            return anchor;
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void SetReference(SerializedObject target, string property, Object value)
        {
            target.FindProperty(property).objectReferenceValue = value;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var split = path.LastIndexOf('/');
            var parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
