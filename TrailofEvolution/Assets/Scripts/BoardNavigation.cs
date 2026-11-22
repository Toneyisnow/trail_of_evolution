using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrailOfMutation
{
    public sealed class BoardNavigation : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler, ICanvasRaycastFilter
    {
        RectTransform surface, movable;
        TabletopBoard tabletop;
        Canvas canvas;
        Vector2 lastPointer;
        float fit = 1;
        int lastWidth, lastHeight;
        public float Zoom { get; private set; } = 1;
        Camera UICamera => canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        public void Initialize(TabletopBoard owner)
        {
            tabletop = owner; surface = (RectTransform)transform;
            movable = (RectTransform)owner.transform; canvas = GetComponentInParent<Canvas>();
            Constrain();
        }
        public Rect MapScreenBounds()
        {
            Rect uv = tabletop.MapViewportBounds;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                var local = new Vector3(Mathf.Lerp(surface.rect.xMin, surface.rect.xMax, i % 2 == 0 ? uv.xMin : uv.xMax),
                    Mathf.Lerp(surface.rect.yMin, surface.rect.yMax, i < 2 ? uv.yMin : uv.yMax), 0);
                Vector2 p = RectTransformUtility.WorldToScreenPoint(UICamera, surface.TransformPoint(local));
                min = Vector2.Min(min, p); max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        public Vector2 VisibleFraction
        {
            get { var b = MapScreenBounds(); return new Vector2(
                Mathf.Max(0, Mathf.Min(Screen.width, b.xMax) - Mathf.Max(0, b.xMin)) / b.width,
                Mathf.Max(0, Mathf.Min(Screen.height, b.yMax) - Mathf.Max(0, b.yMin)) / b.height); }
        }
        public bool IsRaycastLocationValid(Vector2 point, Camera camera) => tabletop && MapScreenBounds().Contains(point);
        public void OnBeginDrag(PointerEventData e) { lastPointer = e.position; }
        public void OnDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || Time.timeScale == 0) return;
            PanScreen(e.position - lastPointer); lastPointer = e.position;
        }
        public void OnScroll(PointerEventData e)
        {
            var module = EventSystem.current.currentInputModule as UnityEngine.InputSystem.UI.InputSystemUIInputModule;
            float unitsPerTick = module ? Mathf.Max(.001f, Mathf.Abs(module.scrollDeltaPerTick)) : 1;
            if (Time.timeScale != 0) SetZoom(Zoom + e.scrollDelta.y / unitsPerTick * .1f, e.position);
        }
        void ShiftScreen(Vector2 delta)
        {
            var parent = (RectTransform)movable.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Vector2.zero, UICamera, out var a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, delta, UICamera, out var b);
            movable.anchoredPosition += b - a;
        }
        public void PanScreen(Vector2 delta) { ShiftScreen(delta); Constrain(); }
        public void SetZoom(float zoom, Vector2 focus)
        {
            float next = Mathf.Clamp(zoom, 1, 2), ratio = next / Zoom;
            var parent = (RectTransform)movable.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, focus, UICamera, out var local);
            movable.localPosition = (Vector3)local + (movable.localPosition - (Vector3)local) * ratio;
            Zoom = next; movable.localScale = Vector3.one * fit * Zoom;
            Constrain();
        }
        public void Constrain()
        {
            Canvas.ForceUpdateCanvases();
            var b = MapScreenBounds();
            // Fit the 100% baseline so even a 200% map can retain 60% on both axes after a window resize.
            float baseW = b.width / (fit * Zoom), baseH = b.height / (fit * Zoom);
            fit = Mathf.Min(1, Screen.width / (1.2f * baseW), Screen.height / (1.2f * baseH));
            movable.localScale = Vector3.one * fit * Zoom;
            b = MapScreenBounds();
            float x = Mathf.Clamp(b.xMin, -.4f * b.width, Screen.width - .6f * b.width);
            float y = Mathf.Clamp(b.yMin, -.4f * b.height, Screen.height - .6f * b.height);
            ShiftScreen(new Vector2(x - b.xMin, y - b.yMin));
            lastWidth = Screen.width; lastHeight = Screen.height;
        }
        void LateUpdate() { if (tabletop && (Screen.width != lastWidth || Screen.height != lastHeight)) Constrain(); }
    }
}
