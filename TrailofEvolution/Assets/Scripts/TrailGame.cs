using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TrailOfMutation.TrailUI;

namespace TrailOfMutation
{
    public sealed class TrailGame : MonoBehaviour
    {
        public static int Avatar;
        public static bool HumanFirst = true;
        public static bool AutoPlay;
        public static int CompletedGames;
        readonly System.Random random = new System.Random();
        readonly List<RectTransform> leaves = new List<RectTransform>();
        readonly List<RectTransform> confetti = new List<RectTransform>();
        readonly List<string> history = new List<string>();
        readonly Vector2[] points = {
            new Vector2(102.60f, 621.00f),
            new Vector2(104.76f, 517.50f),
            new Vector2(113.40f, 434.70f),
            new Vector2(118.80f, 365.70f),
            new Vector2(124.20f, 296.70f),
            new Vector2(145.80f, 227.70f),
            new Vector2(183.60f, 165.60f),
            new Vector2(243.00f, 131.10f),
            new Vector2(302.40f, 138.00f),
            new Vector2(334.80f, 179.40f),
            new Vector2(344.52f, 248.40f),
            new Vector2(331.56f, 317.40f),
            new Vector2(324.00f, 386.40f),
            new Vector2(324.00f, 448.50f),
            new Vector2(345.60f, 510.60f),
            new Vector2(378.00f, 565.80f),
            new Vector2(432.00f, 603.75f),
            new Vector2(496.80f, 600.30f),
            new Vector2(550.80f, 496.80f),
            new Vector2(551.88f, 414.00f),
            new Vector2(547.56f, 338.10f),
            new Vector2(543.24f, 269.10f),
            new Vector2(551.88f, 207.00f),
            new Vector2(594.00f, 151.80f),
            new Vector2(648.00f, 110.40f),
            new Vector2(702.00f, 96.60f),
            new Vector2(745.20f, 124.20f),
            new Vector2(756.00f, 179.40f),
            new Vector2(756.00f, 248.40f),
            new Vector2(749.52f, 317.40f),
            new Vector2(745.20f, 386.40f),
            new Vector2(745.20f, 476.10f),
            new Vector2(770.04f, 531.30f),
            new Vector2(820.80f, 558.90f),
            new Vector2(880.20f, 558.90f),
            new Vector2(930.96f, 517.50f),
            new Vector2(961.20f, 462.30f),
            new Vector2(982.80f, 400.20f),
            new Vector2(999.00f, 338.10f),
            new Vector2(1008.72f, 276.00f),
            new Vector2(1010.88f, 213.90f),
            new Vector2(1013.04f, 165.60f),
            new Vector2(1014.12f, 124.20f),
            new Vector2(1014.12f, 82.80f),
            new Vector2(1014.12f, 44.85f)
        };
        RectTransform root, board, overlay, cardArea, handArea, deckVisual;
        RectTransform[] tokens = new RectTransform[2];
        Text status, turnLabel, humanStatus, botStatus, discardPrompt;
        Button rollButton;
        TabletopDie dice3D;
        PlayerCharacter[] players;
        CardDeck<Mutation> mutationDeck;
        CardDeck<int> scenarioDeck;
        int active, round = 1, choice = -1;
        bool rollRequested, finished, paused, selectingDiscard, handBusy;
        public bool SelectingDiscard => selectingDiscard;
        public int HumanHandCount => players[0].Hand.Count;
        public int HandDiscardCount { get; private set; }
        const float HandWidth = 154;
        Vector2 HandOrigin => RootPoint(handArea);
        Vector2 DeckOrigin => RootPoint(deckVisual);
        Vector2 RootPoint(RectTransform rect)
        {
            var point = root.InverseTransformPoint(rect.position);
            return new Vector2(point.x - root.rect.xMin, point.y - root.rect.yMax);
        }
        float priorScale = 1;
        static readonly string[] Avatars = { "FERN", "EMBER", "TIDAL" };
        Color HumanColor => new[] { Mint, Coral, Hex("86C9DE") }[Avatar];

