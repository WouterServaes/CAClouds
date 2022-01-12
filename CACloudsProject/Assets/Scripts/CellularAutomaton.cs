using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Threading;
public class CellularAutomaton : MonoBehaviour
{
    [SerializeField] private CASettings _CASettings = null;
    [SerializeField] private CAGridSettings _CAGridSettings = null;
    private bool _IsPaused = true;

    private int _GenerationCount = 0;
    private bool[,,,] _Cells;
    private bool[,,,] _NextCells;

    private List<Vector3Int> _CloudCells = new List<Vector3Int>();
    public List<Vector3Int> CloudCells => _CloudCells;
    private int _CellWidthInArray = 3;
    //state var positions:
    //0 = act
    //1 = cld
    //2 = hum

    //Invoked in CellularAutomatonEditor by pressing GUI buttons
    public UnityAction<bool> PauseContinueAction; //pauses and continues ca, true = paused | false = not paused
    public UnityAction ResetAction; //resets ca

    //Update timer
    private float _ElapsedSec = 0f;

    //Metric saver
    private Metrics _Metrics = null;

    //info properties
    public int CAMemoryCount => (_Cells == null) ? 0 : sizeof(bool) * _Cells.Length;
    public int CellMemoryCount => sizeof(bool) * _CellWidthInArray;
    public int CellCount => (_Cells == null) ? 0 : _Cells.Length / _CellWidthInArray;
    public int CloudCount => _CloudCells.Count;
    public float AvgCalcTime => (_Metrics)?_Metrics.GetAverageCalcTime():0;
    private void Start()
    {
        _Metrics = GetComponent<Metrics>();
        PauseContinueAction += PauseContinue;
        ResetAction += ResetCA;
    }

    public void InitializeCA()
    {
        _Cells = new bool[_CAGridSettings.Columns, _CAGridSettings.Rows, _CAGridSettings.Depth, _CellWidthInArray];
        _NextCells = new bool[_CAGridSettings.Columns, _CAGridSettings.Rows, _CAGridSettings.Depth, _CellWidthInArray];
        _GenerationCount = 0;
        SetInitialValues();

        Debug.Log(string.Format("Initialized {0} cells", _Cells.Length / _CellWidthInArray));
    }

    private void SetInitialValues()
    {
        //hum and act are set randomly at start
        for (int idxI = 0; idxI < _CAGridSettings.Columns; idxI++)
        {
            for (int idxJ = 0; idxJ < _CAGridSettings.Rows; idxJ++)
            {
                for (int idxK = 0; idxK < _CAGridSettings.Depth; idxK++)
                {
                    //hum
                    _Cells[idxI, idxJ, idxK, 2] = UnityEngine.Random.Range(0f, 1f) <= _CASettings.HumProbabilityAtStart;

                    //act can't be one if hum is zero at start
                    if (!_Cells[idxI, idxJ, idxK, 2])
                        _Cells[idxI, idxJ, idxK, 0] = UnityEngine.Random.Range(0f, 1f) <= _CASettings.ActProbabilityAtStart;
                }
            }
        }
    }

    private void PauseContinue(bool isPaused)
    {
        _IsPaused = isPaused;

        if (_IsPaused) Debug.Log("Paused");
        else Debug.Log("Continuing");
    }

    private void ResetCA()
    {
        Debug.Log("Reset CA");
        InitializeCA();
    }

    private void Update()
    {
        if (!_IsPaused)
        {
            _ElapsedSec += Time.deltaTime;
            if (_ElapsedSec >= _CASettings.MinimumUpdateTime)
            {
                _ElapsedSec = 0f;
                _CloudCells.Clear();
                UpdateCA();
            }
        }
    }

