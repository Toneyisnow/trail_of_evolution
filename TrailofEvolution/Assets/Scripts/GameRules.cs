using System;
using System.Collections.Generic;

namespace TrailOfMutation
{
    public enum ControllerKind { Human, Computer }
    public enum Mutation { BrightColors, Camouflage, Speed, HardShell, LongNeck, SharpClaws, LargeBeak }

    [Serializable]
    public sealed class PlayerCharacter
    {
        public string Name;
        public ControllerKind Controller;
        public int Position;
        public readonly List<Mutation> Hand = new List<Mutation>(3);
        public PlayerCharacter(string name, ControllerKind controller) { Name = name; Controller = controller; }
    }

    public sealed class ScenarioCard
    {
        public readonly string Title, Description, Instruction;
        public readonly int Penalty;
        public readonly Mutation Trait;
        public readonly bool HarmfulTrait;
        public ScenarioCard(string title, string description, string instruction, int penalty, Mutation trait, bool harmful = false)
        { Title = title; Description = description; Instruction = instruction; Penalty = penalty; Trait = trait; HarmfulTrait = harmful; }
        public int Movement(Mutation mutation) => (HarmfulTrait ? mutation == Trait : mutation != Trait) ? -Penalty : 0;
        public int Movement(IReadOnlyList<Mutation> hand)
        {
            bool contains = false;
            for (int i = 0; i < hand.Count; i++) contains |= hand[i] == Trait;
            return (HarmfulTrait ? contains : !contains) ? -Penalty : 0;
        }
    }

    // Pure rules: no scene, animation, frame-rate or Unity dependencies.
    public static class GameRules
    {
        public const int Finish = 44;
        public const int ShortcutEntry = 18;
        public const int ShortcutExit = 31;
        public static readonly string[] MutationNames = { "Bright Colors", "Camouflage", "Speed", "Hard Shell", "Long Neck", "Sharp Claws", "Large Beak" };
        // The photograph contains eight physical cards, including two Hard Shell cards.
        public static readonly Mutation[] MutationDeck = { Mutation.BrightColors, Mutation.Camouflage, Mutation.Speed, Mutation.HardShell, Mutation.LongNeck, Mutation.SharpClaws, Mutation.LargeBeak, Mutation.HardShell };
        public static readonly ScenarioCard[] Scenarios = {
            new ScenarioCard("SPEED", "Your only pray are very fast bunnies", "Move back 4 spaces UNLESS you are fast", 4, Mutation.Speed),
            new ScenarioCard("EARTHQUAKE", "Your area is prone to earthquakes and they kill lots of members of the community", "Move back 2 spaces UNLESS you have a hard shell", 2, Mutation.HardShell),
            new ScenarioCard("DROUGHT", "All of the leave on the ground are dead. The only ones are high up", "Move back 3 spaces UNLESS you have a long neck", 3, Mutation.LongNeck),
            new ScenarioCard("Eagle eyes", "birds circle overhead, looking for something to eat", "If you have bright colors, move back 6 spaces", 6, Mutation.BrightColors, true),
            new ScenarioCard("", "Deadly hawks are hunting you", "Move back 1 space UNLESS you are camaflauged", 1, Mutation.Camouflage),
            new ScenarioCard("PREDATORS", "Some new animals are in your space", "Move back 5 spaces UNLESS you have a claws", 5, Mutation.SharpClaws),
            new ScenarioCard("HARD SEEDS", "The soft fruits are gone. Only hard seeds remain.", "Move back 3 spaces UNLESS you have a large beak", 3, Mutation.LargeBeak)
        };
        public static int Move(int position, int delta) => Math.Max(0, Math.Min(Finish, position + delta));
        public static bool Won(PlayerCharacter p) => p.Position >= Finish;
        public static int MutationRisk(Mutation mutation)
        { int risk = 0; foreach (var card in Scenarios) risk -= card.Movement(mutation); return risk; }
        public static bool ShouldReplace(Mutation current, Mutation offered) => MutationRisk(offered) < MutationRisk(current);
        public static int ChooseDiscard(IReadOnlyList<Mutation> hand)
        {
            int best = 0, bestRisk = int.MaxValue;
            for (int discard = 0; discard < hand.Count; discard++)
            {
                var remaining = new List<Mutation>();
                for (int i = 0; i < hand.Count; i++) if (i != discard) remaining.Add(hand[i]);
                int risk = 0; foreach (var scenario in Scenarios) risk -= scenario.Movement(remaining);
                if (risk < bestRisk) { bestRisk = risk; best = discard; }
            }
            return best;
        }
    }

    // Discards re-enter only when the draw pile is exhausted; held mutations stay out.
    public sealed class CardDeck<T>
    {
        readonly List<T> draw = new List<T>();
        readonly List<T> discard = new List<T>();
        readonly Random random;
        public int DrawCount => draw.Count;
        public int DiscardCount => discard.Count;
        public CardDeck(IEnumerable<T> cards, Random random) { this.random = random; draw.AddRange(cards); Shuffle(); }
        void Shuffle() { for (int i = draw.Count - 1; i > 0; i--) { int j = random.Next(i + 1); T t = draw[i]; draw[i] = draw[j]; draw[j] = t; } }
        public T Draw()
        {
            if (draw.Count == 0) { draw.AddRange(discard); discard.Clear(); Shuffle(); }
            if (draw.Count == 0) throw new InvalidOperationException("No cards available.");
            int index = draw.Count - 1; T card = draw[index]; draw.RemoveAt(index); return card;
        }
        public void Discard(T card) => discard.Add(card);
    }
}