        void Start()
        {
            Time.timeScale = AutoPlay ? 60 : 1;
            Application.targetFrameRate = 60;
            var canvasGo = new GameObject("Trail Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // Keep the existing board coordinates, uniformly fitted inside the 1280 x 720 canvas.
            root = Rect("Reference surface", canvas.transform, 0, 0, 1600, 1000);
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f); root.pivot = new Vector2(.5f, .5f); root.anchoredPosition = Vector2.zero;
            root.localScale = Vector3.one * .72f;
            if (FindAnyObjectByType<EventSystem>() == null) new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Panel(root, -2000, -2000, 5600, 5000, Hex("102E2C"));
            BuildAmbient();
            if (SceneManager.GetActiveScene().name == "GameBoardScene") BuildGame(); else BuildStartup();
        }

        void BuildAmbient()
        {
            for (int i = 0; i < 26; i++)
            {
                float x = (i * 149) % 1570, y = i % 2 == 0 ? 15 + (i % 4) * 14 : 940 + (i % 3) * 12;
                var leaf = Panel(root, x, y, 8 + i % 5 * 3, 5 + i % 3 * 2, new Color(.5f, .72f, .47f, .25f), true).rectTransform;
                leaves.Add(leaf);
            }
        }

        void BuildStartup()
        {
            var background = Rect("Painted evolution landscape (1280 x 720)", root.parent, 0, 0, 1280, 720).gameObject.AddComponent<RawImage>();
            background.rectTransform.pivot = new Vector2(.5f, .5f);
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
            background.texture = Resources.Load<Texture2D>("Startup/EvolutionBackground");
            background.raycastTarget = false;
            background.transform.SetAsFirstSibling();
            // The reference surface's backdrop must not cover the full-screen painting.
            root.GetChild(0).gameObject.SetActive(false);
            var fit = background.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = (float)background.texture.width / background.texture.height;

            var subtitle = Label(root, "A small change. A different survival story.", 250, 335, 1100, 55, 30, Paper, TextAnchor.MiddleCenter, true);
            var subtitleShadow = subtitle.gameObject.AddComponent<Shadow>();
            subtitleShadow.effectColor = Ink;
            subtitleShadow.effectDistance = new Vector2(2, -2);

            // The reference surface is centred on screen: (800, 500) is its centre.
            var start = Button(root, "Start", 580, 425, 440, 150, Color.clear, ShowStartingModal);
            start.name = "Start";
            var lettering = Rect("Painted Start lettering", start.transform, 0, 0, 440, 150).gameObject.AddComponent<RawImage>();
            lettering.texture = Resources.Load<Texture2D>("Startup/StartLettering");
            lettering.raycastTarget = false;
            start.targetGraphic = lettering;
            var hoverOutline = start.gameObject.AddComponent<StartHoverOutline>();
            hoverOutline.Initialize(lettering);
            start.onClick.AddListener(hoverOutline.Hide);
            start.GetComponentInChildren<Text>().color = Color.clear;
            Button(root, "HOW TO PLAY", 682, 840, 236, 50, Paper, ShowRules);
            var credit = Label(root, "Based on the original handmake game by BASIS Grade 7", 150, 935, 1300, 35, 20, Paper, TextAnchor.MiddleCenter);
            credit.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -2);
        }

