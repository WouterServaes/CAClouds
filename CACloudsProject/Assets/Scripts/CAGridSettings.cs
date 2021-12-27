using UnityEngine;

[CreateAssetMenu(fileName = "CAGridSettings", menuName = "ScriptableObjects/CAGridSettings", order = 1)]
public class CAGridSettings : ScriptableObject
{
    public int Rows;
    public int Columns;
    public int Depth;
    public float CellHeight;
}
