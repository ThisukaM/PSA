using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceAnxietySystem
{
    public class GraphDrawer : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public RectTransform graphContainer;
        public int maxPoints = 100;

        private List<float> dataPoints = new List<float>();

        void Start()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            VoiceAnxietyAnalyzer.OnAnxietyLevelChanged += OnAnxietyUpdate;
        }

        void OnDestroy()
        {
            VoiceAnxietyAnalyzer.OnAnxietyLevelChanged -= OnAnxietyUpdate;
        }

        void OnAnxietyUpdate(float value)
        {
            dataPoints.Add(value);
            if (dataPoints.Count > maxPoints)
                dataPoints.RemoveAt(0);

            DrawGraph();
        }

        void DrawGraph()
        {
            if (lineRenderer == null || dataPoints.Count < 2) return;

            lineRenderer.positionCount = dataPoints.Count;

            float width = graphContainer ? graphContainer.rect.width : 500f;
            float height = graphContainer ? graphContainer.rect.height : 200f;

            for (int i = 0; i < dataPoints.Count; i++)
            {
                float x = (i / (float)(maxPoints - 1)) * width;
                float y = dataPoints[i] * height;
                lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }
    }
}