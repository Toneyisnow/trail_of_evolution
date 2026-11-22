using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrailOfMutation
{
    // Use the lettering's alpha, rather than outlining its rectangular image bounds.
    public sealed class StartHoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        GameObject halo;
        Material material;
        Button button;

        public void Initialize(RawImage lettering)
        {
            button = GetComponent<Button>();
            material = new Material(Resources.Load<Shader>("Startup/WhiteSilhouette"));
            var container = TrailUI.Rect("White hover shadow", transform, 0, 0, 440, 150);
            container.SetSiblingIndex(lettering.transform.GetSiblingIndex());
            halo = container.gameObject;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                var rect = TrailUI.Rect("Shadow", container,
                    Mathf.Cos(angle) * 3, Mathf.Sin(angle) * 3, 440, 150);
                var shadow = rect.gameObject.AddComponent<RawImage>();
                shadow.texture = lettering.texture;
                shadow.material = material;
                shadow.color = Color.white;
                shadow.raycastTarget = false;
            }
            halo.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (halo && button.IsInteractable()) halo.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData) => Hide();
        public void Hide() { if (halo) halo.SetActive(false); }
        void OnDisable() => Hide();
        void OnDestroy() { if (material) Destroy(material); }
    }
}
