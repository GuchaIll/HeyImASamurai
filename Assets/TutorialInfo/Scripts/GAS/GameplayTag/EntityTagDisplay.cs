using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Displays an overhead UI panel showing all gameplay tags on an entity.
/// Attach this to any entity with a GameplayTagComponent.
/// </summary>
public class EntityTagDisplay : MonoBehaviour
{
    [Header("Tag Source")]
    [Tooltip("The tag component to display. Auto-finds if null.")]
    [SerializeField] private GameplayTagComponent tagComponent;
    
    [Header("Display Settings")]
    [Tooltip("Offset above the entity")]
    [SerializeField] private Vector3 displayOffset = new Vector3(0, 2f, 0);
    
    [Tooltip("Scale of the UI canvas")]
    [SerializeField] private float canvasScale = 0.01f;
    
    [Tooltip("Background color for the panel")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
    
    [Tooltip("Text color for tags")]
    [SerializeField] private Color textColor = Color.white;
    
    [Tooltip("Font size for tag text")]
    [SerializeField] private float fontSize = 14f;
    
    [Tooltip("Padding inside the panel")]
    [SerializeField] private float padding = 8f;
    
    [Tooltip("Space between tag entries")]
    [SerializeField] private float tagSpacing = 4f;
    
    [Tooltip("Maximum width of the panel")]
    [SerializeField] private float maxWidth = 300f;
    
    [Header("Behavior")]
    [Tooltip("Always face the camera")]
    [SerializeField] private bool billboard = true;
    
    [Tooltip("Update frequency in seconds (0 = every frame)")]
    [SerializeField] private float updateInterval = 0.1f;
    
    [Tooltip("Hide when no tags are present")]
    [SerializeField] private bool hideWhenEmpty = true;
    
    [Tooltip("Show only in play mode (for debugging)")]
    [SerializeField] private bool debugModeOnly = false;
    
    // UI Components
    private Canvas canvas;
    private RectTransform canvasRect;
    private Image backgroundImage;
    private VerticalLayoutGroup layoutGroup;
    private ContentSizeFitter sizeFitter;
    private RectTransform contentRect;
    
    // Tag text components pool
    private List<TextMeshProUGUI> tagTexts = new List<TextMeshProUGUI>();
    private List<int> lastTagIds = new List<int>();
    
    private float updateTimer;
    private Camera mainCamera;
    
    private void Awake()
    {
        if (tagComponent == null)
            tagComponent = GetComponent<GameplayTagComponent>();
        
        mainCamera = Camera.main;
        CreateUI();
    }
    
    private void Start()
    {
        RefreshDisplay();
    }
    
    private void Update()
    {
        if (debugModeOnly && !Application.isPlaying)
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
            return;
        }
        
        // Update position
        UpdatePosition();
        
        // Billboard to face camera
        if (billboard && mainCamera != null && canvas != null)
        {
            canvas.transform.rotation = mainCamera.transform.rotation;
        }
        
        // Periodic refresh
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            RefreshDisplay();
        }
    }
    
    private void OnEnable()
    {
        RefreshDisplay();
    }
    
    private void CreateUI()
    {
        // Create Canvas
        var canvasGO = new GameObject("TagDisplayCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = displayOffset;
        canvasGO.transform.localScale = Vector3.one * canvasScale;
        
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        
        canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(maxWidth, 100);
        
        // Add CanvasScaler for consistent sizing
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        
        // Add GraphicRaycaster (required for UI)
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create Background Panel
        var panelGO = new GameObject("Background");
        panelGO.transform.SetParent(canvasGO.transform, false);
        
        contentRect = panelGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0);
        contentRect.anchorMax = new Vector2(0.5f, 0);
        contentRect.pivot = new Vector2(0.5f, 0);
        contentRect.anchoredPosition = Vector2.zero;
        
        backgroundImage = panelGO.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = false;
        
        // Add rounded corners if available (Unity 2021.2+)
        // backgroundImage.pixelsPerUnitMultiplier = 1;
        
        // Add Layout Group
        layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
        layoutGroup.spacing = tagSpacing;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        
        // Add Content Size Fitter
        sizeFitter = panelGO.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
    
    private void UpdatePosition()
    {
        if (canvas != null)
        {
            canvas.transform.position = transform.position + displayOffset;
        }
    }
    
    /// <summary>
    /// Refreshes the tag display. Call this after modifying tags.
    /// </summary>
    public void RefreshDisplay()
    {
        if (tagComponent == null || tagComponent.Database == null)
        {
            SetVisible(false);
            return;
        }
        
        var container = tagComponent.TagContainer;
        var tagIds = container.Tags;
        
        // Check if tags changed
        if (TagsUnchanged(tagIds))
            return;
        
        // Cache current tags
        lastTagIds.Clear();
        lastTagIds.AddRange(tagIds);
        
        // Hide if empty
        if (tagIds.Count == 0 && hideWhenEmpty)
        {
            SetVisible(false);
            return;
        }
        
        SetVisible(true);
        
        // Ensure we have enough text components
        while (tagTexts.Count < tagIds.Count)
        {
            CreateTagText();
        }
        
        // Update text components
        for (int i = 0; i < tagIds.Count; i++)
        {
            var tagName = tagComponent.Database.GetTagName(tagIds[i]);
            tagTexts[i].text = FormatTagName(tagName);
            tagTexts[i].gameObject.SetActive(true);
        }
        
        // Hide unused text components
        for (int i = tagIds.Count; i < tagTexts.Count; i++)
        {
            tagTexts[i].gameObject.SetActive(false);
        }
        
        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }
    
    private bool TagsUnchanged(IReadOnlyList<int> current)
    {
        if (current.Count != lastTagIds.Count)
            return false;
        
        for (int i = 0; i < current.Count; i++)
        {
            if (current[i] != lastTagIds[i])
                return false;
        }
        return true;
    }
    
    private void CreateTagText()
    {
        var textGO = new GameObject($"Tag_{tagTexts.Count}");
        textGO.transform.SetParent(contentRect, false);
        
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        
        // Add background for individual tag
        var bgGO = new GameObject("TagBg");
        bgGO.transform.SetParent(textGO.transform, false);
        bgGO.transform.SetAsFirstSibling();
        
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(4, 2);
        bgRect.anchoredPosition = Vector2.zero;
        
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        bgImage.raycastTarget = false;
        
        tagTexts.Add(text);
    }
    
    private string FormatTagName(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
            return "Unknown";
        
        // Option: Show only the last part of hierarchical tags
        // return tagName.Contains(".") ? tagName.Substring(tagName.LastIndexOf('.') + 1) : tagName;
        
        // Show full tag with periods replaced by arrows
        return tagName.Replace(".", " → ");
    }
    
    private void SetVisible(bool visible)
    {
        if (canvas != null)
            canvas.gameObject.SetActive(visible);
    }
    
    /// <summary>
    /// Force immediate refresh of the display
    /// </summary>
    public void ForceRefresh()
    {
        lastTagIds.Clear();
        RefreshDisplay();
    }
    
    /// <summary>
    /// Set the display offset at runtime
    /// </summary>
    public void SetOffset(Vector3 offset)
    {
        displayOffset = offset;
        UpdatePosition();
    }
    
    /// <summary>
    /// Show/hide the display
    /// </summary>
    public void SetDisplayEnabled(bool enabled)
    {
        this.enabled = enabled;
        SetVisible(enabled);
    }
    
    private void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;
        
        foreach (var text in tagTexts)
        {
            if (text != null)
            {
                text.fontSize = fontSize;
                text.color = textColor;
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw where the display will appear
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + displayOffset, 0.1f);
        Gizmos.DrawLine(transform.position, transform.position + displayOffset);
    }
    #endif
}