using UnityEngine;
using UnityEngine.UI;

namespace VoiceAnxietySystem
{
    public class UIController : MonoBehaviour
    {
        [Header("Ãæ°å")]
        public GameObject mainPanel;
        public GameObject statsPanel;

        [Header("°´Å¥")]
        public Button startButton;
        public Button stopButton;

        private VoiceAnxietyAnalyzer analyzer;

        void Start()
        {
            analyzer = GetComponent<VoiceAnxietyAnalyzer>();

            if (startButton) startButton.onClick.AddListener(OnStartRecording);
            if (stopButton) stopButton.onClick.AddListener(OnStopRecording);

            ShowMainPanel();
        }

        public void ShowMainPanel()
        {
            if (mainPanel) mainPanel.SetActive(true);
            if (statsPanel) statsPanel.SetActive(false);
        }

        public void ShowStats()
        {
            if (statsPanel) statsPanel.SetActive(true);
        }

        public void HideStats()
        {
            if (statsPanel) statsPanel.SetActive(false);
        }

        public void OnStartRecording()
        {
            if (analyzer)
            {
                analyzer.StartRecording();
                if (startButton) startButton.interactable = false;
                if (stopButton) stopButton.interactable = true;
            }
        }

        public void OnStopRecording()
        {
            if (analyzer)
            {
                analyzer.StopRecording();
                if (startButton) startButton.interactable = true;
                if (stopButton) stopButton.interactable = false;
            }
        }
    }
}