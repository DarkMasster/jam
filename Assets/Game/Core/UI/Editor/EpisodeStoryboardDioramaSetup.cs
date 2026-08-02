using System.Linq;
using Jam.Core.Cutscenes;
using Jam.Episodes.Office;
using Jam.Episodes.Photo;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jam.Core.UI.Editor
{
    public static class EpisodeStoryboardDioramaSetup
    {
        [MenuItem("Jam/Cutscenes/Apply Episode 3D Storyboards")]
        public static void Apply()
        {
            ApplyPhoto();
            ApplyOffice();
            Debug.Log("Photo and Office storyboard dioramas configured.");
        }

        private static void ApplyPhoto()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/Prologue_Photo.unity", OpenSceneMode.Single);
            var diorama = Object.FindFirstObjectByType<PhotoRoomDioramaPresenter>();
            var root = GameObject.Find("PhotoStoryboardPresenters");
            if (root != null) Object.DestroyImmediate(root);
            root = new GameObject("PhotoStoryboardPresenters");
            ConfigurePhoto(root, diorama, "photo.prologue.intro", "Assets/Game/Episodes/Photo/Cutscenes/PhotoIntroStoryboard.asset", PhotoStoryboardScenePresenter.StoryboardMode.Intro);
            ConfigurePhoto(root, diorama, "photo.prologue.to_be_continued", "Assets/Game/Episodes/Photo/Cutscenes/PhotoOutroStoryboard.asset", PhotoStoryboardScenePresenter.StoryboardMode.Outro);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePhoto(GameObject root, PhotoRoomDioramaPresenter diorama, string id, string assetPath, PhotoStoryboardScenePresenter.StoryboardMode mode)
        {
            var presentation = Object.FindObjectsByType<UiStoryboardPresentation>(FindObjectsInactive.Include).First(item => item.CutsceneId == id);
            var adapterObject = new GameObject(mode + "3DPresenter", typeof(PhotoStoryboardScenePresenter)); adapterObject.transform.SetParent(root.transform, false);
            var adapter = adapterObject.GetComponent<PhotoStoryboardScenePresenter>(); adapter.Configure(diorama, mode);
            presentation.Configure(id, AssetDatabase.LoadAssetAtPath<StoryboardCutsceneAsset>(assetPath), adapter); EditorUtility.SetDirty(presentation);
        }

        private static void ApplyOffice()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/Prologue_Office.unity", OpenSceneMode.Single);
            var old = GameObject.Find("OfficeStoryboardDioramas"); if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("OfficeStoryboardDioramas");
            var setup = BuildOfficePresenter(root.transform, "SetupDiorama", new Vector3(0f, -30f, 0f), false, OutputChannels.Channel09, OutputChannels.Channel10);
            var awakening = BuildOfficePresenter(root.transform, "AwakeningDiorama", new Vector3(20f, -30f, 0f), true, OutputChannels.Channel11, OutputChannels.Channel12);
            ConfigureOffice("office.prologue.setup", "Assets/Game/Episodes/Office/Cutscenes/OfficeSetupStoryboard.asset", setup);
            ConfigureOffice("office.prologue.awakening", "Assets/Game/Episodes/Office/Cutscenes/OfficeAwakeningStoryboard.asset", awakening);
            setup.Hide(); awakening.Hide();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        private static OfficeStoryboardScenePresenter BuildOfficePresenter(Transform parent, string name, Vector3 offset, bool awakening, OutputChannels stageChannel, OutputChannels portraitChannel)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); root.transform.localPosition = offset;
            var presenter = root.AddComponent<OfficeStoryboardScenePresenter>();
            var visual = new GameObject("VisualRoot"); visual.transform.SetParent(root.transform, false);
            Cube(visual.transform, "Floor", new Vector3(0f, -0.1f, 1f), new Vector3(8f, .2f, 6f), new Color(.10f,.11f,.13f));
            Cube(visual.transform, "Backdrop", new Vector3(0f, 1.7f, 3.8f), new Vector3(8f,3.5f,.2f), awakening ? new Color(.08f,.04f,.04f) : new Color(.18f,.19f,.22f));
            for (var i=0;i<5;i++) Cube(visual.transform, awakening ? "Server"+i : "Car"+i, new Vector3(-2.8f+i*1.4f,.6f,1.6f+(i%2)*.5f), awakening ? new Vector3(.8f,1.4f,.7f) : new Vector3(1.1f,.8f,1.8f), awakening ? new Color(.18f,.04f,.04f) : new Color(.16f,.18f,.22f));
            var stage = OutputCamera(root.transform,"StageCamera",stageChannel,new Color(.025f,.03f,.04f));
            var shots = new[] { Shot(root.transform,"Shot01",new Vector3(4.5f,2.7f,-4f),new Vector3(0f,.8f,1.5f),40f,stageChannel), Shot(root.transform,"Shot02",new Vector3(0f,1.7f,-2.2f),new Vector3(0f,.8f,1.7f),32f,stageChannel), Shot(root.transform,"Shot03",new Vector3(-4f,2.3f,-3f),new Vector3(0f,1f,1.5f),38f,stageChannel) };
            var portraitRoot=new GameObject("PortraitRig"); portraitRoot.transform.SetParent(root.transform,false); portraitRoot.transform.localPosition=new Vector3(0f,-10f,0f);
            AddCharacter(portraitRoot.transform); Light(portraitRoot.transform,new Vector3(-.5f,2f,-.7f),new Color(1f,.65f,.5f),3.2f); Light(portraitRoot.transform,new Vector3(.6f,1.6f,-.4f),new Color(.3f,.45f,.8f),1.1f);
            var portrait=OutputCamera(portraitRoot.transform,"PortraitCamera",portraitChannel,new Color(.02f,.025f,.04f)); Shot(portraitRoot.transform,"PortraitShot",new Vector3(0f,1.7f,-1.05f),new Vector3(0f,1.7f,0f),29f,portraitChannel);
            var so=new SerializedObject(presenter); so.FindProperty("visualRoot").objectReferenceValue=visual; so.FindProperty("stageCamera").objectReferenceValue=stage; so.FindProperty("portraitCamera").objectReferenceValue=portrait; var a=so.FindProperty("shots"); a.arraySize=shots.Length; for(var i=0;i<shots.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=shots[i]; so.ApplyModifiedPropertiesWithoutUndo(); return presenter;
        }

        private static void ConfigureOffice(string id,string path,OfficeStoryboardScenePresenter presenter) { var p=Object.FindObjectsByType<UiStoryboardPresentation>(FindObjectsInactive.Include).First(x=>x.CutsceneId==id); p.Configure(id,AssetDatabase.LoadAssetAtPath<StoryboardCutsceneAsset>(path),presenter); EditorUtility.SetDirty(p); }
        private static Camera OutputCamera(Transform p,string n,OutputChannels c,Color bg){var g=new GameObject(n,typeof(Camera),typeof(CinemachineBrain));g.transform.SetParent(p,false);var cam=g.GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=bg;cam.nearClipPlane=.1f;cam.farClipPlane=30f;g.GetComponent<CinemachineBrain>().ChannelMask=c;return cam;}
        private static CinemachineCamera Shot(Transform p,string n,Vector3 pos,Vector3 target,float fov,OutputChannels c){var g=new GameObject(n,typeof(CinemachineCamera));g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.LookAt(p.TransformPoint(target));var cam=g.GetComponent<CinemachineCamera>();cam.OutputChannel=c;var l=cam.Lens;l.FieldOfView=fov;cam.Lens=l;return cam;}
        private static void Cube(Transform p,string n,Vector3 pos,Vector3 scale,Color color){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=scale;var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};g.GetComponent<Renderer>().sharedMaterial=m;}
        private static void AddCharacter(Transform p){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonOffice/Prefabs/Characters/SM_Chr_Developer_Male_01.prefab");var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);foreach(var t in g.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);g.transform.SetParent(p,false);g.transform.localEulerAngles=new Vector3(0f,180f,0f);}
        private static void Light(Transform p,Vector3 pos,Color color,float intensity){var g=new GameObject("Light",typeof(UnityEngine.Light));g.transform.SetParent(p,false);g.transform.localPosition=pos;var l=g.GetComponent<UnityEngine.Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=3f;l.shadows=LightShadows.None;}
    }
}