        void ShowStartingModal()
        {
            if (overlay) return;
            var setup = Modal(700, 650);
            overlay.name = "StartingModal";
            Label(setup, "Choose your creature", 42, 40, 555, 50, 34, Ink, bold: true);
            Button(setup, "X", 614, 30, 48, 48, Hex("DDE4D2"), CloseModal);
            var avatarText = Label(setup, "", 42, 320, 616, 35, 20, Ink, TextAnchor.MiddleCenter);
            var cards = new Button[3];
            System.Action refresh = () => {
                avatarText.text = "Selected: " + Avatars[Avatar];
                for (int i = 0; i < cards.Length; i++)
                    cards[i].GetComponent<Outline>().enabled = i == Avatar;
            };
            for (int i = 0; i < 3; i++)
            {
                int selected = i;
                var card = Button(setup, Avatars[i], 42 + i * 210, 120, 196, 185, new[] { Mint, Coral, Hex("86C9DE") }[i], () => { Avatar = selected; refresh(); });
                cards[i] = card;
                var caption = card.GetComponentInChildren<Text>();
                caption.rectTransform.anchoredPosition = new Vector2(8, -140);
                caption.rectTransform.sizeDelta = new Vector2(180, 36);
                var silhouette = Panel(card.transform, 18, 16, 160, 118, Ink);
                silhouette.sprite = CreatureSilhouettes.Get(i);
                silhouette.type = Image.Type.Simple;
                silhouette.preserveAspect = true;
                var outline = card.gameObject.AddComponent<Outline>();
                outline.effectColor = Ink; outline.effectDistance = new Vector2(3, -3);
            }
            refresh();
            var order = Button(setup, HumanFirst ? "FIRST TURN: YOU" : "FIRST TURN: COMPUTER", 42, 385, 616, 56, Hex("DDE4D2"), () => { });
            order.onClick.AddListener(() => { HumanFirst = !HumanFirst; order.GetComponentInChildren<Text>().text = HumanFirst ? "FIRST TURN: YOU" : "FIRST TURN: COMPUTER"; });
            Label(setup, "Choose who goes first in place of the paper\ngame's longest-hair / most-superstitious rule.", 42, 465, 616, 60, 18, Ink, TextAnchor.MiddleCenter);
            Button(setup, "BEGIN THE TRAIL   >", 42, 550, 616, 66, Gold, () => { CloseModal(); SceneManager.LoadScene("GameBoardScene"); });
        }

        void BuildGame()
        {
            players = new[] { new PlayerCharacter(Avatars[Avatar], ControllerKind.Human), new PlayerCharacter("MOSS", ControllerKind.Computer) };
            mutationDeck = new CardDeck<Mutation>(GameRules.MutationDeck, random);
            var scenarioIds = new int[GameRules.Scenarios.Length];
            for (int i = 0; i < scenarioIds.Length; i++) scenarioIds[i] = i;
            scenarioDeck = new CardDeck<int>(scenarioIds, random);
            var dock = Rect("Right edge dock", root, 1206, 35, 364, 930);
            Button(dock, "RULES", 0, 0, 120, 56, Mint, ShowRules);
            Button(dock, "MENU  /  ESC", 132, 0, 232, 56, Paper, ToggleMenu);
            board = Panel(root, -70, -50, 1460, 1110, Hex("D7E5CE")).rectTransform;
            board.SetSiblingIndex(1);
            DrawBoard();
            for (int i = 0; i < 2; i++)
            {
                tokens[i] = Rect("Animal position " + i, board, 0, 0, 1, 1);
                board.GetComponent<TabletopBoard>().AddPawn(tokens[i], i == 0 ? Avatar : (Avatar + 1) % 3, i == 0 ? HumanColor : Gold);
                PositionToken(i);
            }
            var sideImage = Panel(dock, 0, 85, 364, 845, Paper);
            sideImage.raycastTarget = true;
            var side = sideImage.rectTransform;
            turnLabel = Label(side, "DEALING MUTATIONS", 17, 20, 330, 32, 20, Ink, bold: true);
            status = Label(side, "Each creature starts with two mutations.", 17, 60, 330, 90, 18, Ink);
            humanStatus = Label(side, "", 17, 152, 160, 28, 15, Ink, bold: true);
            botStatus = Label(side, "", 187, 152, 160, 28, 15, Ink, bold: true);
            rollButton = Button(side, "ROLL DICE", 17, 205, 210, 58, Gold, () => rollRequested = true); rollButton.interactable = false;
            deckVisual = PaintedCards.Back(side, 260, 185, 63, 87.5f, 14);
            dice3D = Rect("Rolling 3D die", side, 17, 290, 330, 180).gameObject.AddComponent<TabletopDie>();
            dice3D.Initialize();
            cardArea = Rect("Turn decisions", side, 17, 310, 330, 150);
            discardPrompt = Label(side, "Select one card to discard", 17, 480, 330, 44, 20, Ink, TextAnchor.MiddleCenter, true);
            discardPrompt.gameObject.SetActive(false);
            handArea = Rect("Your hand", side, 17, 620, 330, 214);
            var edgeDock = dock.gameObject.AddComponent<ScreenEdgeDock>();
            edgeDock.Side = side; edgeDock.Hand = handArea; edgeDock.Prompt = discardPrompt.rectTransform;
            Canvas.ForceUpdateCanvases(); edgeDock.Layout();
            StartCoroutine(GameLoop());
        }

