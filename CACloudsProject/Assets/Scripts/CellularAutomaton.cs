using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CellularAutomaton : MonoBehaviour
{
    [SerializeField] private CASettings _CASettings = null;
    [SerializeField] private WindSettings _WindSettings = null;
    private CAGridSettings _CAGridSettings = null;
    public CAGridSettings CAGridSettings { set { _CAGridSettings = value; } }

    private bool _IsPaused = true;

    private BitArray _Act, _Cld, _Hum, _NextAct, _NextCld, _NextHum;

    private int _GenerationCount = 0;
   
    private List<int> _CloudCells = new List<int>();
    public List<int> CloudCells => _CloudCells;
    private int _StateCount = 3;
    

    //Invoked in CellularAutomatonEditor by pressing GUI buttons
    public UnityAction<bool> PauseContinueAction; //pauses and continues ca, true = paused | false = not paused

    public UnityAction ResetAction; //resets ca

    //Update timer
    private float _ElapsedSec = 0f;

    //Metric saver
    private Metrics _Metrics = null;

    //info properties
    public int CAMemoryCount => (_CAGridSettings == null) ? 0 : _CAGridSettings.TotalCells * _StateCount;
    public int CellMemoryCount => _StateCount;
    public int CellCount => (_CAGridSettings == null) ? 0 : _CAGridSettings.TotalCells;
    public int CloudCount => _CloudCells.Count;
    public int GenerationCount => _GenerationCount;
    public float AvgCalcTime => (_Metrics) ? _Metrics.GetAverageCalcTime() : 0;

    private void Start()
    {
        _Metrics = GetComponent<Metrics>();
        PauseContinueAction += PauseContinue;
        ResetAction += ResetCA;
    }

    public void InitializeCA()
    {
        _Act = new BitArray(_CAGridSettings.TotalCells, false);
        _Cld = new BitArray(_CAGridSettings.TotalCells, false);
        _Hum = new BitArray(_CAGridSettings.TotalCells, false);

        _NextAct = new BitArray(_CAGridSettings.TotalCells, false);
        _NextCld = new BitArray(_CAGridSettings.TotalCells, false);
        _NextHum = new BitArray(_CAGridSettings.TotalCells, false);

        _GenerationCount = 0;
        SetInitialValues();

        Debug.Log(string.Format("Initialized {0} cells", _NextAct.Count));
    }

    private void SetInitialValues()
    {
        for (int idx = 0; idx < _CAGridSettings.TotalCells; idx++)
        {
            bool humAtStart = Random.Range(0f, 1f) <= _CASettings.HumProbabilityAtStart;
            _Hum[idx] = humAtStart;
            if (!humAtStart)
                _Act[idx] = Random.Range(0f, 1f) <= _CASettings.ActProbabilityAtStart;
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
        //---start metrics---
        for (int cellIdx = 0; cellIdx < _CAGridSettings.TotalCells; cellIdx++)
        {
            processStateTransitionRules(cellIdx);
            if (_GenerationCount >= _CASettings.ExtStartGeneration) ProcessExtFormRules(cellIdx);
            if (_GenerationCount >= _CASettings.WindStartGeneration) ProcessWind(cellIdx);
            
            //saving the cell position of cloud cells so CAGrid can visualize those
            if (_NextCld[cellIdx])
                _CloudCells.Add(cellIdx);
        }

        //copy the t+1 array into t
        //.Clone -> BitArray is a reference type
        _Act = (BitArray)_NextAct.Clone();
        _Cld = (BitArray)_NextCld.Clone();
        _Hum = (BitArray)_NextHum.Clone();

        //---stop metrics---
        _Metrics.StopCalcTimer();
        _GenerationCount++;
    }

    
    private void processStateTransitionRules(int cellIdx)
    {
        //simple state transition rules
        bool currentAct = _Act[cellIdx]
            , currentCld = _Cld[cellIdx]
            , currentHum = _Hum[cellIdx];
        //hum
        _NextHum[cellIdx] = currentHum && !currentAct;

        //cld
        _NextCld[cellIdx] = currentCld || currentAct;

        //act
        _NextAct[cellIdx] = !currentAct && currentHum && GetActFromSurrounding(cellIdx);
    }

    private void ProcessExtFormRules(int cellIdx)
    {
        //cloud extinction and formation rules
        float rand = Random.Range(0f, 1f);

        //hum becomes 1 if hum is 1 or rand is smaller than humidity probability
        _NextHum[cellIdx] = _Hum[cellIdx] || rand < _CASettings.HumProbability;

        //cld becomes 0 if cld is 1 and rand is bigger than the extinction probability
        _NextCld[cellIdx] = _Cld[cellIdx] && rand > _CASettings.ExtProbability;

        //act becomes 1 if act is 1 or rand is smaller than act probability
        _NextAct[cellIdx] = _Act[cellIdx] || rand < _CASettings.ActProbability;
    }

    private void ProcessWind(int cellIdx)
    {
        //advection by wind rules

        int cellIdxK, cellIdxJ, cellIdxI;
        OneDToThreeDIndex(cellIdx, out cellIdxI, out cellIdxJ, out cellIdxK);

        float cellHeight = GetCellHeightInWorld(cellIdxK);
        int cellDisplacementByWind = WindHelper.GetWindSpeedCellDisplacementAtHeight();
        
        if(cellIdxI - cellDisplacementByWind >= 0)
        {
            int cellIdxDisplacementByWind = ThreeDToOneDIndex(cellIdxI - cellDisplacementByWind, cellIdxJ, cellIdxK);
              
            //hum
            _NextHum[cellIdx] = _Hum[cellIdxDisplacementByWind];

            //cld
            _NextCld[cellIdx] = _Cld[cellIdxDisplacementByWind];

            //act
            _NextAct[cellIdx] = _Act[cellIdxDisplacementByWind];
        }
        else if(cellIdxI == 0)
        {
            _NextHum[cellIdx] = false;
            _NextCld[cellIdx] = false;
            _NextAct[cellIdx] = false;
        }
        
    }

    private float GetCellHeightInWorld(int cellIdxK)
    {
        return transform.position.y + cellIdxK * _CAGridSettings.CellHeight;
    }

    private bool GetActFromSurrounding(int cellIdx)
    {       
        int cellIdxK, cellIdxJ, cellIdxI;
        OneDToThreeDIndex(cellIdx, out cellIdxI, out cellIdxJ, out cellIdxK);

        //checks the act from the surrounding cells of a cell;
        //if a neighboring cell is out the grid, don't count that 

        return (cellIdxI + 1 < _CAGridSettings.Columns && _Act[ThreeDToOneDIndex(cellIdxI + 1, cellIdxJ, cellIdxK)])
            || (cellIdxI > 0 && _Act[ThreeDToOneDIndex(cellIdxI - 1, cellIdxJ, cellIdxK)])

            || (cellIdxJ + 1 < _CAGridSettings.Rows && _Act[ThreeDToOneDIndex(cellIdxI, cellIdxJ + 1, cellIdxK)])
            || (cellIdxJ > 0 && _Act[ThreeDToOneDIndex(cellIdxI, cellIdxJ - 1, cellIdxK)])

            || (cellIdxK + 1 < _CAGridSettings.Depth && _Act[ThreeDToOneDIndex(cellIdxI, cellIdxJ, cellIdxK + 1)])
            || (cellIdxK > 0 && _Act[ThreeDToOneDIndex(cellIdxI, cellIdxJ, cellIdxK - 1)])

            || (cellIdxI + 2 < _CAGridSettings.Columns && _Act[ThreeDToOneDIndex(cellIdxI + 2, cellIdxJ, cellIdxK)])
            || (cellIdxI > 1 && _Act[ThreeDToOneDIndex(cellIdxI - 2, cellIdxJ, cellIdxK)])

            || (cellIdxJ + 2 < _CAGridSettings.Rows && _Act[ThreeDToOneDIndex(cellIdxI, cellIdxJ + 2, cellIdxK)])
            || (cellIdxJ > 1 && _Act[ThreeDToOneDIndex(cellIdxI, cellIdxJ - 2, cellIdxK)])

            || (cellIdxK > 1 && _Act[ThreeDToOneDIndex(cellIdxI, cellIdxJ, cellIdxK - 2)]);
    }


    //3d array index to 1d array index https://stackoverflow.com/questions/13894028/efficient-way-to-compute-3d-indexes-from-1d-array-representation
    public int ThreeDToOneDIndex(int i, int j, int k)
    {
        return i + j * _CAGridSettings.Columns + k * _CAGridSettings.Columns * _CAGridSettings.Rows;
    }

    //1d index to 3d index https://stackoverflow.com/questions/13894028/efficient-way-to-compute-3d-indexes-from-1d-array-representation
    public void OneDToThreeDIndex(int cellIdx, out int i, out int j, out int k)
    {
        k = cellIdx / (_CAGridSettings.Columns * _CAGridSettings.Rows);
        cellIdx -= k * _CAGridSettings.Columns * _CAGridSettings.Rows;
        j = cellIdx / _CAGridSettings.Columns;
        cellIdx -= j * _CAGridSettings.Columns;
        i = cellIdx / 1;
    }
}