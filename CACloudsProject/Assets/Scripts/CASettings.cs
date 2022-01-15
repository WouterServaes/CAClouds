using UnityEngine;

[CreateAssetMenu(fileName = "CASettings", menuName = "ScriptableObjects/CASettings")]
public class CASettings : ScriptableObject
{
    [Header("Update time")]
    public float MinimumUpdateTime;
    [Header("Clouds at start")]
    public float ActProbabilityAtStart = 0;
    public float HumProbabilityAtStart = 0;
    public int WindStartGeneration = 0;
    public int ExtStartGeneration = 0;
    [Header("Cloud extinction")]
    public float ExtProbability = 0;
    public float HumProbability = 0;
    public float ActProbability = 0;
}
