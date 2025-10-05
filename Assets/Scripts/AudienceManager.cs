using UnityEngine;
using System.Collections.Generic;

public class AudienceManager : MonoBehaviour
{
    [Header("Audience Settings")]
    public List<CityPeople.CityPeople> audienceMembers = new List<CityPeople.CityPeople>();
    public float transitionSpeed = 2f;
    
    [Header("Score Ranges")]
    public int currentScore = 10;
    public int previousScore = 10;
    
    private Dictionary<int, AudienceBehavior> scoreBehaviors;
    private Dictionary<CityPeople.CityPeople, Vector3> originalPositions = new Dictionary<CityPeople.CityPeople, Vector3>();
    private Dictionary<CityPeople.CityPeople, Vector3> originalRotations = new Dictionary<CityPeople.CityPeople, Vector3>();
    
    void Start()
    {
        InitializeBehaviors();

        // Store original positions and rotations
        foreach (var member in audienceMembers)
        {
            originalPositions[member] = member.transform.position;
            originalRotations[member] = member.transform.eulerAngles;
        }

        // DISABLE AUTO-ANIMATIONS for all audience members
        foreach (var member in audienceMembers)
        {
            // Stop the automatic animation coroutine that runs every 15-20 seconds
            member.StopAllCoroutines();
            
            // Use reflection to disable AutoPlayAnimations
            var autoPlayField = typeof(CityPeople.CityPeople).GetField("AutoPlayAnimations", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            autoPlayField?.SetValue(member, false);
            
            // Manually take control of the animator
            var animator = member.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                // Start with an idle animation
                var clips = animator.runtimeAnimatorController.animationClips;
                if (clips != null && clips.Length > 0)
                {
                    foreach (var clip in clips)
                    {
                        if (clip.name.ToLower().Contains("idle"))
                        {
                            animator.CrossFadeInFixedTime(clip.name, 0.3f, -1, 0f);
                            break;
                        }
                    }
                }
            }
        }
        SetAudienceBehavior(currentScore);
    }
    
    void InitializeBehaviors()
    {
        scoreBehaviors = new Dictionary<int, AudienceBehavior>
        {
            {10, new AudienceBehavior("Idle", 1f, 0f, false, 0f)},
            {9, new AudienceBehavior("Idle", 0.8f, 0.1f, false, 0f)},
            {8, new AudienceBehavior("WarmingUp", 0.6f, 0.2f, true, 0.1f)},
            {7, new AudienceBehavior("WarmingUp", 0.5f, 0.3f, true, 0.2f)},
            {6, new AudienceBehavior("Walking", 0.4f, 0.4f, true, 0.3f)},
            {5, new AudienceBehavior("Walking", 0.3f, 0.5f, true, 0.4f)},
            {4, new AudienceBehavior("Dancing", 0.2f, 0.6f, true, 0.5f)},
            {3, new AudienceBehavior("Dancing", 0.1f, 0.7f, true, 0.6f)},
            {2, new AudienceBehavior("Running", 0.05f, 0.8f, true, 0.7f)},
            {1, new AudienceBehavior("Running", 0f, 1f, true, 1f)}
        };
    }
    
    public void UpdateScore(int newScore)
    {
        previousScore = currentScore;
        currentScore = Mathf.Clamp(newScore, 1, 10);
        StartCoroutine(TransitionToNewBehavior());
    }
    
    System.Collections.IEnumerator TransitionToNewBehavior()
    {
        // First, stop all current behavior coroutines
        foreach (var member in audienceMembers)
        {
            member.StopAllCoroutines();
        }
        
        // Reset everyone to original positions/rotations first
        yield return StartCoroutine(ResetAudienceToOriginalPositions());
        
        // Then apply new behaviors
        for (int i = 0; i < audienceMembers.Count; i++)
        {
            StartCoroutine(ApplyEngagementBehavior(audienceMembers[i], scoreBehaviors[currentScore], i));
            yield return new WaitForSeconds(0.1f); // Stagger the starts slightly
        }
    }
    
