using UnityEngine;

namespace TrailOfMutation
{
    // Keeps the right, top and bottom margins equal even outside the reference aspect ratio.
    public sealed class ScreenEdgeDock : MonoBehaviour
    {
        public RectTransform Side, Hand, Prompt;
        readonly Vector3[] corners = new Vector3[4];
        public void Layout()
        {
            var rect = (RectTransform)transform;
            var canvas = GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            rect.GetWorldCorners(corners);
            Vector2 left = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            Vector2 right = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            float scale = (right.x - left.x) / rect.rect.width;
            if (scale <= 0) return;
            float padding = 35 * scale;
            rect.anchoredPosition += new Vector2(Screen.width - padding - right.x, Screen.height - padding - right.y) / scale;
            rect.sizeDelta = new Vector2(364, Screen.height / scale - 70);
            Side.sizeDelta = new Vector2(364, rect.sizeDelta.y - 85);
            Hand.anchoredPosition = new Vector2(17, -(Side.rect.height - 225));
            Prompt.anchoredPosition = new Vector2(17, -(Side.rect.height - 365));
        }
        void LateUpdate() { Layout(); }
    }
}
