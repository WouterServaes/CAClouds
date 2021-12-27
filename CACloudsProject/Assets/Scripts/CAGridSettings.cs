using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "CAGridSettings", menuName = "ScriptableObjects/CAGridSettings")]
public class CAGridSettings : ScriptableObject
{
    public int Rows;
    public int Columns;
    public int Depth;
    public float CellHeight;

    //Invoked in GridEditorSettings by pressing GUI button
    public UnityAction UpdatedGridSettingsAction;
}
