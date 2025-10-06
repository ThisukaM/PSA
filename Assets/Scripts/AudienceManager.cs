using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public enum MicroAction
{
    LookForward,        // neutral/attentive
    SmallGlanceAway,    // quick yaw +/- ~10–15°
    BiggerGlanceAway,   // yaw +/- ~20–30°
    LeanForwardSmall,   // pitch -5..-10 (toward speaker)
    LeanBackSmall,      // pitch +4..+8
    FidgetMinor,        // subtle head/shoulder micro-adjust
    ChairShift,         // small position wiggle toward original pos
    SighOrYawn,         // slow lean + tiny head dip
    HostileTurn,        // yaw +35..50
}

public class AudienceManager : MonoBehaviour
{
    [Header("Audience Settings")]
    public List<CityPeople.CityPeople> audienceMembers = new List<CityPeople.CityPeople>();
    public float transitionSpeed = 2f;

    [Header("Score Ranges")]
    public int currentScore = 10;
    public int previousScore = 10;

    [Header("Random Timing")]
    public int globalSeed = 12345;
    public Vector2 actionIntervalRange = new Vector2(2f, 5f);
    public float actionCooldown = 2f;

    private Dictionary<int, AudienceBehavior> scoreBehaviors;
    private Dictionary<CityPeople.CityPeople, Vector3> originalPositions = new Dictionary<CityPeople.CityPeople, Vector3>();
    private Dictionary<CityPeople.CityPeople, Vector3> originalRotations = new Dictionary<CityPeople.CityPeople, Vector3>();
    private Dictionary<CityPeople.CityPeople, MicroAction> lastAction = new Dictionary<CityPeople.CityPeople, MicroAction>();
    private Dictionary<CityPeople.CityPeople, float> lastActionTime = new Dictionary<CityPeople.CityPeople, float>();
    private Dictionary<CityPeople.CityPeople, System.Random> memberRands = new Dictionary<CityPeople.CityPeople, System.Random>();
    private Dictionary<CityPeople.CityPeople, Coroutine> activeBehaviorCoroutines = new Dictionary<CityPeople.CityPeople, Coroutine>();

    void Start()
    {
        InitializeBehaviors();

        // Store original positions and rotations
        foreach (var member in audienceMembers)
        {
            if (member == null) continue;
            originalPositions[member] = member.transform.position;
            originalRotations[member] = member.transform.eulerAngles;
        }

        // DISABLE AUTO-ANIMATIONS for all audience members
        foreach (var member in audienceMembers)
        {
            if (member == null) continue;

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

        // Independent random loops per member (no personalities)
        foreach (var member in audienceMembers)
        {
            if (member == null) continue;

            int seed = member.GetInstanceID() ^ globalSeed;
            memberRands[member] = new System.Random(seed);
            lastAction[member] = MicroAction.LookForward;
            lastActionTime[member] = -999f;

            StartCoroutine(MemberActionLoop(member));
        }
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
        // Do NOT StopAllCoroutines(), or you'll kill MemberActionLoop.
        // Only stop previously running score-behavior coroutines.
        foreach (var member in audienceMembers)
        {
            if (member == null) continue;
            if (activeBehaviorCoroutines.TryGetValue(member, out var co) && co != null)
            {
                StopCoroutine(co);
            }
        }

        // Apply new behaviors and track coroutines
        for (int i = 0; i < audienceMembers.Count; i++)
        {
            var member = audienceMembers[i];
            if (member == null) continue;

            var co = StartCoroutine(ApplyEngagementBehavior(member, scoreBehaviors[currentScore], i));
            activeBehaviorCoroutines[member] = co;

            // Stagger starts to further desync crowd behavior
            yield return new WaitForSeconds(0.1f + 0.05f * i);
        }
    }

    void ApplyBehaviorToMember(CityPeople.CityPeople member, float progress, int memberIndex)
    {
        if (member == null) return;

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
                animator.CrossFadeInFixedTime(clip.name, 0.5f, -1, UnityEngine.Random.value * clip.length);
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
        if (member == null) yield break;

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
        while (time < 3f && currentScore >= 9)
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
            yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 6f));

