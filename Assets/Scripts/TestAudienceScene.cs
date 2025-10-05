using UnityEngine;
using UnityEngine.InputSystem;

public class TestAudienceScene : MonoBehaviour
{
    public AudienceManager audienceManager;
    
    void Start()
    {
        if (audienceManager == null)
            audienceManager = FindFirstObjectByType<AudienceManager>();
    }
    
    void Update()
    {
        // Test score changes with number keys (1-0 for scores 1-10)
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 1");
            audienceManager.UpdateScore(1);
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 2");
            audienceManager.UpdateScore(2);
        }
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 3");
            audienceManager.UpdateScore(3);
        }
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 4");
            audienceManager.UpdateScore(4);
        }
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 5");
            audienceManager.UpdateScore(5);
        }
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 6");
            audienceManager.UpdateScore(6);
        }
        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 7");
            audienceManager.UpdateScore(7);
        }
        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 8");
            audienceManager.UpdateScore(8);
        }
        if (Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 9");
            audienceManager.UpdateScore(9);
        }
        if (Keyboard.current.digit0Key.wasPressedThisFrame)
        {
            Debug.Log("Setting score to: 10");
            audienceManager.UpdateScore(10);
        }
        
        // Quick test buttons
        if (Keyboard.current.gKey.wasPressedThisFrame) // G for Good (score 9)
        {
            Debug.Log("Setting score to: 9 (Good)");
            audienceManager.UpdateScore(9);
        }
        if (Keyboard.current.bKey.wasPressedThisFrame) // B for Bad (score 3)
        {
            Debug.Log("Setting score to: 3 (Bad)");
            audienceManager.UpdateScore(3);
        }
        if (Keyboard.current.tKey.wasPressedThisFrame) // T for Terrible (score 1)
        {
            Debug.Log("Setting score to: 1 (Terrible)");
            audienceManager.UpdateScore(1);
        }
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 100), 
                  "Press 1-9,0 for scores 1-10\nG = Good (9)\nB = Bad (3)\nT = Terrible (1)");
    }
}