using UnityEngine;

namespace Features.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [SerializeField] private DialogueBubbleView bubblePrefab;

        private void Awake()
        {
            Instance = this;
        }

        public void Say(DialogueController controller, string text, float duration)
        {
            if (controller == null) return;
            controller.InitializeIfNeeded(bubblePrefab);
            controller.Show(text, duration);
        }
    }
}