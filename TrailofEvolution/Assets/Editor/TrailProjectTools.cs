using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrailOfMutation.Editor
{
    public static class TrailProjectTools
    {
        [MenuItem("Trail of Mutation/Create Scenes")]
        public static void CreateScenes()
        {
            Directory.CreateDirectory("Assets/Scenes");
            foreach (var name in new[] { "StartupScene", "GameBoardScene" })
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camera.tag = "MainCamera"; camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                camera.GetComponent<Camera>().backgroundColor = TrailUI.Ink;
                new GameObject("Trail of Mutation", typeof(TrailGame));
                EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + name + ".unity");
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/StartupScene.unity", true), new EditorBuildSettingsScene("Assets/Scenes/GameBoardScene.unity", true) };
            PlayerSettings.productName = "Trail of Mutation";
            PlayerSettings.companyName = "Woodland Games";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.defaultWebScreenWidth = 1280; PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/StartupScene.unity");
            ValidateRules();
            Debug.Log("TRAIL_SETUP_OK");
        }

        [MenuItem("Trail of Mutation/Validate Rules")]
        public static void ValidateRules()
        {
            Require(GameRules.Move(1, -6) == 0, "Start clamp");
            Require(GameRules.Move(43, 6) == GameRules.Finish, "Finish overshoot");
            foreach (var card in GameRules.Scenarios)
                foreach (Mutation trait in Enum.GetValues(typeof(Mutation)))
                    Require(card.Movement(trait) == ((card.HarmfulTrait ? trait == card.Trait : trait != card.Trait) ? -card.Penalty : 0), "Scenario protection");
            Require(GameRules.Scenarios[3].Movement(Mutation.BrightColors) == -6, "Bright colors vulnerability");
            Require(GameRules.Scenarios[3].Movement(Mutation.LargeBeak) == 0, "Eagle eyes only harms bright colors");
            Require(GameRules.Scenarios[6].Movement(Mutation.LargeBeak) == 0, "Large Beak cracks hard seeds");
            foreach (Mutation trait in Enum.GetValues(typeof(Mutation)))
                if (trait != Mutation.LargeBeak) Require(GameRules.Scenarios[6].Movement(trait) == -3, "Hard Seeds penalizes other traits");
            Require(GameRules.ShouldReplace(Mutation.Camouflage, Mutation.LargeBeak), "AI values Large Beak protection");
            Require(GameRules.ShouldReplace(Mutation.BrightColors, Mutation.SharpClaws), "AI adapts");
            Require(!GameRules.ShouldReplace(Mutation.SharpClaws, Mutation.BrightColors), "AI avoids regression");
            var deck = new CardDeck<Mutation>(GameRules.MutationDeck, new System.Random(731));
            var a = deck.Draw(); var b = deck.Draw();
            for (int i = 0; i < 1000; i++) { var next = deck.Draw(); deck.Discard(a); a = next; Require(deck.DrawCount + deck.DiscardCount == 6, "Held cards stay out of deck"); }
            int shells = 0; foreach (var m in GameRules.MutationDeck) if (m == Mutation.HardShell) shells++;
            Require(shells == 2, "Two original hard shell cards");
            var scenarios = new CardDeck<int>(new[] { 0, 1, 2, 3, 4, 5, 6 }, new System.Random(9));
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < 7; i++) { int card = scenarios.Draw(); Require(seen.Add(card), "No scenario repeats before exhaustion"); scenarios.Discard(card); }
            Require(scenarios.Draw() >= 0, "Scenario reshuffle");
            var pair = new[] { Mutation.LargeBeak, Mutation.Speed };
            Require(GameRules.Scenarios[0].Movement(pair) == 0 && GameRules.Scenarios[6].Movement(pair) == 0, "Both held traits protect");
            Require(GameRules.Scenarios[3].Movement(new[] { Mutation.BrightColors, Mutation.Camouflage }) == -6, "Held harmful trait still triggers");
            Require(GameRules.ChooseDiscard(new[] { Mutation.HardShell, Mutation.HardShell, Mutation.Speed }) != 2, "AI removes duplicate protection");
            var handDeck = new CardDeck<Mutation>(GameRules.MutationDeck, new System.Random(48));
            var human = new System.Collections.Generic.List<Mutation> { handDeck.Draw(), handDeck.Draw() };
            var cpu = new System.Collections.Generic.List<Mutation> { handDeck.Draw(), handDeck.Draw() };
            for (int turn = 0; turn < 500; turn++)
            {
                var hand = turn % 2 == 0 ? human : cpu;
                hand.Add(handDeck.Draw()); Require(hand.Count == 3, "Draw before discard");
                int discard = turn % 3;
                handDeck.Discard(hand[discard]); hand.RemoveAt(discard);
                Require(hand.Count == 2, "Keep two physical cards");
                Require(handDeck.DrawCount + handDeck.DiscardCount + human.Count + cpu.Count == 8, "Two hands conserve deck");
            }
            Debug.Log("TRAIL_RULES_OK: clamps, all 49 mutation/scenario combinations, Large Beak protection and AI, duplicate cards, 1000 redraws, reshuffle");
        }
        static void Require(bool condition, string label) { if (!condition) throw new Exception("Rule validation failed: " + label); }

        [MenuItem("Trail of Mutation/Build Windows Player")]
        public static void BuildWindows()
        {
            EnsureBoardMaterials();
            ValidateRules();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/StartupScene.unity", "Assets/Scenes/GameBoardScene.unity" },
                locationPathName = "Builds/Windows/TrailOfMutation.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("TRAIL_BUILD_OK");
        }

        // Resource materials keep these runtime-created mesh shaders in player builds.
        public static void EnsureBoardMaterials()
        {
            foreach (var name in new[] { "BoardPaint", "PawnResin" })
            {
                var path = "Assets/Resources/Board/" + name + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(path)) continue;
                var shader = Shader.Find(name == "BoardPaint" ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                AssetDatabase.CreateAsset(new Material(shader), path);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
