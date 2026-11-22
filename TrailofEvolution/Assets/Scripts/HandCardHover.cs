using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrailOfMutation
{
    public sealed class HandCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        RectTransform rect;
        Vector2 home;
        Outline highlight;
        int order;
        bool hovering;
        public bool IsRaised => hovering;
        void Awake()
        {
            rect = (RectTransform)transform; home = rect.anchoredPosition;
            highlight = gameObject.AddComponent<Outline>();
            highlight.effectColor = TrailUI.Gold; highlight.effectDistance = new Vector2(4, -4);
            highlight.enabled = false;
        }
        public void OnPointerEnter(PointerEventData data)
        {
            if (hovering) return;
            hovering = true; order = transform.GetSiblingIndex(); transform.SetAsLastSibling(); highlight.enabled = true;
        }
        public void OnPointerExit(PointerEventData data)
        {
            hovering = false; transform.SetSiblingIndex(order); highlight.enabled = false;
        }
        void Update()
        {
            float blend = 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime);
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, home + (hovering ? Vector2.up * 32 : Vector2.zero), blend);
            rect.localScale = Vector3.Lerp(rect.localScale, Vector3.one * (hovering ? 1.08f : 1), blend);
        }
    }
}
