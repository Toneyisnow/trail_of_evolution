using UnityEngine;

namespace TrailOfMutation
{
    // Small code-native silhouettes, matching the existing procedural UI assets.
    public static class CreatureSilhouettes
    {
        static readonly Sprite[] cache = new Sprite[3];
        // Coordinates are on a 160 x 118 canvas, with the origin at the top left.
        static readonly Vector2[][] contours = {
            Points(24,52, 37,44, 85,47, 99,31, 104,21, 100,10, 93,6, 95,3, 103,7, 105,1, 108,2, 108,14, 113,20, 118,12, 117,2, 120,1, 122,9, 129,5, 131,8, 122,16, 120,24, 132,28, 143,37, 140,42, 125,40, 116,47, 107,65, 99,73, 104,105, 98,108, 91,78, 85,77, 83,107, 77,107, 77,75, 49,73, 43,85, 41,108, 34,108, 35,82, 31,75, 25,88, 20,107, 13,107, 19,82, 25,65, 17,50, 16,43),
            Points(15,82, 5,66, 3,45, 14,52, 23,69, 39,75, 48,61, 82,56, 101,40, 106,17, 118,32, 127,19, 130,44, 151,54, 141,63, 127,63, 114,77, 111,104, 103,104, 101,80, 93,79, 87,103, 78,103, 82,80, 64,81, 56,93, 56,105, 47,105, 47,86, 36,90, 24,88),
            Points(29,49, 38,36, 54,27, 75,24, 94,29, 112,43, 119,53, 133,44, 146,47, 153,56, 149,65, 135,68, 119,64, 105,73, 118,90, 112,98, 95,82, 89,78, 58,79, 44,96, 33,94, 40,77, 26,67, 12,71, 5,66, 23,59)
        };
        static Vector2[] Points(params float[] values)
        {
            var points = new Vector2[values.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector2(values[i * 2], values[i * 2 + 1]);
            return points;
        }
        public static Sprite Get(int index)
        {
            if (cache[index]) return cache[index];
            const int scale = 3, width = 160 * scale, height = 118 * scale;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = new[] { "Fern deer silhouette", "Ember fox silhouette", "Tidal turtle silhouette" }[index];
            var pixels = new Color32[width * height];
            var polygon = contours[index];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float px = (x + .5f) / scale, py = (y + .5f) / scale;
                bool inside = false;
                for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                {
                    var a = polygon[i]; var b = polygon[j];
                    if ((a.y > py) != (b.y > py) && px < (b.x - a.x) * (py - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                pixels[(height - 1 - y) * width + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
            }
            texture.SetPixels32(pixels); texture.Apply();
            cache[index] = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * .5f, 100);
            return cache[index];
        }
    }
}
