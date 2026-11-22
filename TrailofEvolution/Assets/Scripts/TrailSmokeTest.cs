using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrailOfMutation
{
    // Opt-in standalone integration run, exercising real scene/coroutine/UI code.
    public sealed class TrailSmokeTest : MonoBehaviour
    {
        bool failed, checkedHand, checkedEvent;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--trail-smoke") < 0) return;
            TrailGame.AutoPlay = false;
            Application.runInBackground = true;
            var go = new GameObject("Integration test"); DontDestroyOnLoad(go); go.AddComponent<TrailSmokeTest>();
        }
        void Awake() { Application.logMessageReceived += OnLog; }
        void OnDestroy() { Application.logMessageReceived -= OnLog; }
        void OnLog(string message, string trace, LogType type) { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) failed = true; }
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            Capture("StartupScene");
            yield return new WaitForSecondsRealtime(.5f);
            Click("HOW TO PLAY");
            if (Time.timeScale != 0) failed = true;
            yield return null;
            Click("BACK TO THE TRAIL");
            if (Time.timeScale != 1) failed = true;
            yield return null;
            Click("Start");
            yield return null;
            Capture("StartingModal");
            Click("EMBER");
            if (TrailGame.Avatar != 1) failed = true;
            Click("X");
            yield return null;
            if (Time.timeScale != 1) failed = true;
            Click("Start");
            yield return null;
            Click("BEGIN THE TRAIL   >");
            yield return new WaitForSecondsRealtime(2);
            Capture("GameBoardScene");
            var game = FindAnyObjectByType<TrailGame>();
            var idleCard = GameObject.Find("Hand card 0");
            if (idleCard.GetComponent<HandCardHover>()) { failed = true; Debug.LogError("Two-card hands must not have hover behavior"); }
            var idlePosition = ((RectTransform)idleCard.transform).anchoredPosition;
            ExecuteEvents.Execute(idleCard, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.2f);
            if (((RectTransform)idleCard.transform).anchoredPosition != idlePosition || idleCard.transform.localScale != Vector3.one) {
                failed = true; Debug.LogError("Idle hand changed on hover");
            }
            idleCard.GetComponent<Button>().onClick.Invoke();
            if (GameObject.Find("Modal") || Time.timeScale != 1) { failed = true; Debug.LogError("Two-card hand clicks must have no effect"); }
            var navigation = FindAnyObjectByType<BoardNavigation>();
            var boardRect = (RectTransform)navigation.transform.parent;
            var initialPosition = boardRect.anchoredPosition;
            ExecuteEvents.Execute(navigation.gameObject, new PointerEventData(EventSystem.current) {
                position = new Vector2(640,360), scrollDelta = new Vector2(0,6)
            }, ExecuteEvents.scrollHandler);
            if (Mathf.Abs(navigation.Zoom - 1.1f) > .001f) { failed = true; Debug.LogError("One wheel notch must zoom ten percent"); }
            ExecuteEvents.Execute(navigation.gameObject, new PointerEventData(EventSystem.current) {
                position = new Vector2(640,360), scrollDelta = new Vector2(0,600)
            }, ExecuteEvents.scrollHandler);
            if (navigation.Zoom != 2) { failed = true; Debug.LogError("Zoom must stop at 200 percent"); }
            foreach (var offset in new[] { new Vector2(10000,10000), new Vector2(-20000,0), new Vector2(0,-20000), new Vector2(20000,0) })
            {
                var pointer = new PointerEventData(EventSystem.current) { position = Vector2.zero, button = PointerEventData.InputButton.Left };
                ExecuteEvents.Execute(navigation.gameObject,pointer,ExecuteEvents.beginDragHandler);
                pointer.position = offset;
                ExecuteEvents.Execute(navigation.gameObject,pointer,ExecuteEvents.dragHandler);
                var fraction = navigation.VisibleFraction;
                if (fraction.x < .599f || fraction.y < .599f) { failed = true; Debug.LogError("Board must retain 60 percent on each axis: " + fraction); }
            }
            Capture("BoardZoom200");
            navigation.SetZoom(-10,new Vector2(640,360));
            if (navigation.Zoom != 1) { failed = true; Debug.LogError("Zoom must stop at 100 percent"); }
            boardRect.anchoredPosition = initialPosition; navigation.Constrain();
            var die3D = FindAnyObjectByType<TabletopDie>();
            for (int face=1; face<=6; face++) {
                die3D.SetFace(face);
                if (die3D.UpwardValue != face) { failed = true; Debug.LogError("Wrong upward dice face: " + face); }
            }
            var rollAnimation = StartCoroutine(die3D.Roll(6,731));
            yield return new WaitForSecondsRealtime(.55f);
            Capture("DiceRolling");
            yield return rollAnimation;
            if (die3D.IsRolling || die3D.Value != 6 || die3D.UpwardValue != 6) { failed = true; Debug.LogError("Dice did not settle on its result"); }
            Capture("DiceSettled");
            die3D.SetFace(1);
            var canvas = FindAnyObjectByType<Canvas>();
            var preview = game.ScenarioVisual(canvas.transform.GetChild(0), 6, 390, 175, 350, 485);
            Capture("HardSeeds"); Destroy(preview.gameObject);
            yield return null;
            Click("MENU  /  ESC");
            if (Time.timeScale != 0) failed = true;
            yield return new WaitForSecondsRealtime(.2f);
            Click("CONTINUE");
            if (Time.timeScale != 1) failed = true;
            if (game.HumanHandCount != 2) { failed = true; Debug.LogError("Initial hand must have two cards"); }
            TrailGame.AutoPlay = false; Time.timeScale = 60;
            Click("ROLL DICE");
            // Three randomly drawn games plus explicit 3D captures can exceed 90 seconds on a software GPU.
            float deadline = Time.realtimeSinceStartup + 180;
            while (TrailGame.CompletedGames < 3 && Time.realtimeSinceStartup < deadline && !failed)
            {
                int count = TrailGame.CompletedGames;
                while (TrailGame.CompletedGames == count && Time.realtimeSinceStartup < deadline && !failed) { yield return PumpInteraction(); yield return null; }
                if (TrailGame.CompletedGames == 1)
                {
                    Capture("Victory");
                    yield return new WaitForSecondsRealtime(.2f);
                }
                if (TrailGame.CompletedGames < 3) {
                    TrailGame.HumanFirst = !TrailGame.HumanFirst;
                    TrailGame.Avatar = TrailGame.CompletedGames == 1 ? 0 : 2;
                    SceneManager.LoadScene("GameBoardScene"); yield return null;
                    yield return null; Time.timeScale = 60;
                    Capture(TrailGame.Avatar == 0 ? "DeerBoard" : "TurtleBoard");
                }
            }
            bool passed = !failed && checkedHand && checkedEvent && TrailGame.CompletedGames >= 3;
            Debug.Log(passed ? "TRAIL_SMOKE_OK: three completed games, both starting orders" : "TRAIL_SMOKE_FAILED: completed=" + TrailGame.CompletedGames + ", runtimeError=" + failed);
            Application.Quit(passed ? 0 : 1);
        }
        IEnumerator PumpInteraction()
        {
            var game = FindAnyObjectByType<TrailGame>();
            if (game.SelectingDiscard)
            {
                if (game.HumanHandCount != 3) { failed = true; Debug.LogError("Discard selection must expose three cards"); yield break; }
                var card = GameObject.Find("Hand card 0");
                var hover = card.GetComponent<HandCardHover>();
                var rect = (RectTransform)card.transform; var home = rect.anchoredPosition;
                if (!checkedHand)
                {
                    bool promptVisible = false;
                    foreach (var label in FindObjectsByType<Text>(FindObjectsSortMode.None))
                        promptVisible |= label.text == "Select one card to discard";
                    if (!promptVisible) { failed = true; Debug.LogError("Discard prompt must be visible above the hand"); }
                    Capture("ThreeCardHand");
                    ExecuteEvents.Execute(card, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
                    yield return new WaitForSecondsRealtime(.25f);
                    if (!hover.IsRaised || rect.anchoredPosition.y <= home.y + 10 || rect.localScale.x <= 1.01f) {
                        failed = true; Debug.LogError("Hover must highlight and lift the hand card");
                    }
                    Capture("HandHover"); checkedHand = true;
                }
                card.GetComponent<Button>().onClick.Invoke();
                yield break;
            }
            foreach (var text in FindObjectsByType<Text>(FindObjectsSortMode.None))
            {
                var button = text.GetComponentInParent<Button>();
                if (!button || !button.interactable) continue;
                if (text.text == "Apply Event")
                {
                    if (!checkedEvent) { Capture("ApplyEvent"); checkedEvent = true; }
                    button.onClick.Invoke(); yield break;
                }
                if (text.text == "ROLL DICE" || text.text == "STAY ON TRAIL") { button.onClick.Invoke(); yield break; }
            }
        }
        void Capture(string name)
        {
            var tabletop = FindAnyObjectByType<TabletopBoard>();
            if (tabletop) tabletop.RenderForCapture();
            var die = FindAnyObjectByType<TabletopDie>();
            if (die) die.RenderForCapture();
            // Hidden windows do not present a swapchain. Explicit camera rendering verifies the real UI.
            var canvas = FindAnyObjectByType<Canvas>(); var cam = Camera.main;
            var target = RenderTexture.GetTemporary(1280, 720, 24, RenderTextureFormat.ARGB32);
            var oldTarget = cam.targetTexture; var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1;
            cam.targetTexture = target; Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var oldActive = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath, "../" + name + ".png"), texture.EncodeToPNG());
            if (texture.GetPixel(640, 360).maxColorComponent < .02f) { failed = true; Debug.LogError("Blank render: " + name); }
            RenderTexture.active = oldActive; cam.targetTexture = oldTarget;
            canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
            RenderTexture.ReleaseTemporary(target); Destroy(texture);
        }
        void Click(string caption)
        {
            foreach (var text in FindObjectsByType<Text>(FindObjectsSortMode.None))
                if (text.text == caption)
                {
                    var button = text.GetComponentInParent<Button>();
                    if (button && button.interactable) { button.onClick.Invoke(); return; }
                }
            failed = true; Debug.LogError("Smoke test could not click " + caption);
        }
    }
}