        void DrawBoard()
        {
            board.GetComponent<Image>().color = Color.clear;
            board.gameObject.AddComponent<TabletopBoard>().Initialize();
        }

        IEnumerator GameLoop()
        {
            for (int slot = 0; slot < 2; slot++) for (int i = 0; i < 2; i++)
            {
                var drawn = mutationDeck.Draw();
                if (i == 0) yield return DealToHand(drawn, players[i].Hand.Count);
                players[i].Hand.Add(drawn); Refresh(); RenderHand();
            }
            int first = HumanFirst ? 0 : 1;
            while (!finished)
            {
                for (int t = 0; t < 2; t++)
                {
                    active = (first + t) % 2; var player = players[active];
                    turnLabel.text = player.Controller == ControllerKind.Human ? "YOUR TURN" : "COMPUTER'S TURN";
                    status.text = player.Name + " is ready to explore.";
                    rollRequested = false; rollButton.interactable = player.Controller == ControllerKind.Human;
                    if (player.Controller == ControllerKind.Human && !AutoPlay) yield return new WaitUntil(() => rollRequested && !paused);
                    else yield return new WaitForSeconds(.85f);
                    rollButton.interactable = false;
                    int rolled = random.Next(1, 7);
                    yield return dice3D.Roll(rolled, random.Next());
                    Note(player.Name + " rolled " + rolled + ".");
                    yield return MoveCharacter(active, rolled, true);
                    if (finished) yield break;
                    if (rolled == 2) yield return Scenario(-1, false);
                    Refresh(); yield return new WaitForSeconds(.45f);
                }
                turnLabel.text = "TIME TO ADAPT";
                for (int i = 0; i < 2; i++) yield return OfferMutation(i);
                yield return Scenario(-1, false);
                round++;
            }
        }

        IEnumerator MoveCharacter(int index, int amount, bool shortcuts = false)
        {
            var p = players[index]; int target = GameRules.Move(p.Position, amount);
            while (p.Position != target)
            {
                int next = p.Position + Math.Sign(target - p.Position);
                yield return AnimateToken(index, next); p.Position = next; Refresh();
                if (GameRules.Won(p)) { Win(index); yield break; }
                if (shortcuts && amount > 0 && next == GameRules.ShortcutEntry)
                {
                    choice = -1;
                    status.text = p.Name + " found a shortcut. Jump to 31, then pay two random setbacks (only this creature).";
                    if (p.Controller == ControllerKind.Human && !AutoPlay) { ShowChoice("TAKE SHORTCUT", "STAY ON TRAIL"); yield return new WaitUntil(() => choice >= 0 && !paused); Clear(cardArea); dice3D.gameObject.SetActive(true); }
                    else { yield return new WaitForSeconds(.6f); choice = 0; }
                    if (choice == 0)
                    {
                        yield return AnimateToken(index, GameRules.ShortcutExit); p.Position = GameRules.ShortcutExit;
                        Note(p.Name + " took the shortcut.");
                        yield return Scenario(index, true); yield return Scenario(index, true);
                        yield break; // Taking the bridge uses the rest of this roll.
                    }
                }
            }
        }

        IEnumerator AnimateToken(int index, int destination)
        {
            var from = tokens[index].anchoredPosition; var to = TokenPosition(index, destination);
            var tabletop = board.GetComponent<TabletopBoard>();
            for (float t = 0; t < 1; t += Time.deltaTime / .18f) {
                tokens[index].anchoredPosition = Vector2.Lerp(from, to, t);
                tabletop.SetHop(index, Mathf.Sin(t * Mathf.PI) * .3f);
                yield return null;
            }
            tabletop.SetHop(index, 0);
            tokens[index].anchoredPosition = to;
        }
        Vector2 TokenPosition(int index, int position) => new Vector2(points[position].x + (index == 0 ? -22 : 22), -points[position].y + (index == 0 ? 9 : -9));
        void PositionToken(int index) => tokens[index].anchoredPosition = TokenPosition(index, players[index].Position);

