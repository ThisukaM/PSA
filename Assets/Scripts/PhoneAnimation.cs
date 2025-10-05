using UnityEngine;

public class PhoneAnimation : MonoBehaviour
{
    public static void SimulatePhoneUse(CityPeople.CityPeople member, float intensity)
    {
        member.transform.rotation = Quaternion.Euler(
            member.transform.eulerAngles.x + 30f,
            member.transform.eulerAngles.y + Random.Range(-20f, 20f),
            member.transform.eulerAngles.z
        );
        
        if (member.GetComponent<Animator>())
        {
            member.GetComponent<Animator>().Play("WarmingUp");
        }
    }
    
    public static void SimulateDisagreement(CityPeople.CityPeople member)
    {
        if (member.GetComponent<Animator>())
        {
            member.GetComponent<Animator>().Play("Dancing");
        }
        
        member.StartCoroutine(HeadShake(member));
    }
    
    static System.Collections.IEnumerator HeadShake(CityPeople.CityPeople member)
    {
        Vector3 originalRotation = member.transform.eulerAngles;
        float shakeTime = 2f;
        
        while (shakeTime > 0)
        {
            float shakeAmount = Mathf.Sin(Time.time * 8f) * 10f;
            member.transform.rotation = Quaternion.Euler(
                originalRotation.x,
                originalRotation.y + shakeAmount,
                originalRotation.z
            );
            
            shakeTime -= Time.deltaTime;
            yield return null;
        }
        
        member.transform.rotation = Quaternion.Euler(originalRotation);
    }
}