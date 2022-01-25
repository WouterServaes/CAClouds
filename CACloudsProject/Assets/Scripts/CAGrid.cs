using UnityEngine;
using System.Collections.Generic;
public class CAGrid : MonoBehaviour
{
    [SerializeField] private CAGridSettings _CAGridSettings = null;
    [SerializeField] private CACellSettings _CACellSettings = null;
    [SerializeField] private WindSettings _WindSettings = null;
    [SerializeField] private bool _DrawGridOutline = true;
    [SerializeField] private bool _UseShader = true;

    private CellularAutomaton _CA;
    private CellularAutomatonShader _CAS;
    private void Start()
    {
        _CA = GetComponent<CellularAutomaton>();
        _CAS = GetComponent<CellularAutomatonShader>();
        _CAS.enabled = _UseShader;
        _CA.enabled = !_UseShader;
        _CAGridSettings.UpdatedGridSettingsAction += ResetCells;
        _WindSettings.UpdatedWind += RotateGridToWind;
        RotateGridToWind();
    }

    private void OnDrawGizmos()
    {
        if (_DrawGridOutline)
            DrawGridOutline();
    }

    private void ResetCells()
    {
        if (!_UseShader)
            _CA.ResetAction.Invoke();
        else
            _CAS.ResetAction.Invoke();
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

    private void RotateGridToWind()
    {
        var dir = _WindSettings.WindDirection.normalized;
        dir.y = 0f;
        transform.right = dir;
    }
}