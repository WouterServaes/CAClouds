using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class CellularAutomaton : MonoBehaviour
{
    //settings
    [Header("settings")]
    [SerializeField] private CASettings _CASettings = null;
    [SerializeField] private WindSettings _WindSettings = null;
    [SerializeField] private CAGridSettings _CAGridSettings = null;
    [SerializeField] private ScreenshotCamera _ScreenshotCamera = null;
    public ScreenshotCamera ScreenshotCamera => _ScreenshotCamera;

    //cloud visuals
    [Header("cloud visuals")]
    [SerializeField] private Mesh _CloudMesh = null;
    [SerializeField] private Material _CloudMaterial = null;

    

    private ComputeBuffer _CloudBuffer;
    private int _CloudBufferId;
    private List<float3> _CloudPositions = new List<float3>();
    private int _CloudCount = 0;

    //pausing
    private bool _IsPaused = true;

    //state containers
    private BitArray _Act, _Cld, _Hum, _NextAct, _NextCld, _NextHum;

    //counters
    private int _GenerationCount = 0;

    private int _StateCount = 3;

    //Invoked in CellularAutomatonEditor by pressing GUI buttons
    public UnityAction<bool> PauseContinueAction; //pauses and continues ca, true = paused | false = not paused

    public UnityAction ResetAction; //resets ca

    public UnityAction NextGenerationAction; //calculates and shows one generation

    private bool _StepThroughGen = false;
    //Update timer
    private float _ElapsedSec = 0f;

    //Metric saver
    private Metrics _Metrics = null;

    //info properties
    public int CAMemoryCount => (_CAGridSettings == null) ? 0 : _CAGridSettings.TotalCells * _StateCount;

    public int CellMemoryCount => _StateCount;
    public int CellCount => (_CAGridSettings == null) ? 0 : _CAGridSettings.TotalCells;
    public int GenerationCount => _GenerationCount;
    public float AvgCalcTime => (_Metrics) ? _Metrics.GetAverageCalcTime() : 0;
    public int CloudCount => _CloudCount;

    private void Start()
    {
        //get metrics component
        _Metrics = GetComponent<Metrics>();

        //assign events
        PauseContinueAction += PauseContinue;
        ResetAction += ResetCA;
        NextGenerationAction += NextGeneration;

        //set cloud position buffer
        _CloudBuffer = new ComputeBuffer(_CAGridSettings.TotalCells, sizeof(float) * 3);
        _CloudBufferId = Shader.PropertyToID("_CloudPositions");
    }

    private void NextGeneration()
    {
        _StepThroughGen = true;
        _IsPaused = true;
    }

    private void OnDestroy()
    {
        //release the buffer at destroy to avoid leaks
        _CloudBuffer.Release();
        _CloudBuffer = null;
    }

    //initializes the CA state containers and sets the initial states
    public void InitializeCA()
    {
        _Metrics.StartCalcTimer();
        _Act = new BitArray(_CAGridSettings.TotalCells, false);
        _Cld = new BitArray(_CAGridSettings.TotalCells, false);
        _Hum = new BitArray(_CAGridSettings.TotalCells, false);

        _NextAct = new BitArray(_CAGridSettings.TotalCells, false);
        _NextCld = new BitArray(_CAGridSettings.TotalCells, false);
        _NextHum = new BitArray(_CAGridSettings.TotalCells, false);

        _GenerationCount = 0;
        SetInitialStates();

        Debug.Log(string.Format("Initialized {0} cells", _NextAct.Count));
    }

    //sets the initial states for hum and act
    private void SetInitialStates()
    {
        for (int idx = 0; idx < _CAGridSettings.TotalCells; idx++)
        {
            bool humAtStart = Random.Range(0f, 1f) <= _CASettings.HumProbabilityAtStart;
            _Hum[idx] = humAtStart;
            if (!humAtStart)
                _Act[idx] = Random.Range(0f, 1f) <= _CASettings.ActProbabilityAtStart;
        }
        _Metrics.StopCalcTimer();
    }

    //Pauses and continues simulation
    private void PauseContinue(bool isPaused)
    {
        _IsPaused = isPaused;

        if (_IsPaused) Debug.Log("Paused");
        else Debug.Log("Continuing");
    }

    //resets the CA
    private void ResetCA()
    {
        Debug.Log("Reset CA");
        _Metrics.ClearMetrics();
        InitializeCA();
    }

    private void Update()
    {
        if (_StepThroughGen)
        {
            _StepThroughGen = false;
            UpdateCA();
            _ElapsedSec = 0f;
        }

        if (!_IsPaused)
        {
            _ElapsedSec += Time.deltaTime;
            if (_ElapsedSec >= _CASettings.MinimumUpdateTime)
            {
                _ElapsedSec = 0f;
                UpdateCA();
            }
        }

        DrawCloudCells();
    }

    //Updates the CA, goes over each cell, calls the rule functions for each cell and saves the position of the cell if it is a cloud
    private void UpdateCA()
    {
        _CloudCount = 0;
        _Metrics.StartCalcTimer();
        //---start metrics---
        //go over each cell
        for (int cellIdx = 0; cellIdx < _CAGridSettings.TotalCells; cellIdx++)
        {
            //go over rules for cell
            processGrowthRules(cellIdx);
            if (_GenerationCount >= _CASettings.ExtStartGeneration) ProcessExtRules(cellIdx);
            if (_GenerationCount >= _CASettings.WindStartGeneration) ProcessWind(cellIdx);

            //saving the cell position of cloud cells so CAGrid can visualize those
            if (_NextCld[cellIdx])
                AddCloudPosition(cellIdx);
        }

        //copy the t+1 array into t
        //.Clone -> BitArray is a reference type
        _Act = (BitArray)_NextAct.Clone();
        _Cld = (BitArray)_NextCld.Clone();
        _Hum = (BitArray)_NextHum.Clone();

        //---stop metrics---
        _GenerationCount++;
        _Metrics.StopCalcTimer(_GenerationCount);
    }

    //Draw the cloud cells using the gpu with Graphics.DrawMeshInstancedProcedural
    private void DrawCloudCells()
    {
        if (_CloudCount > 0)
        {
            _CloudBuffer.SetData(_CloudPositions);
            _CloudMaterial.SetBuffer(_CloudBufferId, _CloudBuffer);

            Graphics.DrawMeshInstancedProcedural(_CloudMesh, 0, _CloudMaterial, _CAGridSettings.GridBouds, _CloudCount);
            _CloudPositions.Clear();
        }
    }

    //Adds the position of given cell to cloud positions container
    private void AddCloudPosition(int cellIdx)
    {
        _CloudCount++;
        OneDToThreeDIndex(cellIdx, out int i, out int j, out int k);
        _CloudPositions.Add(new float3(
            i * _CAGridSettings.CellHeight,
            j * _CAGridSettings.CellHeight,
            k * _CAGridSettings.CellHeight
        ));
    }

    //processes the growth rules for cell
    private void processGrowthRules(int cellIdx)
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

    //processes the extinction and regeneration rules for cell
    private void ProcessExtRules(int cellIdx)
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

    //processes the wind, shifts cell in a set direction at a set speed
    private void ProcessWind(int cellIdx)
    {
        OneDToThreeDIndex(cellIdx, out int cellIdxI, out int cellIdxJ, out int cellIdxK);

        //get the displacement according to the height
        float cellHeight = GetCellHeightInWorld(cellIdxK);
        int cellDisplacementByWind = WindHelper.GetWindSpeedCellDisplacementAtHeight();

        if (cellIdxI - cellDisplacementByWind >= 0)
        {
            int cellIdxDisplacementByWind = ThreeDToOneDIndex(cellIdxI - cellDisplacementByWind, cellIdxJ, cellIdxK);

            //hum
            _NextHum[cellIdx] = _Hum[cellIdxDisplacementByWind];

            //cld
            _NextCld[cellIdx] = _Cld[cellIdxDisplacementByWind];

            //act
            _NextAct[cellIdx] = _Act[cellIdxDisplacementByWind];
        }
        else if (cellIdxI == 0)
        {
            _NextHum[cellIdx] = false;
            _NextCld[cellIdx] = false;
            _NextAct[cellIdx] = false;
        }
    }

    //returns the height of the cell in the world
    private float GetCellHeightInWorld(int cellIdxK)
    {
        return transform.position.y + cellIdxK * _CAGridSettings.CellHeight;
    }

    //returns true if act of surrounding cell is true
    private bool GetActFromSurrounding(int cellIdx)
    {
        OneDToThreeDIndex(cellIdx, out int cellIdxI, out int cellIdxJ, out int cellIdxK);

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