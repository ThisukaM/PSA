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
        // Stop all current behavior coroutines
        foreach (var member in audienceMembers)
        {
            member.StopAllCoroutines();
        }
        
        // Apply new behaviors directly without resetting to center
        for (int i = 0; i < audienceMembers.Count; i++)
        {
            StartCoroutine(ApplyEngagementBehavior(audienceMembers[i], scoreBehaviors[currentScore], i));
            yield return new WaitForSeconds(0.1f); // Stagger the starts slightly
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
        
        // Apply behavior based on score - transition naturally from current position
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
                // Disengaged - slight slouching, occasional looking away
                yield return StartCoroutine(DisengagedBehavior(member, originalPos, originalRot));
                break;
                
            case 2:
            case 1:
                // Hostile - turned away but reasonably
                yield return StartCoroutine(HostileBehavior(member, originalPos, originalRot));
                break;
        }
    }

    System.Collections.IEnumerator EngagedBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Lean slightly forward to show interest
        Vector3 engagedRotation = originalRot + new Vector3(8f, 0f, 0f);
        
        // Transition naturally from current rotation
        float time = 0f;
        while (time < 3f)
        {
            member.transform.rotation = Quaternion.Lerp(
                member.transform.rotation,
                Quaternion.Euler(engagedRotation),
                Time.deltaTime * 0.8f
            );
            
            member.transform.position = Vector3.Lerp(
                member.transform.position,
                originalPos,
                Time.deltaTime * 1f
            );
            
            time += Time.deltaTime;
            yield return null;
        }
        
        // Stay engaged with minimal movement
        while (currentScore >= 9)
        {
            yield return new WaitForSeconds(1f);
        }
    }

    System.Collections.IEnumerator ModerateEngagementBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Transition to upright, attentive position first
        Vector3 attentiveRotation = originalRot + new Vector3(3f, 0f, 0f);
        
        float transitionTime = 2f;
        while (transitionTime > 0 && currentScore >= 7 && currentScore <= 8)
        {
            member.transform.rotation = Quaternion.Lerp(
                member.transform.rotation,
                Quaternion.Euler(attentiveRotation),
                Time.deltaTime * 1f
            );
            
            member.transform.position = Vector3.Lerp(
                member.transform.position,
                originalPos,
                Time.deltaTime * 1f
            );
            
            transitionTime -= Time.deltaTime;
            yield return null;
        }
        
        // Small fidgets while staying in original position
        while (currentScore >= 7 && currentScore <= 8)
        {
            yield return new WaitForSeconds(Random.Range(3f, 6f));
            
            // Very small head turn
            Vector3 fidgetRot = originalRot + new Vector3(
                Random.Range(0f, 8f),
                Random.Range(-8f, 8f),
                0f
            );
            
            // Move to fidget position
            float fidgetTime = 1f;
            while (fidgetTime > 0 && currentScore >= 7 && currentScore <= 8)
            {
                member.transform.rotation = Quaternion.Lerp(member.transform.rotation, Quaternion.Euler(fidgetRot), Time.deltaTime * 1.5f);
                fidgetTime -= Time.deltaTime;
                yield return null;
            }
            
            // Return to attentive position
            fidgetTime = 2f;
            while (fidgetTime > 0 && currentScore >= 7 && currentScore <= 8)
            {
                member.transform.rotation = Quaternion.Lerp(member.transform.rotation, Quaternion.Euler(attentiveRotation), Time.deltaTime * 1f);
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
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            
            // Look away - reasonable head turn
            Vector3 lookAwayRotation = originalRot + new Vector3(
                Random.Range(-5f, 5f), 
                Random.Range(-20f, 20f), // Reduced further
                0f
            );
            
            // Turn to look away naturally
            float turnTime = 2f;
            while (turnTime > 0 && currentScore >= 5 && currentScore <= 6)
            {
                member.transform.rotation = Quaternion.Lerp(
                    member.transform.rotation,
                    Quaternion.Euler(lookAwayRotation),
                    Time.deltaTime * 1f
                );
                turnTime -= Time.deltaTime;
                yield return null;
            }
            
            yield return new WaitForSeconds(Random.Range(3f, 5f));
            
            // Look back to speaker
            turnTime = 3f;
            while (turnTime > 0 && currentScore >= 5 && currentScore <= 6)
            {
                member.transform.rotation = Quaternion.Lerp(
                    member.transform.rotation,
                    Quaternion.Euler(originalRot),
                    Time.deltaTime * 0.7f
                );
                turnTime -= Time.deltaTime;
                yield return null;
            }
        }
    }

    System.Collections.IEnumerator DisengagedBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Slight slouch and occasional looking away - much more subtle
        Vector3 slouchedRotation = originalRot + new Vector3(-8f, Random.Range(-25f, 25f), 0f); // Much reduced
        
        // Slouch gradually
        float slouchTime = 4f;
        while (slouchTime > 0 && currentScore >= 3 && currentScore <= 4)
        {
            member.transform.rotation = Quaternion.Lerp(
                member.transform.rotation,
                Quaternion.Euler(slouchedRotation),
                Time.deltaTime * 0.5f
            );
            slouchTime -= Time.deltaTime;
            yield return null;
        }
        
        // Stay disengaged with occasional subtle movement
        while (currentScore >= 3 && currentScore <= 4)
        {
            yield return new WaitForSeconds(Random.Range(4f, 8f));
            
            // Change direction slightly
            slouchedRotation = originalRot + new Vector3(-8f, Random.Range(-25f, 25f), 0f);
            
            float changeTime = 3f;
            while (changeTime > 0 && currentScore >= 3 && currentScore <= 4)
            {
                member.transform.rotation = Quaternion.Lerp(
                    member.transform.rotation,
                    Quaternion.Euler(slouchedRotation),
                    Time.deltaTime * 0.3f
                );
                changeTime -= Time.deltaTime;
                yield return null;
            }
        }
    }

    System.Collections.IEnumerator HostileBehavior(CityPeople.CityPeople member, Vector3 originalPos, Vector3 originalRot)
    {
        // Turn away but keep it realistic - maximum 45-60 degrees
        Vector3 hostileRotation = originalRot + new Vector3(-5f, Random.Range(35f, 50f), 0f); // Much reduced
        
        // Turn away gradually
        float hostileTime = 5f;
        while (hostileTime > 0 && currentScore >= 1 && currentScore <= 2)
        {
            member.transform.rotation = Quaternion.Lerp(
                member.transform.rotation,
                Quaternion.Euler(hostileRotation),
                Time.deltaTime * 0.6f
            );
            hostileTime -= Time.deltaTime;
            yield return null;
        }
        
        // Stay turned away with occasional shifts
        while (currentScore >= 1 && currentScore <= 2)
        {
            yield return new WaitForSeconds(Random.Range(5f, 10f));
            
            // Occasionally shift to look even more away or back slightly
            Vector3 newHostileRotation = originalRot + new Vector3(-5f, Random.Range(35f, 50f), 0f);
            
            float shiftTime = 3f;
            while (shiftTime > 0 && currentScore >= 1 && currentScore <= 2)
            {
                member.transform.rotation = Quaternion.Lerp(
                    member.transform.rotation,
                    Quaternion.Euler(newHostileRotation),
                    Time.deltaTime * 0.4f
                );
                shiftTime -= Time.deltaTime;
                yield return null;
            }
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