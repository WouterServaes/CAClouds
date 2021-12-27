using UnityEngine;
using UnityEditor;
public class CAGrid : MonoBehaviour
{
    [SerializeField] private CAGridSettings _CAGridSettings = null;
    [SerializeField] private CACellSettings _CACellSettings = null;
    [SerializeField] private bool _DrawGridOutline = true;
    private CACell[] _Cells;

    private void Start()
    {
        _CAGridSettings.UpdatedGridSettingsAction += InitializeCells;
        InitializeCells();
    }

    private void OnDrawGizmos()
    {
        if (_DrawGridOutline)
            DrawGridOutline();
    }

    private void InitializeCells()
    {
        _Cells = new CACell[_CAGridSettings.Columns * _CAGridSettings.Rows * _CAGridSettings.Depth];
        Debug.Log(string.Format("Initialized {0} cells", _Cells.Length));
    }

    private void DrawGridOutline()
    {
        var gridPos = transform.position;
        float cellHeight = _CAGridSettings.CellHeight;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(gridPos, gridPos + (transform.forward * _CAGridSettings.Depth * cellHeight));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(gridPos, gridPos + (transform.right * _CAGridSettings.Columns * cellHeight));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(gridPos, gridPos + (transform.up * _CAGridSettings.Rows * cellHeight));
    }
}