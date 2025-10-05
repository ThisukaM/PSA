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
    
    void Start()
    {
        InitializeBehaviors();
        SetAudienceBehavior(currentScore);
    }
    
    void InitializeBehaviors()
    {
        scoreBehaviors = new Dictionary<int, AudienceBehavior>
        {
            {10, new AudienceBehavior("Idle", 1f, 0f, false, false, 0f)},
            {9, new AudienceBehavior("Idle", 0.8f, 0.1f, false, false, 0f)},
            {8, new AudienceBehavior("WarmingUp", 0.6f, 0.2f, true, false, 0.1f)},
            {7, new AudienceBehavior("WarmingUp", 0.5f, 0.3f, true, false, 0.2f)},
            {6, new AudienceBehavior("Walking", 0.4f, 0.4f, true, true, 0.3f)},
            {5, new AudienceBehavior("Walking", 0.3f, 0.5f, true, true, 0.4f)},
            {4, new AudienceBehavior("Dancing", 0.2f, 0.6f, true, true, 0.5f)},
            {3, new AudienceBehavior("Dancing", 0.1f, 0.7f, true, true, 0.6f)},
            {2, new AudienceBehavior("Running", 0.05f, 0.8f, true, true, 0.7f)},
            {1, new AudienceBehavior("Running", 0f, 1f, true, true, 1f)}
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
        float transitionTime = 0f;
        
        while (transitionTime < 1f)
        {
            transitionTime += Time.deltaTime * transitionSpeed;
            
            for (int i = 0; i < audienceMembers.Count; i++)
            {
                float memberProgress = transitionTime + (i * 0.1f);
                if (memberProgress >= 1f) memberProgress = 1f;
                
                ApplyBehaviorToMember(audienceMembers[i], memberProgress, i);
            }
            
            yield return null;
        }
    }
    
    void ApplyBehaviorToMember(CityPeople.CityPeople member, float progress, int memberIndex)
    {
        var behavior = scoreBehaviors[currentScore];
        
        if (Random.value < behavior.animationChangeChance)
        {
            member.PlayAnyClip();
        }
        
        if (behavior.shouldFidget && Random.value < behavior.fidgetIntensity)
        {
            StartCoroutine(FidgetMember(member, behavior.fidgetIntensity));
        }
        
        if (behavior.phoneAnimation && Random.value < behavior.disengagementLevel)
        {
            StartCoroutine(DisengageMember(member));
        }
    }
    
    System.Collections.IEnumerator FidgetMember(CityPeople.CityPeople member, float intensity)
    {
        Vector3 originalPosition = member.transform.position;
        float fidgetTime = Random.Range(0.5f, 2f);
        
        while (fidgetTime > 0)
        {
            Vector3 fidgetOffset = new Vector3(
                Random.Range(-0.1f, 0.1f) * intensity,
                0,
                Random.Range(-0.1f, 0.1f) * intensity
            );
            
            member.transform.position = Vector3.Lerp(
                member.transform.position,
                originalPosition + fidgetOffset,
                Time.deltaTime * 2f
            );
            
            fidgetTime -= Time.deltaTime;
            yield return null;
        }
        
        member.transform.position = originalPosition;
    }
    
    System.Collections.IEnumerator DisengageMember(CityPeople.CityPeople member)
    {
        Vector3 originalRotation = member.transform.eulerAngles;
        Vector3 disengagedRotation = originalRotation + new Vector3(0, Random.Range(15f, 45f), 0);
        
        float rotateTime = 1f;
        while (rotateTime > 0)
        {
            member.transform.rotation = Quaternion.Lerp(
                member.transform.rotation,
                Quaternion.Euler(disengagedRotation),
                Time.deltaTime * 2f
            );
            
            rotateTime -= Time.deltaTime;
            yield return null;
        }
        
        yield return new WaitForSeconds(Random.Range(3f, 8f));
        
        if (currentScore > 5)
        {
            rotateTime = 0f;
            while (rotateTime < 1f)
            {
                member.transform.rotation = Quaternion.Lerp(
                    member.transform.rotation,
                    Quaternion.Euler(originalRotation),
                    Time.deltaTime
                );
                
                rotateTime += Time.deltaTime;
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
    public bool phoneAnimation;
    public float fidgetIntensity;
    public float animationChangeChance;
    
    public AudienceBehavior(string anim, float attention, float disengagement, 
                          bool fidget, bool phone, float fidgetInt)
    {
        primaryAnimation = anim;
        attentionLevel = attention;
        disengagementLevel = disengagement;
        shouldFidget = fidget;
        phoneAnimation = phone;
        fidgetIntensity = fidgetInt;
        animationChangeChance = 0.3f;
    }
}