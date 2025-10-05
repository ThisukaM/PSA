using UnityEngine;

public class TestAudienceScene : MonoBehaviour
{
    public AudienceManager audienceManager;
    public KeyCode[] scoreKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, 
                                   KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };
    
    void Start()
    {
        if (audienceManager == null)
            audienceManager = FindObjectOfType<AudienceManager>();
    }
    
    void Update()
    {
        for (int i = 0; i < scoreKeys.Length; i++)
        {
            if (Input.GetKeyDown(scoreKeys[i]))
            {
                int score = (i == 9) ? 10 : i + 1;
                Debug.Log($"Setting score to: {score}");
                audienceManager.UpdateScore(score);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.G))
            audienceManager.UpdateScore(9);
        if (Input.GetKeyDown(KeyCode.B))
            audienceManager.UpdateScore(3);
        if (Input.GetKeyDown(KeyCode.T))
            audienceManager.UpdateScore(1);
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 100), 
                  "Press 1-9,0 for scores 1-10\nG = Good (9)\nB = Bad (3)\nT = Terrible (1)");
    }
}