using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "CAGridSettings", menuName = "ScriptableObjects/CAGridSettings")]
public class CAGridSettings : ScriptableObject
{
    [Header("Grid dimensions")]
    public int Columns;
    public int Rows;
    public int Depth;
    [Header("Cell dimensions")]
    public float CellHeight;
    
    //Invoked in GridEditorSettings by pressing GUI button
    public UnityAction UpdatedGridSettingsAction;
}
