using UnityEngine;

[CreateAssetMenu(fileName = "CACellSettings", menuName = "ScriptableObjects/CACellSettings")]
public class CACellSettings : ScriptableObject
{
    [Header("Colors")]
    public Color CloudColor = Color.gray;
}
