using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Dialogue
{
    public class DialogueController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueAnchorFollower follower;
        [SerializeField] private DialogueBubbleView bubblePrefab;

        [Header("Stack Settings")]
        [SerializeField] private int maxLines = 4;
        [SerializeField] private float lineHeight = 42f; // in UI units (tune to your prefab)
        [SerializeField] private float pushAnimDuration = 0.18f;

        private readonly List<DialogueBubbleView> _active = new();
        private Coroutine _repositionRoutine;

        private RectTransform _bubbleAnchor;

        void Awake()
        {
            if (follower == null) follower = GetComponent<DialogueAnchorFollower>();
            follower.EnsureAnchor();
            _bubbleAnchor = follower.AnchorRect;
        }

        public void InitializeIfNeeded(DialogueBubbleView prefab)
        {
            if (bubblePrefab == null) bubblePrefab = prefab;
        }
        
        private void PruneDead()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] == null) _active.RemoveAt(i);
            }
        }

        public void Show(string text, float duration)
        {
            PruneDead();

            var bubble = Instantiate(bubblePrefab, _bubbleAnchor);
            bubble.BindOwner(this);               // important, see below
            bubble.RectTransform.anchoredPosition = Vector2.zero;

            _active.Insert(0, bubble);

            while (_active.Count > maxLines)
            {
                var last = _active[_active.Count - 1];
                _active.RemoveAt(_active.Count - 1);
                last.ForceKill(); // ok, controller already removed it
            }

            bubble.Play(text, duration);

            if (_repositionRoutine != null) StopCoroutine(_repositionRoutine);
            _repositionRoutine = StartCoroutine(AnimateReposition());
        }

        internal void NotifyBubbleExpired(DialogueBubbleView bubble)
        {
            int idx = _active.IndexOf(bubble);
            if (idx < 0) return;

            _active.RemoveAt(idx);
            if (_repositionRoutine != null) StopCoroutine(_repositionRoutine);
            _repositionRoutine = StartCoroutine(AnimateReposition());
        }

        private IEnumerator AnimateReposition()
        {
            // Snapshot start/end positions
            var starts = new Vector2[_active.Count];
            var ends = new Vector2[_active.Count];

            for (int i = 0; i < _active.Count; i++)
            {
                var rt = _active[i].RectTransform;
                starts[i] = rt.anchoredPosition;
                ends[i] = new Vector2(0f, i * lineHeight);
            }

            float t = 0f;
            while (t < pushAnimDuration)
            {
                t += Time.deltaTime;
                float a = pushAnimDuration <= 0f ? 1f : Mathf.Clamp01(t / pushAnimDuration);
                // simple ease out
                float eased = 1f - Mathf.Pow(1f - a, 3f);

                for (int i = 0; i < _active.Count; i++)
                {
                    if (_active[i] == null) continue;
                    _active[i].RectTransform.anchoredPosition = Vector2.Lerp(starts[i], ends[i], eased);
                }

                yield return null;
            }

            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] == null) continue;
                _active[i].RectTransform.anchoredPosition = ends[i];
            }

            _repositionRoutine = null;
        }
    }
}