    System.Collections.IEnumerator ResetAudienceToOriginalPositions()
    {
        float resetTime = 1f;
        float elapsed = 0f;
        
        // Store current positions/rotations
        Dictionary<CityPeople.CityPeople, Vector3> currentPositions = new Dictionary<CityPeople.CityPeople, Vector3>();
        Dictionary<CityPeople.CityPeople, Vector3> currentRotations = new Dictionary<CityPeople.CityPeople, Vector3>();
        
        foreach (var member in audienceMembers)
        {
            currentPositions[member] = member.transform.position;
            currentRotations[member] = member.transform.eulerAngles;
        }
        
        // Smoothly return to original positions
        while (elapsed < resetTime)
        {
            float progress = elapsed / resetTime;
            
            foreach (var member in audienceMembers)
            {
                member.transform.position = Vector3.Lerp(
                    currentPositions[member],
                    originalPositions[member],
                    progress
                );
                
                member.transform.rotation = Quaternion.Lerp(
                    Quaternion.Euler(currentRotations[member]),
                    Quaternion.Euler(originalRotations[member]),
                    progress
                );
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure exact original positions
        foreach (var member in audienceMembers)
        {
            member.transform.position = originalPositions[member];
            member.transform.rotation = Quaternion.Euler(originalRotations[member]);
        }
    }
    
    void ApplyBehaviorToMember(CityPeople.CityPeople member, float progress, int memberIndex)
    {
        var behavior = scoreBehaviors[currentScore];

        // Since we only have idle animations, focus on behavior through movement and posture
        var animator = member.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            if (clips != null && clips.Length > 0)
            {
                // Play the available idle animation with varying speed based on engagement
                var clip = clips[0]; // Use the only available clip
                
                // Vary the animation speed based on engagement level
                float animationSpeed = GetAnimationSpeedForScore(currentScore);
                animator.speed = animationSpeed;
                
                Debug.Log($"[Score {currentScore}] Playing '{clip.name}' on {member.name} at speed {animationSpeed:F2}");
                animator.CrossFadeInFixedTime(clip.name, 0.5f, -1, Random.value * clip.length);
            }
        }
    }

    float GetAnimationSpeedForScore(int score)
    {
        // Higher scores = slower, more attentive animation
        // Lower scores = faster, more restless animation
        switch (score)
        {
            case 10:
            case 9:
                return 0.8f; // Slow, calm, attentive
            case 8:
            case 7:
                return 1.0f; // Normal speed
            case 6:
            case 5:
                return 1.2f; // Slightly restless
            case 4:
            case 3:
                return 1.5f; // More restless
            case 2:
            case 1:
                return 2.0f; // Very restless/agitated
            default:
                return 1.0f;
        }
    }

    System.Collections.IEnumerator ApplyEngagementBehavior(CityPeople.CityPeople member, AudienceBehavior behavior, int memberIndex)
    {
        Vector3 originalPos = originalPositions[member];
        Vector3 originalRot = originalRotations[member];
        
        // Apply behavior based on score
        switch (currentScore)
        {
            case 10:
            case 9:
                // Highly engaged - face forward, lean slightly forward
                yield return StartCoroutine(EngagedBehavior(member, originalPos, originalRot));
                break;
                
            case 8:
            case 7:
                // Moderately engaged - small fidgets while staying in seat
                yield return StartCoroutine(ModerateEngagementBehavior(member, originalPos, originalRot));
                break;
                
            case 6:
            case 5:
                // Distracted - looking around but staying seated
                yield return StartCoroutine(DistractedBehavior(member, originalPos, originalRot));
                break;
                
            case 4:
            case 3:
                // Disengaged - slouching, looking away
                yield return StartCoroutine(DisengagedBehavior(member, originalPos, originalRot));
                break;
                
            case 2:
            case 1:
                // Hostile - turned away but still in seat
                yield return StartCoroutine(HostileBehavior(member, originalPos, originalRot));
                break;
        }
    }

    System.Collections.IEnumerator EngagedBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Lean slightly forward to show interest (small rotation change only)
        Vector3 engagedRotation = originalRot + new Vector3(10f, 0f, 0f);
        
        float time = 0f;
        while (time < 2f)
        {
            member.transform.rotation = Quaternion.Lerp(
                member.transform.rotation,
                Quaternion.Euler(engagedRotation),
                Time.deltaTime * 1f
            );
            time += Time.deltaTime;
            yield return null;
        }
        
        // Stay engaged - minimal movement
        while (currentScore >= 9)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    System.Collections.IEnumerator ModerateEngagementBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Small fidgets while staying in original position
        while (currentScore >= 7 && currentScore <= 8)
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            
            // Very small position shift (stay in seat)
            Vector3 fidgetPos = originalPos + new Vector3(
                Random.Range(-0.02f, 0.02f), 
                0, 
                Random.Range(-0.02f, 0.02f)
            );
            
            // Small head turn
            Vector3 fidgetRot = originalRot + new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-10f, 10f),
                0f
            );
            
            // Move to fidget position
            float fidgetTime = 0.5f;
            while (fidgetTime > 0 && currentScore >= 7 && currentScore <= 8)
            {
                member.transform.position = Vector3.Lerp(member.transform.position, fidgetPos, Time.deltaTime * 3f);
                member.transform.rotation = Quaternion.Lerp(member.transform.rotation, Quaternion.Euler(fidgetRot), Time.deltaTime * 2f);
                fidgetTime -= Time.deltaTime;
                yield return null;
            }
            
            // Return to original
            fidgetTime = 1f;
            while (fidgetTime > 0 && currentScore >= 7 && currentScore <= 8)
            {
                member.transform.position = Vector3.Lerp(member.transform.position, originalPos, Time.deltaTime * 2f);
                member.transform.rotation = Quaternion.Lerp(member.transform.rotation, Quaternion.Euler(originalRot), Time.deltaTime * 1.5f);
                fidgetTime -= Time.deltaTime;
                yield return null;
            }
        }
    }

    System.Collections.IEnumerator DistractedBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Look around occasionally but stay seated
        while (currentScore >= 5 && currentScore <= 6)
        {
            yield return new WaitForSeconds(Random.Range(1f, 3f));
            
            // Look away (head turn only)
            Vector3 lookAwayRotation = originalRot + new Vector3(
                Random.Range(-15f, 15f), 
                Random.Range(-45f, 45f), 
                0f
            );
            
            // Turn to look away
            float turnTime = 1f;
            while (turnTime > 0 && currentScore >= 5 && currentScore <= 6)
            {
                member.transform.rotation = Quaternion.Lerp(
                    member.transform.rotation,
                    Quaternion.Euler(lookAwayRotation),
                    Time.deltaTime * 2f
                );
                turnTime -= Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            
            // Look back to speaker
            turnTime = 1.5f;
            while (turnTime > 0 && currentScore >= 5 && currentScore <= 6)
            {
                member.transform.rotation = Quaternion.Lerp(
                    member.transform.rotation,
                    Quaternion.Euler(originalRot),
                    Time.deltaTime * 1f
                );
                turnTime -= Time.deltaTime;
                yield return null;
            }
        }
    }

    System.Collections.IEnumerator DisengagedBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Slouch and look away more frequently
        Vector3 slouchedRotation = originalRot + new Vector3(-20f, Random.Range(-60f, 60f), 0f);
        
        // Slouch down
        float slouchTime = 2f;
        while (slouchTime > 0 && currentScore >= 3 && currentScore <= 4)
        {
            member.transform.rotation = Quaternion.Lerp(
                member.transform.rotation,
                Quaternion.Euler(slouchedRotation),
                Time.deltaTime * 1f
            );
            slouchTime -= Time.deltaTime;
            yield return null;
        }
        
        // Stay disengaged with occasional movement
        while (currentScore >= 3 && currentScore <= 4)
        {
            yield return new WaitForSeconds(Random.Range(3f, 6f));
            
            // Change slouch direction occasionally
            slouchedRotation = originalRot + new Vector3(-20f, Random.Range(-60f, 60f), 0f);
            
            float changeTime = 2f;
            while (changeTime > 0 && currentScore >= 3 && currentScore <= 4)
            {
                member.transform.rotation = Quaternion.Lerp(
                    member.transform.rotation,
                    Quaternion.Euler(slouchedRotation),
                    Time.deltaTime * 0.5f
                );
                changeTime -= Time.deltaTime;
                yield return null;
            }
        }
    }

    System.Collections.IEnumerator HostileBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Turn significantly away but stay in seat
        Vector3 hostileRotation = originalRot + new Vector3(0f, Random.Range(120f, 180f), 0f);
        
        // Turn away dramatically
        float hostileTime = 3f;
        while (hostileTime > 0 && currentScore >= 1 && currentScore <= 2)
        {
            member.transform.rotation = Quaternion.Lerp(
                member.transform.rotation,
                Quaternion.Euler(hostileRotation),
                Time.deltaTime * 1.5f
            );
            hostileTime -= Time.deltaTime;
            yield return null;
        }
        
        // Stay turned away
        while (currentScore >= 1 && currentScore <= 2)
        {
            yield return new WaitForSeconds(1f);
        }
    }
    
    void SetAudienceBehavior(int score)
    {
        if (!scoreBehaviors.ContainsKey(score)) return;
        
        for (int i = 0; i < audienceMembers.Count; i++)
        {
            ApplyBehaviorToMember(audienceMembers[i], 1f, i);
        }
    }
}

[System.Serializable]
public class AudienceBehavior
{
    public string primaryAnimation;
    public float attentionLevel;
    public float disengagementLevel;
    public bool shouldFidget;
    public float fidgetIntensity;
    public float animationChangeChance;
    
    public AudienceBehavior(string anim, float attention, float disengagement, 
                          bool fidget, float fidgetInt)
    {
        primaryAnimation = anim;
        attentionLevel = attention;
        disengagementLevel = disengagement;
        shouldFidget = fidget;
        fidgetIntensity = fidgetInt;
        animationChangeChance = 0.3f;
    }
}