using UnityEngine;
using UnityEngine.UI;

namespace TrailOfMutation
{
    public static class TrailUI
    {
        public static readonly Color Ink = Hex("173B36"), Paper = Hex("F4EFDF"), Gold = Hex("EABB69"), Mint = Hex("9AD0AE"), Coral = Hex("EF936F");
        static Font font;
        static Sprite round, circle;
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public static Sprite Shape(bool circular)
        {
            if (circular && circle != null) return circle;
            if (!circular && round != null) return round;
            const int size = 64; var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = circular ? "Circle" : "Rounded panel";
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - (circular ? 0 : 20), 0);
                float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - (circular ? 0 : 20), 0);
                float alpha = Mathf.Clamp01((circular ? 31.5f : 11.5f) - Mathf.Sqrt(dx * dx + dy * dy));
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, circular ? Vector4.zero : Vector4.one * 15);
            if (circular) circle = sprite; else round = sprite;
            return sprite;
        }
        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        public static Image Panel(Transform parent, float x, float y, float w, float h, Color color, bool circular = false)
        {
            var r = Rect("Panel", parent, x, y, w, h); var i = r.gameObject.AddComponent<Image>();
            i.sprite = Shape(circular); i.type = circular ? Image.Type.Simple : Image.Type.Sliced; i.color = color; i.raycastTarget = false; return i;
        }
        public static Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.UpperLeft, bool bold = false)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var t = Rect(text, parent, x, y, w, h).gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.supportRichText = true; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; return t;
        }
        public static Button Button(Transform parent, string text, float x, float y, float w, float h, Color color, UnityEngine.Events.UnityAction action)
        {
            var panel = Panel(parent, x, y, w, h, color); panel.raycastTarget = true;
            var b = panel.gameObject.AddComponent<Button>(); b.targetGraphic = panel; b.onClick.AddListener(action);
            var colors = b.colors; colors.highlightedColor = new Color(.88f, .94f, .87f); colors.pressedColor = new Color(.7f, .83f, .72f); colors.disabledColor = new Color(.5f, .5f, .5f, .65f); b.colors = colors;
            Label(panel.transform, text, 8, 4, w - 16, h - 8, 20, Ink, TextAnchor.MiddleCenter, true); return b;
        }
        public static void Line(Transform parent, Vector2 a, Vector2 b, float width, Color c)
        {
            var r = Panel(parent, a.x, a.y, Vector2.Distance(a, b), width, c).rectTransform;
            r.pivot = new Vector2(0, .5f); r.localEulerAngles = new Vector3(0, 0, -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        }
        public static void Tree(Transform parent, float x, float y, float scale = 1)
        {
            Panel(parent, x + 13 * scale, y + 20 * scale, 5 * scale, 30 * scale, Hex("997653"));
            Panel(parent, x, y, 32 * scale, 38 * scale, Hex("387564"), true);
            Panel(parent, x + 4 * scale, y - 8 * scale, 23 * scale, 31 * scale, Hex("4A8B70"), true);
        }
        public static Color MutationColor(Mutation m) => new[] { Hex("B63F61"), Hex("267456"), Hex("A16B14"), Hex("565478"), Hex("7A6E19"), Hex("8A4BA2"), Hex("287F9D") }[(int)m];
        public static void Clear(Transform transform) { foreach (Transform child in transform) Object.Destroy(child.gameObject); }
    }
}