        IEnumerator OfferMutation(int index)
        {
            var p = players[index]; var offered = mutationDeck.Draw();
            status.text = p.Controller == ControllerKind.Human ? "You drew a mutation." : "Moss is choosing a card to discard.";
            if (index == 0) yield return DealToHand(offered, 2);
            p.Hand.Add(offered);
            choice = -1;
            if (p.Controller == ControllerKind.Human)
            {
                selectingDiscard = true; RenderHand();
                if (!AutoPlay) yield return new WaitUntil(() => choice >= 0 && !paused);
                else { yield return new WaitForSeconds(.7f); choice = GameRules.ChooseDiscard(p.Hand); }
                selectingDiscard = false; discardPrompt.gameObject.SetActive(false);
            }
            else { yield return new WaitForSeconds(.7f); choice = GameRules.ChooseDiscard(p.Hand); }
            int discardedSlot = choice;
            handBusy = true;
            var discarded = p.Hand[discardedSlot];
            if (index == 0)
            {
                // Hide exactly the selected physical card before animating it back to the discard pile.
                var departure = HandOrigin + new Vector2(HandX(discardedSlot, 3), -HandY(discardedSlot, 3));
                foreach (Transform child in handArea) if (child.name == "Hand card " + discardedSlot)
                {
                    departure = HandOrigin + ((RectTransform)child).anchoredPosition;
                    child.gameObject.SetActive(false);
                }
                var flying = MutationCard(root, discarded, 0, 0, HandWidth, HandWidth * 1.386f);
                yield return Fly(flying, departure, DeckOrigin, .45f, true);
                Destroy(flying.gameObject);
                HandDiscardCount++;
            }
            p.Hand.RemoveAt(discardedSlot); mutationDeck.Discard(discarded);
            Note(p.Name + " discarded " + GameRules.MutationNames[(int)discarded] + ".");
            handBusy = false; Refresh(); RenderHand();
        }

        IEnumerator Scenario(int onlyPlayer, bool forceNegative)
        {
            int cardIndex = scenarioDeck.Draw(); var card = GameRules.Scenarios[cardIndex];
            string name = string.IsNullOrEmpty(card.Title) ? "Hawks" : card.Title;
            turnLabel.text = forceNegative ? "SHORTCUT COST" : "SHARED SCENARIO";
            status.text = forceNegative ? "Only " + players[onlyPlayer].Name + " pays this setback. Mutations cannot cancel the shortcut cost." : "Both creatures must follow this scenario.";
            Clear(cardArea);
            var visual = ScenarioVisual(root, cardIndex, 390, 175, 350, 485);
            yield return Fly(visual, new Vector2(1440, -780), new Vector2(390, -175), .45f);
            // Give the human unlimited reading time; the AI cannot dismiss a new shared event.
            choice = -1;
            var apply = Button(visual, "Apply Event", 28, 490, 294, 58, Gold, () => { if (!paused && choice < 0) choice = 0; });
            if (AutoPlay) yield return new WaitForSeconds(.1f); else yield return new WaitUntil(() => choice >= 0 && !paused);
            apply.interactable = false;
            for (int i = 0; i < 2; i++)
            {
                if (onlyPlayer >= 0 && onlyPlayer != i) continue;
                int delta = forceNegative ? -card.Penalty : card.Movement(players[i].Hand);
                Note(players[i].Name + ": " + (delta == 0 ? "safe from " + name : name + " / back " + -delta) + ".");
                yield return MoveCharacter(i, delta);
            }
            yield return Fly(visual, visual.anchoredPosition, new Vector2(1460, -770), .4f, true);
            Destroy(visual.gameObject); scenarioDeck.Discard(cardIndex); Refresh();
        }

        public RectTransform ScenarioVisual(Transform parent, int index, float x, float y, float w, float h)
        {
            var card = GameRules.Scenarios[index];
            // Portrait ratio is invariant, including callers supplying a legacy landscape rectangle.
            h = Mathf.Max(h, w * 1.386f);
            var r = PaintedCards.Face(parent, index + 7, x, y, w, h,
                string.IsNullOrEmpty(card.Title) ? "Hawks" : card.Title,
                card.Description + "\n\n" + card.Instruction);
            r.GetComponent<RawImage>().raycastTarget = true;
            return r;
        }

