using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public AnxietyFusionManager anxietyFusion;
    public TMP_Text textMeshPro;
    void Start()
    {
        string formattedText = $"Face score: {anxietyFusion.faceScore1to10}\nVoice score: {anxietyFusion.voiceScore1to10}\nFused score: {anxietyFusion.fusedScore1to10}";
        UpdateText(formattedText);
    }

    // Update is called once per frame
    void Update()
    {
        string formattedText = $"Face score: {anxietyFusion.faceScore1to10}\nVoice score: {anxietyFusion.voiceScore1to10}\nFused score: {anxietyFusion.fusedScore1to10}";
        UpdateText(formattedText);
    }

    public void UpdateText(string newText)
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = newText;  // Change the text at runtime
        }
    }
}
