using UnityEngine;
using System.Collections.Generic;
public class CAGrid : MonoBehaviour
{
    [SerializeField] private CAGridSettings _CAGridSettings = null;
    [SerializeField] private CACellSettings _CACellSettings = null;
    [SerializeField] private WindSettings _WindSettings = null;
    [SerializeField] private bool _DrawGridOutline = true;
    [SerializeField] private bool _DrawCloudCells = true;
    
    private CellularAutomaton _CA;
    private void Start()
    {
        _CA = GetComponent<CellularAutomaton>();
        _CAGridSettings.UpdatedGridSettingsAction += InitializeCells;
        _WindSettings.UpdatedWind += RotateGridToWind;
        RotateGridToWind();
        InitializeCells();
    }

    private void OnDrawGizmos()
    {
        if (_DrawGridOutline)
            DrawGridOutline();
        if (_DrawCloudCells && _CA != null)
            DrawCloudCells();
    }

    private void InitializeCells()
    {
        _CA.InitializeCA();
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

    private void DrawCloudCells()
    {
        List<Vector3Int> cloudCells = _CA.CloudCells ;
        if (cloudCells == null) return;

        Vector3 gridPos = transform.position;
        
        float cellHeight = _CAGridSettings.CellHeight;
        float halfHeight = cellHeight / 2f;
       
        Gizmos.color = _CACellSettings.CloudColor;
        for(int idx = 0; idx < cloudCells.Count;idx++)
        {
            Vector3 cellPos = new Vector3(
                              cloudCells[idx].x * cellHeight + halfHeight
                            , cloudCells[idx].y * cellHeight + halfHeight
                            , cloudCells[idx].z * cellHeight + halfHeight);
            cellPos = transform.localToWorldMatrix * cellPos;

            Gizmos.DrawSphere(cellPos, halfHeight);
        }
    }

    private void RotateGridToWind()
    {
        var dir = _WindSettings.WindDirection.normalized;
        dir.y = 0f;
        transform.right = dir;
    }
}