using UnityEngine;
using UnityEngine.UI;

public class AnxietyHud : MonoBehaviour
{
    public AnxietyFusionManager fusion;
    public string prefabResourceName = "AnxietyHudCanvas";

    [Header("UI Refs")]
    public Text faceText;
    public Slider faceSlider;
    public Text voiceText;
    public Slider voiceSlider;
    public Text fusedText;
    public Slider fusedSlider;

    void Awake()
    {
        if (!fusion) fusion = FindFirstObjectByType<AnxietyFusionManager>();
        TryLoadPrefabOrBuild();
    }

    void Update()
    {
        if (!fusion) return;

        if (faceText) faceText.text = $"Face: {fusion.faceScore1to10:F2} ({fusion.faceDetections} faces)";
        if (faceSlider)
        {
            faceSlider.minValue = 1f;
            faceSlider.maxValue = 10f;
            faceSlider.value = fusion.faceScore1to10;
        }

        if (voiceText) voiceText.text = $"Voice: {fusion.voiceScore1to10:F2}";
        if (voiceSlider)
        {
            voiceSlider.minValue = 1f;
            voiceSlider.maxValue = 10f;
            voiceSlider.value = fusion.voiceScore1to10;
        }

        if (fusedText) fusedText.text = $"Fused: {fusion.fusedScore1to10:F2}";
        if (fusedSlider)
        {
            fusedSlider.minValue = 1f;
            fusedSlider.maxValue = 10f;
            fusedSlider.value = fusion.fusedScore1to10;
        }
    }

    private void TryLoadPrefabOrBuild()
    {
        // Prefer prefab in Resources
        GameObject prefab = Resources.Load<GameObject>(prefabResourceName);
        if (prefab)
        {
            GameObject inst = Instantiate(prefab);
            DontDestroyOnLoad(inst);
            var hud = inst.GetComponentInChildren<AnxietyHud>();
            if (hud)
            {
                // If this instance has references, copy them into newly spawned and destroy this
                hud.fusion = fusion ? fusion : hud.fusion;
                Destroy(gameObject);
                return;
            }
        }

        // Fallback: create Canvas and UI elements at runtime if prefab not found
        Canvas canvas = FindObjectOfType<Canvas>();
        if (!canvas)
        {
            GameObject canvasGo = new GameObject("HUD_Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        RectTransform panel = new GameObject("HUD_Panel").AddComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(12f, -12f);
        panel.sizeDelta = new Vector2(320f, 140f);
        Image panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.4f);

        VerticalLayoutGroup vlg2 = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg2.childForceExpandHeight = false;
        vlg2.childForceExpandWidth = true;
        vlg2.spacing = 6f;
        ContentSizeFitter csf2 = panel.gameObject.AddComponent<ContentSizeFitter>();
        csf2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateRow(panel, "Face", out faceText, out faceSlider);
        CreateRow(panel, "Voice", out voiceText, out voiceSlider);
        CreateRow(panel, "Fused", out fusedText, out fusedSlider);
    }

    private void CreateRow(Transform parent, string label, out Text text, out Slider slider)
    {
        RectTransform row = new GameObject($"Row_{label}").AddComponent<RectTransform>();
        row.SetParent(parent, false);
        HorizontalLayoutGroup hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 8f;
        ContentSizeFitter csf = row.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGo = new GameObject($"Text_{label}");
        textGo.transform.SetParent(row, false);
        text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 16;
        text.color = Color.white;
        text.text = label + ":";
        LayoutElement leText = textGo.AddComponent<LayoutElement>();
        leText.preferredWidth = 120f;

        GameObject sliderGo = new GameObject($"Slider_{label}");
        sliderGo.transform.SetParent(row, false);
        slider = sliderGo.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 1f;
        slider.maxValue = 5f;
        slider.value = 3f;
        LayoutElement leSlider = sliderGo.AddComponent<LayoutElement>();
        leSlider.minWidth = 150f;

        // Background image to make slider visible
        Image bg = sliderGo.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.1f);
        slider.targetGraphic = bg;
        slider.fillRect = null;
        slider.handleRect = null;
    }
}


