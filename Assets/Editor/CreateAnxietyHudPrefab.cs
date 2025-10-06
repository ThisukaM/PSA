using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CreateAnxietyHudPrefab
{
    private const string ResourcesDir = "Assets/Resources";
    private const string PrefabPath = ResourcesDir + "/AnxietyHudCanvas.prefab";

    [MenuItem("Tools/Create Anxiety HUD Prefab")]
    public static void CreatePrefab()
    {
        Directory.CreateDirectory(ResourcesDir);

        GameObject canvasGo = new GameObject("HUD_Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform panel = new GameObject("HUD_Panel").AddComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(12f, -12f);
        panel.sizeDelta = new Vector2(320f, 140f);

        // Subtle background for readability
        Image panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.4f);

        VerticalLayoutGroup vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 6f;
        ContentSizeFitter csfPanel = panel.gameObject.AddComponent<ContentSizeFitter>();
        csfPanel.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Attach HUD script to panel root
        AnxietyHud hud = panel.gameObject.AddComponent<AnxietyHud>();

        CreateRow(panel, "Face", out Text faceText, out Slider faceSlider);
        CreateRow(panel, "Voice", out Text voiceText, out Slider voiceSlider);
        CreateRow(panel, "Fused", out Text fusedText, out Slider fusedSlider);

        hud.faceText = faceText;
        hud.faceSlider = faceSlider;
        hud.voiceText = voiceText;
        hud.voiceSlider = voiceSlider;
        hud.fusedText = fusedText;
        hud.fusedSlider = fusedSlider;

        bool success;
        PrefabUtility.SaveAsPrefabAsset(canvasGo, PrefabPath, out success);
        Object.DestroyImmediate(canvasGo);

        if (success)
        {
            Debug.Log("Created prefab: " + PrefabPath);
            AssetDatabase.Refresh();
        }
        else
        {
            Debug.LogError("Failed to create prefab at: " + PrefabPath);
        }
    }

    private static void CreateRow(Transform parent, string label, out Text text, out Slider slider)
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

        Image bg = sliderGo.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.1f);
        slider.targetGraphic = bg;
        slider.fillRect = null;
        slider.handleRect = null;
    }
}



