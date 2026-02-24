using UnityEngine;

namespace Features.Dialogue
{
    public class DialogueAnchorFollower : MonoBehaviour
    {
        [Header("World target")]
        [SerializeField] private Transform worldTarget;   // e.g. head / bubble anchor in world
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.2f, 0f);

        [Header("UI")]
        [SerializeField] private RectTransform anchorsRoot; // NPCDialogueAnchors under MainCanvas
        [SerializeField] private Camera uiCamera;           // only needed for ScreenSpace-Camera
        [SerializeField] private Camera worldCamera;        // camera rendering the world

        public RectTransform AnchorRect { get; private set; }

        private void Reset()
        {
            worldTarget = transform;
        }

        private void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;

            // If you use Screen Space - Overlay, uiCamera can stay null.
            // If you use Screen Space - Camera, set uiCamera to that canvas camera.
        }

        public void EnsureAnchor()
        {
            if (AnchorRect != null) return;
            if (anchorsRoot == null)
            {
                Debug.LogError("DialogueAnchorFollower: anchorsRoot not assigned.");
                return;
            }

            var go = new GameObject($"{name}_DialogueAnchor", typeof(RectTransform));
            AnchorRect = go.GetComponent<RectTransform>();
            AnchorRect.SetParent(anchorsRoot, false);
            AnchorRect.anchorMin = AnchorRect.anchorMax = new Vector2(0.5f, 0.5f);
            AnchorRect.pivot = new Vector2(0.5f, 0f);
        }

        private void LateUpdate()
        {
            if (AnchorRect == null) return;
            if (worldTarget == null || worldCamera == null) return;

            Vector3 worldPos = worldTarget.position + worldOffset;
            Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

            // Behind camera -> hide anchor
            if (screenPos.z <= 0.01f)
            {
                AnchorRect.gameObject.SetActive(false);
                return;
            }
            AnchorRect.gameObject.SetActive(true);

            // Convert screen point to anchored position
            Canvas canvas = anchorsRoot.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas.transform as RectTransform;

            if (canvasRect == null)
            {
                Debug.LogError("DialogueAnchorFollower: canvasRect not assigned.");
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, uiCamera, out Vector2 localPoint))
            {
                AnchorRect.anchoredPosition = localPoint;
            }
        }

        private void OnDestroy()
        {
            if (AnchorRect != null)
                Destroy(AnchorRect.gameObject);
        }
    }
}