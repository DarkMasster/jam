using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Jam.Core.Audio;
using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jam.Episodes.Photo.Editor
{
    public static class PhotoProductionContentSetup
    {
        private const string Root = "Assets/Game/Episodes/Photo";
        private const string DialogueFolder = Root + "/Dialogues";
        private const string AudioFolder = Root + "/Audio";
        private const string ScenePath = "Assets/Game/Scenes/Prologue_Photo.unity";

        [MenuItem("Jam/Photo/Create Production Dialogue And Audio")]
        public static void CreateProductionContent()
        {
            EnsureFolder(DialogueFolder);
            EnsureFolder(AudioFolder);

            var mother = EnsureDialogue(
                DialogueFolder + "/PhotoMotherDialogue.asset",
                "production.mother.prompt",
                "production.mother.honest",
                "production.mother.lie");
            var border = EnsureDialogue(
                DialogueFolder + "/PhotoBorderDialogue.asset",
                "production.border.prompt",
                "production.border.honest",
                "production.border.recognition");

            var room = EnsureCue("PhotoRoomAmbience", "photo.ambience.room", AudioBus.Ambience, true,
                EnsureWav("room-rain.wav", 2.4f, (time, random) => 0.025f * random + 0.012f * Mathf.Sin(time * 31f)));
            var airport = EnsureCue("PhotoAirportAmbience", "photo.ambience.airport", AudioBus.Ambience, true,
                EnsureWav("airport-terminal.wav", 2.8f, (time, random) => 0.018f * random + 0.014f * Mathf.Sin(time * 19f)));
            var shutter = EnsureCue("PhotoShutter", "photo.sfx.shutter", AudioBus.Sfx, false,
                EnsureWav("camera-shutter.wav", 0.16f, (time, random) => time < 0.035f ? random * 0.7f : Mathf.Sin(time * 720f) * Mathf.Exp(-32f * time) * 0.35f));
            var door = EnsureCue("PhotoDoor", "photo.sfx.door", AudioBus.Sfx, false,
                EnsureWav("room-door.wav", 0.42f, (time, random) => Mathf.Sin(time * (95f - 70f * time)) * Mathf.Exp(-7f * time) * 0.32f));
            var stamp = EnsureCue("PhotoPassportStamp", "photo.sfx.passport_stamp", AudioBus.Sfx, false,
                EnsureWav("passport-stamp.wav", 0.2f, (time, random) => time < 0.055f ? random * 0.8f : Mathf.Sin(time * 190f) * Mathf.Exp(-28f * time) * 0.25f));

            ConfigureScene(mother, border, room, airport, shutter, door, stamp);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Photo production Dialogue Trees and AudioCues are ready.");
        }

        private static DialogueTree EnsureDialogue(string path, string promptKey, string firstChoiceKey, string secondChoiceKey)
        {
            var existing = AssetDatabase.LoadAssetAtPath<DialogueTree>(path);
            if (existing != null)
            {
                return existing;
            }

            var tree = ScriptableObject.CreateInstance<DialogueTree>();
            tree.name = Path.GetFileNameWithoutExtension(path);
            var prompt = tree.AddNode<StatementNode>(new Vector2(0f, 0f));
            prompt.statement = new Statement(promptKey);
            var choices = tree.AddNode<MultipleChoiceNode>(new Vector2(0f, 220f));
            var options = new List<MultipleChoiceNode.Choice>
            {
                new(new Statement(firstChoiceKey)),
                new(new Statement(secondChoiceKey))
            };
            typeof(MultipleChoiceNode)
                .GetField("availableChoices", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(choices, options);
            var firstFinish = tree.AddNode<FinishNode>(new Vector2(-180f, 460f));
            var secondFinish = tree.AddNode<FinishNode>(new Vector2(180f, 460f));
            tree.ConnectNodes(prompt, choices);
            tree.ConnectNodes(choices, firstFinish, 0);
            tree.ConnectNodes(choices, secondFinish, 1);
            tree.primeNode = prompt;
            tree.SelfSerialize();
            AssetDatabase.CreateAsset(tree, path);
            EditorUtility.SetDirty(tree);
            return tree;
        }

        private static AudioCue EnsureCue(string assetName, string stableId, AudioBus bus, bool loop, AudioClip clip)
        {
            var path = AudioFolder + "/" + assetName + ".asset";
            var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(path);
            if (cue == null)
            {
                cue = ScriptableObject.CreateInstance<AudioCue>();
                cue.name = assetName;
                AssetDatabase.CreateAsset(cue, path);
            }

            var serialized = new SerializedObject(cue);
            serialized.FindProperty("stableId").stringValue = stableId;
            serialized.FindProperty("bus").enumValueIndex = (int)bus;
            serialized.FindProperty("loop").boolValue = loop;
            serialized.FindProperty("cooldownSeconds").floatValue = loop ? 0f : 0.08f;
            serialized.FindProperty("maxSimultaneous").intValue = loop ? 1 : 3;
            var clips = serialized.FindProperty("clips");
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cue);
            return cue;
        }

        private static AudioClip EnsureWav(string fileName, float duration, Func<float, float, float> sample)
        {
            var path = AudioFolder + "/" + fileName;
            if (!File.Exists(path))
            {
                const int sampleRate = 22050;
                var sampleCount = Mathf.CeilToInt(duration * sampleRate);
                var samples = new short[sampleCount];
                var random = new System.Random(fileName.GetHashCode());
                for (var index = 0; index < sampleCount; index++)
                {
                    var time = (float)index / sampleRate;
                    var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                    samples[index] = (short)(Mathf.Clamp(sample(time, noise), -1f, 1f) * short.MaxValue);
                }

                File.WriteAllBytes(path, BuildWave(samples, sampleRate));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static byte[] BuildWave(short[] samples, int sampleRate)
        {
            using var stream = new MemoryStream(44 + samples.Length * sizeof(short));
            using var writer = new BinaryWriter(stream);
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + samples.Length * sizeof(short));
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * sizeof(short));
            writer.Write((short)sizeof(short));
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(samples.Length * sizeof(short));
            foreach (var value in samples) writer.Write(value);
            return stream.ToArray();
        }

        private static void ConfigureScene(
            DialogueTree mother,
            DialogueTree border,
            AudioCue room,
            AudioCue airport,
            AudioCue shutter,
            AudioCue door,
            AudioCue stamp)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = GameObject.Find("PhotoWhiteboxRoot");
            if (root == null)
            {
                throw new InvalidOperationException("PhotoWhiteboxRoot is missing from Prologue_Photo.");
            }

            var blackboard = root.GetComponent<Blackboard>() ?? root.AddComponent<Blackboard>();
            var dialogueController = root.GetComponent<DialogueTreeController>() ?? root.AddComponent<DialogueTreeController>();
            dialogueController.blackboard = blackboard;
            var controller = root.GetComponent<PhotoWhiteboxController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("motherDialogue").objectReferenceValue = mother;
            serialized.FindProperty("borderDialogue").objectReferenceValue = border;
            serialized.FindProperty("roomAmbienceCue").objectReferenceValue = room;
            serialized.FindProperty("airportAmbienceCue").objectReferenceValue = airport;
            serialized.FindProperty("shutterCue").objectReferenceValue = shutter;
            serialized.FindProperty("doorCue").objectReferenceValue = door;
            serialized.FindProperty("passportStampCue").objectReferenceValue = stamp;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialogueController);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var separator = path.LastIndexOf('/');
            var parent = path[..separator];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path[(separator + 1)..]);
        }
    }
}
