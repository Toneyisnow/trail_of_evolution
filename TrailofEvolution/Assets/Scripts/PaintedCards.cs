using UnityEngine;
using UnityEngine.UI;
using static TrailOfMutation.TrailUI;

namespace TrailOfMutation
{
    public static class PaintedCards
    {
        public static readonly string[] Traits = {
            "Brilliant plumage attracts attention. Beware of eagle eyes.",
            "Blend into the forest. Stay hidden from hunting hawks.",
            "Run like the wind. Catch even the fastest prey.",
            "A strong shell protects you when the earth shakes.",
            "Reach the leaves high above the dry forest floor.",
            "Stand your ground against new predators.",
            "Crack hard seeds when the soft fruit is gone."
        };
        public static RectTransform Back(Transform parent, float x, float y, float w, float h, int cell)
        {
            var r = Rect("Painted card " + cell, parent, x, y, w, h);
            var art = r.gameObject.AddComponent<RawImage>();
            art.texture = Resources.Load<Texture2D>("Board/CardAtlas");
            art.uvRect = new Rect(cell % 4 * .25f, (3 - cell / 4) * .25f, .25f, .25f);
            art.raycastTarget = false;
            var shadow = r.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.05f, .025f, .01f, .5f);
            shadow.effectDistance = new Vector2(4, -5);
            return r;
        }
        public static RectTransform Face(Transform parent, int cell, float x, float y, float w, float h, string title, string body)
        {
            var r = Back(parent, x, y, w, h, cell);
            var heading = Label(r, title, w * .20f, h * .058f, w * .70f, h * .105f,
                Mathf.RoundToInt(w * .07f), Hex("302718"), TextAnchor.MiddleCenter, true);
            var copy = Label(r, body, w * .105f, h * .66f, w * .79f, h * .24f,
                Mathf.RoundToInt(w * .055f), Hex("302718"), TextAnchor.MiddleCenter, true);
            var font = Resources.Load<Font>("Fonts/CardSerif");
            heading.font = copy.font = font;
            heading.resizeTextForBestFit = copy.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 10; heading.resizeTextMaxSize = Mathf.RoundToInt(w * .075f);
            copy.resizeTextMaxSize = Mathf.RoundToInt(w * .055f);
            copy.resizeTextMinSize = Mathf.Min(10, copy.resizeTextMaxSize);
            return r;
        }
    }
}
