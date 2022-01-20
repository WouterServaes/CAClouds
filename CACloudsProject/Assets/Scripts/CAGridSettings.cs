using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "CAGridSettings", menuName = "ScriptableObjects/CAGridSettings")]
public class CAGridSettings : ScriptableObject
{
    [Header("Grid dimensions")]
    public int Columns;
    public int Rows;
    public int Depth;
    public int TotalCells => Columns * Rows * Depth;
    [Header("Cell dimensions")]
    public float CellHeight;
    
    //Invoked in GridEditorSettings by pressing GUI button
    public UnityAction UpdatedGridSettingsAction;

    //The bounds of the whole grid
    public Bounds GridBouds => new Bounds(
        Vector3.zero, 
        new Vector3(Columns * CellHeight, Rows * CellHeight, Depth * CellHeight)
        );
}
