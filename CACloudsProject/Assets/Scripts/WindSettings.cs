using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "WindSettings", menuName = "ScriptableObjects/WindSettings")]
public class WindSettings : ScriptableObject
{
    [Header("Wind settings")]
    public int WindSpeed;
    public Vector3 WindDirection;

    //Invoked when wind is updated
    public UnityAction UpdatedWind;
}