        RectTransform MutationCard(Transform parent, Mutation m, float x, float y, float w, float h)
        {
            return PaintedCards.Face(parent, (int)m, x, y, w, w * 1.386f,
                GameRules.MutationNames[(int)m], PaintedCards.Traits[(int)m]);
        }
        float HandX(int slot, int count) => count == 3 ? slot * 88.2f : slot * 172.2f;
        float HandY(int slot, int count) => count == 3 && slot == 1 ? -52.5f : 0;
        IEnumerator DealToHand(Mutation mutation, int slot)
        {
            handBusy = true;
            // Make room for the third card before it travels from the deck into the hand.
            RenderHand(slot == 2 ? 3 : 2);
            var card = MutationCard(root, mutation, 0, 0, HandWidth, HandWidth * 1.386f);
            yield return Fly(card, DeckOrigin, HandOrigin + new Vector2(HandX(slot, slot == 2 ? 3 : 2), 0), .55f);
            Destroy(card.gameObject);
            handBusy = false;
        }
        IEnumerator Fly(RectTransform r, Vector2 from, Vector2 to, float duration, bool shrink = false)
        {
            for (float t = 0; t < 1; t += Time.deltaTime / duration)
            {
                float smooth = t * t * (3 - 2 * t);
                r.anchoredPosition = Vector2.Lerp(from, to, smooth) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 140;
                r.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * Mathf.PI) * -18);
                r.localScale = Vector3.one * (shrink ? Mathf.Lerp(1, .15f, t) : Mathf.Lerp(.5f, 1, t)); yield return null;
            }
            r.anchoredPosition = to; r.localEulerAngles = Vector3.zero; r.localScale = Vector3.one * (shrink ? .15f : 1);
        }
        void ShowChoice(string yes, string no)
        {
            dice3D.gameObject.SetActive(false);
            Clear(cardArea);
            Label(cardArea, "Shortcut: pay two random setbacks.", 0, 0, 330, 50, 20, Ink);
            Button(cardArea, yes, 0, 55, 330, 48, Gold, () => choice = 0);
            Button(cardArea, no, 0, 110, 330, 48, Mint, () => choice = 1);
        }
        void Refresh()
        {
            humanStatus.text = players[0].Name + " / YOU     " + players[0].Position + "/" + GameRules.Finish;
            botStatus.text = "MOSS / CPU     " + players[1].Position + "/" + GameRules.Finish;
        }
        void RenderHand(int layoutCount = 0)
        {
            discardPrompt.gameObject.SetActive(selectingDiscard);
            foreach (Transform child in handArea) child.gameObject.SetActive(false);
            Clear(handArea);
            var hand = players[0].Hand;
            int count = layoutCount > 0 ? layoutCount : hand.Count;
            for (int i = 0; i < hand.Count; i++)
            {
                int slot = i; var mutation = hand[i];
                var card = MutationCard(handArea, mutation, HandX(i, count), HandY(i, count), HandWidth, HandWidth * 1.386f);
                card.name = "Hand card " + i;
                card.GetComponent<RawImage>().raycastTarget = true;
                if (selectingDiscard) card.gameObject.AddComponent<HandCardHover>();
                var button = card.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => {
                    if (paused || overlay || finished || handBusy) return;
                    if (selectingDiscard) { if (choice < 0) choice = slot; return; }
                    // Two held cards are display-only; clicks have no action outside discard selection.
                });
            }
        }
        void Note(string message)
        {
            history.Insert(0, message); if (history.Count > 4) history.RemoveAt(4);
        }

        void Win(int index)
        {
            finished = true; CompletedGames++; rollButton.interactable = false;
            turnLabel.text = "TRAIL COMPLETE"; status.text = players[index].Name + " reached the finish!";
            for (int i = 0; i < 100; i++) confetti.Add(Panel(root, random.Next(1600), -random.Next(1000), 7 + random.Next(7), 12 + random.Next(9), new[] { Mint, Gold, Coral, Paper }[i % 4]).rectTransform);
            var panel = Panel(root, 440, 290, 720, 380, Paper).transform;
            Label(panel, "A NEW CHAPTER IN EVOLUTION", 30, 35, 660, 35, 18, Ink, TextAnchor.MiddleCenter, true);
            Label(panel, index == 0 ? "You survived the trail!" : "Moss wins this time!", 30, 110, 660, 80, 44, Ink, TextAnchor.MiddleCenter, true);
            Label(panel, "The first creature to the finish wins.\nEvery mutation tells a different story.", 30, 200, 660, 70, 23, Ink, TextAnchor.MiddleCenter);
            Button(panel, "PLAY AGAIN", 65, 300, 275, 56, Gold, () => SceneManager.LoadScene("GameBoardScene"));
            Button(panel, "HOME", 380, 300, 275, 56, Mint, Home);
        }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ToggleMenu();
            for (int i = 0; i < leaves.Count; i++) if (leaves[i]) leaves[i].localEulerAngles = new Vector3(0, 0, Mathf.Sin(Time.time * .35f + i) * 35);
            for (int i = 0; i < confetti.Count; i++)
            {
                var r = confetti[i]; var p = r.anchoredPosition; p.y -= Time.deltaTime * (110 + i % 7 * 25); p.x += Mathf.Sin(Time.time * 2 + i) * Time.deltaTime * 35;
                if (p.y < -1020) p.y = 30; r.anchoredPosition = p; r.Rotate(0, 0, Time.deltaTime * (30 + i));
            }
        }
        RectTransform Modal(float width, float height)
        {
            Pause();
            overlay = Rect("Modal", root, 0, 0, 1600, 1000);
            var shade = Panel(overlay, -2000, -2000, 5600, 5000, new Color(0, .06f, .05f, .86f)); shade.raycastTarget = true;
            return Panel(overlay, (1600 - width) / 2, (1000 - height) / 2, width, height, Paper).rectTransform;
        }
        void Pause() { if (!paused) { priorScale = Time.timeScale; paused = true; Time.timeScale = 0; } }
        void CloseModal() { if (overlay) Destroy(overlay.gameObject); overlay = null; paused = false; Time.timeScale = priorScale; }
        void ToggleMenu()
        {
            if (overlay) { CloseModal(); return; }
            if (SceneManager.GetActiveScene().name == "StartupScene") return;
            var p = Modal(620, 410);
            Label(p, "TAKE A BREATHER", 35, 45, 550, 65, 35, Ink, TextAnchor.MiddleCenter, true);
            Label(p, "The forest will wait. Your game is paused.", 35, 120, 550, 45, 22, Ink, TextAnchor.MiddleCenter);
            Button(p, "CONTINUE", 60, 200, 500, 62, Gold, CloseModal);
            Button(p, "RETURN TO HOME", 60, 285, 500, 62, Mint, Home);
        }
        void Home() { Time.timeScale = 1; SceneManager.LoadScene("StartupScene"); }
        void ShowRules()
        {
            if (overlay) CloseModal();
            var p = Modal(1000, 790);
            Label(p, "FIELD GUIDE", 45, 35, 900, 60, 43, Ink, bold: true);
            Label(p, "01   Each creature starts with two mutations. Take turns rolling a six-sided die.\n\n02   Move forward by the roll. A roll of 2 also draws a scenario.\n       Every normal scenario affects BOTH creatures; both held mutations apply.\n\n03   After both turns, draw a third mutation, then discard one and keep two.\n       Then draw one shared scenario before the next round.\n\n04   Never move behind START. Reach or pass FINISH to win immediately.\n\n05   The optional bridge at 18 jumps to 31 and ends your movement.\n       Pay two random setbacks yourself, without mutation protection.\n\n06   Bright Colors is vulnerable to Eagle eyes. Large Beak protects you from\n       HARD SEEDS. Discards reshuffle when a deck runs out.", 45, 118, 910, 490, 23, Ink);
            Label(p, "Original card wording is preserved, including spelling.\nYou can choose the first player on the startup screen. ESC pauses the game.", 45, 625, 900, 60, 19, Hex("527866"));
            Button(p, "BACK TO THE TRAIL", 300, 710, 400, 55, Gold, CloseModal);
        }
        void OnDestroy() { Time.timeScale = 1; }
    }
}
