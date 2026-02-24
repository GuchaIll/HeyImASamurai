using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Dialogue
{
    public class DialogueBubbleView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform root;
        [SerializeField] private RectTransform maskRect;
        [SerializeField] private TextMeshProUGUI textLabel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Typing")]
        [Tooltip("Seconds to fully reveal. If 0, reveal instantly.")]
        [SerializeField] private float typingDuration = 0.35f;

        [Header("Fade")]
        [SerializeField] private float fadeIn = 0.08f;
        [SerializeField] private float fadeOut = 0.10f;

        private Coroutine _routine;
        private DialogueController _owner;

        public RectTransform RectTransform => root != null ? root : (RectTransform)transform;

        public void BindOwner(DialogueController owner) => _owner = owner;
        
        private void Awake()
        {
            if (root == null) root = (RectTransform)transform;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }
        

        public void Play(string text, float displayDuration)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Run(text, displayDuration));
        }

        public void ForceKill()
        {
            if (_routine != null) StopCoroutine(_routine);
            _owner?.NotifyBubbleExpired(this);
            Destroy(gameObject);
        }

        private IEnumerator Run(string text, float displayDuration)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            // Set full text first (prevents layout jumping)
            textLabel.text = text;
            textLabel.ForceMeshUpdate();

            // Make mask start at zero width, but keep height
            var fullSize = maskRect.sizeDelta;

            // We need a target width that fully fits the rendered text area.
            // A simple safe option: use the text rect width (assuming bubble fixed width).
            float targetWidth = fullSize.x;
            maskRect.sizeDelta = new Vector2(0f, fullSize.y);

            // Fade in quickly
            if (canvasGroup != null && fadeIn > 0f)
            {
                float t = 0f;
                while (t < fadeIn)
                {
                    t += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(t / fadeIn);
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            // Typing reveal by expanding mask
            if (typingDuration > 0f)
            {
                float t = 0f;
                while (t < typingDuration)
                {
                    t += Time.deltaTime;
                    float a = Mathf.Clamp01(t / typingDuration);
                    // gentle ease
                    float eased = a * a * (3f - 2f * a);
                    maskRect.sizeDelta = new Vector2(Mathf.Lerp(0f, targetWidth, eased), fullSize.y);
                    yield return null;
                }
            }
            maskRect.sizeDelta = new Vector2(targetWidth, fullSize.y);

            // Stay on screen for duration (counts after fully revealed; change if you want inclusive)
            float remaining = Mathf.Max(0f, displayDuration);
            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }

            // Fade out then die
            if (canvasGroup is not null && fadeOut > 0f)
            {
                float t = 0f;
                float start = canvasGroup.alpha;
                while (t < fadeOut)
                {
                    t += Time.deltaTime;
                    canvasGroup.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / fadeOut));
                    yield return null;
                }
            }

            _owner?.NotifyBubbleExpired(this);
            Destroy(gameObject);
        }
    }
}