    private void UpdateCA()
    {
        _Metrics.StartCalcTimer();
        
        for (int idxI = 0; idxI < _CAGridSettings.Columns; idxI++)
        {
            for (int idxJ = 0; idxJ < _CAGridSettings.Rows; idxJ++)
            {
                for (int idxK = 0; idxK < _CAGridSettings.Depth; idxK++)
                {

                    processStateTransitionRules(idxI, idxJ, idxK);
                    ProcessExtFormRules(idxI, idxJ, idxK);



                    //saving the cell position of cloud cells so CAGrid can visualize those
                    if (_NextCells[idxI, idxJ, idxK, 1])
                        _CloudCells.Add(new Vector3Int(idxI, idxJ, idxK));
                }
            }
        }
        _Cells = _NextCells;
        _Metrics.StopCalcTimer();
        _GenerationCount++;
        Debug.Log("Updated CA gen " + _GenerationCount);
    }

    private void processStateTransitionRules(int cellIdxI, int cellIdxJ, int cellIdxK)
    {
        //simple state transition rules
        bool currentAct = _Cells[cellIdxI, cellIdxJ, cellIdxK, 0]
            , currentCld = _Cells[cellIdxI, cellIdxJ, cellIdxK, 1]
            , currentHum = _Cells[cellIdxI, cellIdxJ, cellIdxK, 2];
        //hum
        _NextCells[cellIdxI, cellIdxJ, cellIdxK, 2] = currentHum && !currentAct;

        //cld
        _NextCells[cellIdxI, cellIdxJ, cellIdxK, 1] = currentCld || currentAct;

        //act
        _NextCells[cellIdxI, cellIdxJ, cellIdxK, 0] = !currentAct && currentHum && GetActFromSurrounding(cellIdxI, cellIdxJ, cellIdxK);
    }

    private void ProcessExtFormRules(int cellIdxI, int cellIdxJ, int cellIdxK)
    {
        //cloud extinction and formation rules
        float rand = UnityEngine.Random.Range(0f, 1f);
        //cld becomes 0 if cld is 1 and rand is smaller than the extinction probability
        _NextCells[cellIdxI, cellIdxJ, cellIdxK, 1] = _Cells[cellIdxI, cellIdxJ, cellIdxK, 1] && rand < _CASettings.ExtProbability;

        //hum becomes 1 if hum is 1 or rand is smaller than humidity probability
        _NextCells[cellIdxI, cellIdxJ, cellIdxK, 2] = _Cells[cellIdxI, cellIdxJ, cellIdxK, 2] || rand < _CASettings.HumProbability;

        //act becomes 1 if act is 1 or rand is smaller than act probability
        _NextCells[cellIdxI, cellIdxJ, cellIdxK, 0] = _Cells[cellIdxI, cellIdxJ, cellIdxK, 0] || rand < _CASettings.ActProbability;
    }
    private bool GetActFromSurrounding(int cellIdxI, int cellIdxJ, int cellIdxK)
    {
        //checks the act from the surrounding cells of a cell;
        //if a neighboring cell is out the grid, don't count that cell

        return (cellIdxI + 1 < _CAGridSettings.Columns && _Cells[cellIdxI + 1, cellIdxJ, cellIdxK, 0])
            || (cellIdxI > 0 && _Cells[cellIdxI - 1, cellIdxJ, cellIdxK, 0])
            || (cellIdxJ + 1 < _CAGridSettings.Rows && _Cells[cellIdxI, cellIdxJ + 1, cellIdxK, 0])
            || (cellIdxJ > 0 && _Cells[cellIdxI, cellIdxJ - 1, cellIdxK, 0])
            || (cellIdxK + 1 < _CAGridSettings.Depth && _Cells[cellIdxI, cellIdxJ, cellIdxK + 1, 0])
            || (cellIdxK > 0 && _Cells[cellIdxI, cellIdxJ, cellIdxK - 1, 0])
            || (cellIdxI + 2 < _CAGridSettings.Columns && _Cells[cellIdxI + 2, cellIdxJ, cellIdxK, 0])
            || (cellIdxI > 1 && _Cells[cellIdxI - 2, cellIdxJ, cellIdxK, 0])
            || (cellIdxJ + 2 < _CAGridSettings.Rows && _Cells[cellIdxI, cellIdxJ + 2, cellIdxK, 0])
            || (cellIdxJ > 1 && _Cells[cellIdxI, cellIdxJ - 2, cellIdxK, 0])
            || (cellIdxK > 1 && _Cells[cellIdxI, cellIdxJ, cellIdxK - 2, 0]);
    }
}