            // Very small head turn
            Vector3 fidgetRot = originalRot + new Vector3(
                UnityEngine.Random.Range(0f, 8f),
                UnityEngine.Random.Range(-8f, 8f),
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
            yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 4f));

            // Look away - reasonable head turn
            Vector3 lookAwayRotation = originalRot + new Vector3(
                UnityEngine.Random.Range(-5f, 5f),
                UnityEngine.Random.Range(-20f, 20f), // Reduced further
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

            yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 5f));

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
        Vector3 slouchedRotation = originalRot + new Vector3(-8f, UnityEngine.Random.Range(-25f, 25f), 0f); // Much reduced

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
            yield return new WaitForSeconds(UnityEngine.Random.Range(4f, 8f));

            // Change direction slightly
            slouchedRotation = originalRot + new Vector3(-8f, UnityEngine.Random.Range(-25f, 25f), 0f);

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
        Vector3 hostileRotation = originalRot + new Vector3(-5f, UnityEngine.Random.Range(35f, 50f), 0f); // Much reduced

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
            yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 10f));

            // Occasionally shift to look even more away or back slightly
            Vector3 newHostileRotation = originalRot + new Vector3(-5f, UnityEngine.Random.Range(35f, 50f), 0f);

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
            var member = audienceMembers[i];
            if (member == null) continue;
            ApplyBehaviorToMember(member, 1f, i);
        }
    }

    System.Collections.IEnumerator MemberActionLoop(CityPeople.CityPeople member)
    {
        if (member == null) yield break;

        var rand = memberRands[member];

        // initial desync delay
        float initialDelay = Mathf.Lerp(actionIntervalRange.x, actionIntervalRange.y, (float)rand.NextDouble());
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // interval depends on score (higher score > slower actions)
            float scoreModifier = Mathf.Lerp(0.6f, 1.4f, Mathf.InverseLerp(1, 10, currentScore));
            float interval = Mathf.Lerp(actionIntervalRange.x, actionIntervalRange.y, (float)rand.NextDouble());
            interval *= scoreModifier;
            interval = Mathf.Clamp(interval, 0.8f, 8.0f);

            yield return new WaitForSeconds(interval);

            var action = ChooseAction(rand);
            if (Time.time - lastActionTime[member] < actionCooldown && action == lastAction[member])
                continue;

            yield return StartCoroutine(PlayMicroAction(member, action));
            lastAction[member] = action;
            lastActionTime[member] = Time.time;
        }
    }

    MicroAction ChooseAction(System.Random rand)
    {
        int s = currentScore;
        int delta = currentScore - previousScore;

        var w = new Dictionary<MicroAction, float>
    {
        { MicroAction.BiggerGlanceAway,  1f },
        { MicroAction.ChairShift,        1f },
        { MicroAction.FidgetMinor,       1f },
        { MicroAction.HostileTurn,       1f },
        { MicroAction.LeanBackSmall,     1f },
        { MicroAction.LeanForwardSmall,  1f },
        { MicroAction.LookForward,       1f },
        { MicroAction.SighOrYawn,        1f },
        { MicroAction.SmallGlanceAway,   1f }
    };

        if (s >= 9)
        {
            w[MicroAction.BiggerGlanceAway] = 0.0f;
            w[MicroAction.ChairShift] = 0.2f;
            w[MicroAction.FidgetMinor] = 0.7f;
            w[MicroAction.HostileTurn] = 0.0f;
            w[MicroAction.LeanBackSmall] = 0.0f;
            w[MicroAction.LeanForwardSmall] = 3.0f;
            w[MicroAction.LookForward] = 2.0f;
            w[MicroAction.SighOrYawn] = 0.0f;
            w[MicroAction.SmallGlanceAway] = 0.4f;

            if (delta > 0) w[MicroAction.FidgetMinor] += 0.2f;
        }
        else if (s >= 7)
        {
            w[MicroAction.BiggerGlanceAway] = 0.4f;
            w[MicroAction.ChairShift] = 0.8f;
            w[MicroAction.FidgetMinor] = 1.3f;
            w[MicroAction.HostileTurn] = 0.0f;
            w[MicroAction.LeanBackSmall] = 0.4f;
            w[MicroAction.LeanForwardSmall] = 1.6f;
            w[MicroAction.LookForward] = 1.4f;
            w[MicroAction.SighOrYawn] = 0.2f;
            w[MicroAction.SmallGlanceAway] = 1.2f;

            if (delta < 0) { w[MicroAction.SmallGlanceAway] += 0.3f; w[MicroAction.ChairShift] += 0.3f; }
            if (delta > 0) { w[MicroAction.LeanForwardSmall] += 0.3f; }
        }
        else if (s >= 5)
        {
            w[MicroAction.BiggerGlanceAway] = 1.4f;
            w[MicroAction.ChairShift] = 1.6f;
            w[MicroAction.FidgetMinor] = 1.3f;
            w[MicroAction.HostileTurn] = 0.0f;
            w[MicroAction.LeanBackSmall] = 1.0f;
            w[MicroAction.LeanForwardSmall] = 0.5f;
            w[MicroAction.LookForward] = 0.8f;
            w[MicroAction.SighOrYawn] = 0.9f;
            w[MicroAction.SmallGlanceAway] = 2.0f;
        }
        else if (s >= 3)
        {
            w[MicroAction.BiggerGlanceAway] = 1.4f;
            w[MicroAction.ChairShift] = 1.0f;
            w[MicroAction.FidgetMinor] = 0.0f;
            w[MicroAction.HostileTurn] = 0.0f;
            w[MicroAction.LeanBackSmall] = 2.2f;
            w[MicroAction.LeanForwardSmall] = 0.3f;
            w[MicroAction.LookForward] = 0.6f;
            w[MicroAction.SighOrYawn] = 1.8f;
            w[MicroAction.SmallGlanceAway] = 1.2f;
        }
        else
        {
            w[MicroAction.BiggerGlanceAway] = 1.8f;
            w[MicroAction.ChairShift] = 0.8f;
            w[MicroAction.FidgetMinor] = 0.0f;
            w[MicroAction.HostileTurn] = 2.6f;
            w[MicroAction.LeanBackSmall] = 1.6f;
            w[MicroAction.LeanForwardSmall] = 0.1f;
            w[MicroAction.LookForward] = 0.3f;
            w[MicroAction.SighOrYawn] = 1.2f;
            w[MicroAction.SmallGlanceAway] = 1.0f;
        }

        foreach (var k in w.Keys.ToList())
        {
            float jitter = 0.9f + (float)rand.NextDouble() * 0.2f; // ±10%
            w[k] = Mathf.Max(0f, w[k] * jitter);
        }

        float sum = w.Values.Sum();
        if (sum <= 0f) return MicroAction.LookForward;

        double rPick = rand.NextDouble() * sum;
        foreach (var kv in w)
        {
            rPick -= kv.Value;
            if (rPick <= 0) return kv.Key;
        }
        return MicroAction.LookForward;
    }

    System.Collections.IEnumerator PlayMicroAction(CityPeople.CityPeople member, MicroAction action)
    {
        if (member == null) yield break;

        Vector3 originalRot = originalRotations[member];
        Vector3 targetRot = originalRot;

        float ampScale;
        float distScale;
        float dur = 0.8f;
        float hold = 0.6f;
        float back = 0.8f;

        if (currentScore >= 9) { ampScale = 0.25f; distScale = 0.25f; dur = 0.55f; hold = 0.35f; back = 0.55f; }
        else if (currentScore >= 7) { ampScale = 0.50f; distScale = 0.50f; dur = 0.65f; hold = 0.45f; back = 0.65f; }
        else if (currentScore >= 5) { ampScale = 0.80f; distScale = 0.80f; dur = 0.85f; hold = 0.60f; back = 0.85f; }
        else { ampScale = 1.00f; distScale = 1.00f; dur = 1.00f; hold = 0.80f; back = 1.00f; }
        // --------------------------------------

        switch (action)
        {
            case MicroAction.LookForward:
                {
                    float forwardPitch = (currentScore >= 9 ? -3.5f : 0f) * ampScale;
                    targetRot = originalRot + new Vector3(forwardPitch, 0f, 0f);
                    break;
                }

            case MicroAction.SmallGlanceAway:
                {
                    float baseYaw = 12f;
                    float basePitch = 3f;
                    float yaw = baseYaw * ampScale;
                    float pitch = basePitch * ampScale;

                    targetRot = originalRot + new Vector3(
                        UnityEngine.Random.Range(-pitch, pitch),
                        UnityEngine.Random.Range(-yaw, yaw),
                        0f
                    );
                    break;
                }

            case MicroAction.BiggerGlanceAway:
                {
                    float baseYaw = 28f;
                    float basePitch = 4f;
                    float yaw = baseYaw * ampScale;
                    float pitch = basePitch * ampScale;

                    targetRot = originalRot + new Vector3(
                        UnityEngine.Random.Range(-pitch, pitch),
                        UnityEngine.Random.Range(-yaw, yaw),
                        0f
                    );

                    dur = Mathf.Max(dur, 0.9f);
                    hold = Mathf.Max(hold, 0.6f);
                    back = Mathf.Max(back, 0.9f);
                    break;
                }

            case MicroAction.LeanForwardSmall:
                {
                    float basePitch = 8f;
                    float pitch = basePitch * ampScale;
                    targetRot = originalRot + new Vector3(-UnityEngine.Random.Range(pitch * 0.8f, pitch * 1.2f), 0f, 0f);
                    break;
                }

            case MicroAction.LeanBackSmall:
                {
                    float basePitch = 8f;
                    float pitch = basePitch * ampScale;
                    targetRot = originalRot + new Vector3(UnityEngine.Random.Range(pitch * 0.8f, pitch * 1.2f), 0f, 0f);
                    break;
                }

            case MicroAction.FidgetMinor:
                {
                    float baseYaw = 8f, basePitch = 3f;
                    float yaw = baseYaw * ampScale;
                    float pitch = basePitch * ampScale;

                    targetRot = originalRot + new Vector3(
                        UnityEngine.Random.Range(-pitch, pitch),
                        UnityEngine.Random.Range(-yaw, yaw),
                        0f
                    );

                    dur = Mathf.Min(dur, 0.45f);
                    hold = Mathf.Min(hold, 0.25f);
                    back = Mathf.Min(back, 0.45f);
                    break;
                }

            case MicroAction.ChairShift:
                {
                    Vector3 origPos = originalPositions[member];
                    float baseAmp = 0.03f;
                    float amp = baseAmp * distScale;

                    Vector3 jitter = new Vector3(
                        UnityEngine.Random.Range(-amp, amp),
                        0f,
                        UnityEngine.Random.Range(-amp, amp)
                    );

                    float goT = Mathf.Max(0.40f, dur * 0.6f);
                    float backT = Mathf.Max(0.50f, back * 0.7f);

                    yield return LerpMove(member, member.transform.position, origPos + jitter, goT);
                    yield return new WaitForSeconds(0.2f * distScale);
                    yield return LerpMove(member, member.transform.position, origPos, backT);
                    yield break;
                }

            case MicroAction.SighOrYawn:
                {
                    float basePitch = 10f, baseYaw = 5f;
                    float pitch = basePitch * ampScale;
                    float yaw = baseYaw * ampScale;

                    targetRot = originalRot + new Vector3(
                        UnityEngine.Random.Range(pitch * 0.8f, pitch * 1.1f),
                        UnityEngine.Random.Range(-yaw, yaw),
                        0f
                    );

                    dur = Mathf.Max(dur, 1.0f);
                    hold = Mathf.Max(hold, 0.6f);
                    back = Mathf.Max(back, 1.0f);
                    break;
                }

            case MicroAction.HostileTurn:
                {
                    float yawMin = 35f * ampScale;
                    float yawMax = 55f * ampScale;
                    float yaw = UnityEngine.Random.Range(yawMin, yawMax);
                    targetRot = originalRot + new Vector3(-5f * ampScale, yaw, 0f);

                    dur = Mathf.Max(dur, 1.1f);
                    hold = Mathf.Max(hold, 0.9f);
                    back = Mathf.Max(back, 1.0f);
                    break;
                }
        }

        var animator = member.GetComponent<Animator>();
        if (animator) animator.speed = GetAnimationSpeedForScore(currentScore);

        // go > hold > back
        yield return LerpRot(member, member.transform.eulerAngles, targetRot, dur);
        yield return new WaitForSeconds(hold);
        yield return LerpRot(member, member.transform.eulerAngles, originalRot, back);
    }

    System.Collections.IEnumerator LerpRot(CityPeople.CityPeople member, Vector3 from, Vector3 to, float t)
    {
        float e = 0f;
        while (e < t)
        {
            member.transform.rotation = Quaternion.Lerp(Quaternion.Euler(from), Quaternion.Euler(to), e / t);
            e += Time.deltaTime;
            yield return null;
        }
        member.transform.rotation = Quaternion.Euler(to);
    }

    System.Collections.IEnumerator LerpMove(CityPeople.CityPeople member, Vector3 from, Vector3 to, float t)
    {
        float e = 0f;
        while (e < t)
        {
            member.transform.position = Vector3.Lerp(from, to, e / t);
            e += Time.deltaTime;
            yield return null;
        }
        member.transform.position = to;
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
