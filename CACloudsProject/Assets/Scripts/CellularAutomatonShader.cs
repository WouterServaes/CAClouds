//These helped:
//https://catlikecoding.com/unity/tutorials/basics/compute-shaders/

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;


public class CellularAutomatonShader : MonoBehaviour
{
    //compute shader
    [SerializeField] ComputeShader _ComputeShader = null;

    //buffers
    //state buffers
    private ComputeBuffer _ActBuffer, _CldBuffer, _HumBuffer;
    private ComputeBuffer _ActNextBuffer, _CldNextBuffer, _HumNextBuffer;
    //buffer Ids 
    private int _CldBufferId, _HumBufferId, _ActBufferId, _CldNextBufferId, _HumNextBufferId, _ActNextBufferId;
    //other buffers
    private ComputeBuffer _CloudPositions;
    //int variable buffer holds a set amount of integers, [0] = generation counter, [1] = cloud count
    private ComputeBuffer _IntVariableBuffer;
    //buffer Ids
    private int _CloudPositionsId, _IntVariableId;

    //kernel IDs
    private int _ProcessCellsKernel;
    private int _InitCellsKernel;

    //other IDs
    private int _CellCountId,
        _ColumnsId,
        _RowsId,
        _DepthId,
        _CellHeightId,
        _CABottomPositionId,
        _NormalWindSpeedId,
        _PExtId,
        _PHumId,
        _PActId,
        _WindStartGenId,
        _ExtStartGenId;

    private int _CloudCount = 0;
    private int _GenerationCount = 0;
    //Total int: the amount of integers required to store the states of the grid: 500 cells take up 15.625 integers, so 16 integers are required.
    private int _TotalInts;
    private int TotalInts
    {
        get
        {
            if (_TotalInts == 0)
                _TotalInts = Mathf.CeilToInt(_CAGridSettings.TotalCells / 32f);
            return _TotalInts;
        }
    }

    //CA Settings
    [Header("settings")]
    [SerializeField] private CASettings _CASettings = null;
    [SerializeField] private CAGridSettings _CAGridSettings = null;
    [SerializeField] private WindSettings _WindSettings = null;

    //cloud visuals
    [Header("cloud visuals")]
    [SerializeField] private Mesh _CloudMesh = null;
    [SerializeField] private Material _CloudMaterial = null;

    

    //Invoked in CellularAutomatonEditor by pressing GUI buttons
    public UnityAction<bool> PauseContinueAction; //pauses and continues ca, true = paused | false = not paused
    public UnityAction ResetAction; //resets ca

    //Update timer
    private float _ElapsedSec = 0f;
    private bool _IsPaused = true;

    //Metric saver
    private Metrics _Metrics = null;

    //info properties
    public int CAMemoryCount => (_CAGridSettings == null) ? 0 : _CAGridSettings.TotalCells * 3;
    public int CellMemoryCount => 3;
    public int CellCount => (_CAGridSettings == null) ? 0 : _CAGridSettings.TotalCells;
    public int GenerationCount => _GenerationCount;
    public float AvgCalcTime => (_Metrics) ? _Metrics.GetAverageCalcTime() : 0;
    public int CloudCount => _CloudCount;

    private void Start()
    {
        InitializeComputeShader();
        
        _Metrics = GetComponent<Metrics>();
        PauseContinueAction += PauseContinue;
        ResetAction += ResetCA;
    }
    private void Update()
    {
        if (!_IsPaused)
        {
            _ElapsedSec += Time.deltaTime;
            if (_ElapsedSec >= _CASettings.MinimumUpdateTime)
            {
                _ElapsedSec = 0f;
                UpdateCAShader();

            }
        }
        DrawClouds();
    }

    //sets id to the shader property name id 
    private void SetPropId(out int id, string propName)
    {
        id = Shader.PropertyToID(propName);
    }

    //initializes the buffers and sets the property Ids
    private void InitializeComputeShader()
    {
        //initializing buffers
        //CA buffers
        _ActBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _HumBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _CldBuffer = new ComputeBuffer(TotalInts, sizeof(int));  
        _ActNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _HumNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _CldNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));  

        //other buffers
        _CloudPositions = new ComputeBuffer(_CAGridSettings.TotalCells, sizeof(float)*3);
        _IntVariableBuffer = new ComputeBuffer(2, sizeof(int));
        //saving Property IDs
        //CA buffers
        SetPropId(out _CldBufferId, "_Cld");
        SetPropId(out _HumBufferId, "_Hum");
        SetPropId(out _ActBufferId, "_Act");
        SetPropId(out _CldNextBufferId, "_CldNext");
        SetPropId(out _HumNextBufferId, "_HumNext");
        SetPropId(out _ActNextBufferId, "_ActNext");

        //kernels
        _ProcessCellsKernel = _ComputeShader.FindKernel("CSProcessCells");
        _InitCellsKernel = _ComputeShader.FindKernel("CSInitializeCells");

        //other
        SetPropId(out _CloudPositionsId, "_CloudPositions");
        SetPropId(out _IntVariableId, "_IntVariables");
        SetPropId(out _CellCountId, "_CellCount");
        SetPropId(out _ColumnsId, "_Columns");
        SetPropId(out _RowsId, "_Rows");
        SetPropId(out _DepthId, "_Depth");
        SetPropId(out _CellHeightId, "_CellHeight");
        SetPropId(out _CABottomPositionId, "_CABottomPosition");
        SetPropId(out _NormalWindSpeedId, "_NormalWindSpeed");
        SetPropId(out _PExtId, "_PExt");
        SetPropId(out _PHumId, "_PHum");
        SetPropId(out _PActId, "_PAct");
        SetPropId(out _WindStartGenId, "_WindStartGen");
        SetPropId(out _ExtStartGenId, "_ExtStartGen");
    }

    //dispatches the process cell kernel
    private void UpdateCAShader()
    {
        _Metrics.StartCalcTimer(_GenerationCount+1);
        //buffers
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _CloudPositionsId, _CloudPositions);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _IntVariableId, _IntVariableBuffer);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _CldBufferId, _CldBuffer);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _HumBufferId, _HumBuffer);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _ActBufferId, _ActBuffer);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _CldNextBufferId, _CldNextBuffer);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _HumNextBufferId, _HumNextBuffer);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _ActNextBufferId, _ActNextBuffer);

        //variables
        _ComputeShader.SetInt(_CellCountId, _CAGridSettings.TotalCells);
        _ComputeShader.SetInt(_ColumnsId, _CAGridSettings.Columns);
        _ComputeShader.SetInt(_RowsId, _CAGridSettings.Rows);
        _ComputeShader.SetInt(_DepthId, _CAGridSettings.Depth);
        _ComputeShader.SetFloat(_CellHeightId, _CAGridSettings.CellHeight);
        _ComputeShader.SetFloat(_CABottomPositionId, transform.position.y);

        //probability variables
        _ComputeShader.SetFloat(_PExtId, _CASettings.ExtProbability);
        _ComputeShader.SetFloat(_PHumId, _CASettings.HumProbability);
        _ComputeShader.SetFloat(_PActId, _CASettings.ActProbability);

        //generation variables
        _ComputeShader.SetInt(_WindStartGenId, _CASettings.WindStartGeneration);
        _ComputeShader.SetInt(_ExtStartGenId, _CASettings.ExtStartGeneration);
        
        //Dispatching process cells kernel of shader
        _ComputeShader.Dispatch(_ProcessCellsKernel, 1,1,1);


        GetIntVariablesFromBuffer();
        _Metrics.StopCalcTimer(_GenerationCount);
        
        
    }

    private void GetIntVariablesFromBuffer()
    {
        int[] intVariables = new int[2];
        _IntVariableBuffer.GetData(intVariables);
        //generation counter
        _GenerationCount = intVariables[0];
        //cloud count
        _CloudCount = intVariables[1];
    }
    private void DrawClouds()
    {
        if (_CloudCount > 0)
        {
            _CloudMaterial.SetBuffer(_CloudPositionsId, _CloudPositions);
            Graphics.DrawMeshInstancedProcedural(_CloudMesh, 0, _CloudMaterial
                , _CAGridSettings.GridBouds, _CloudCount);
        }
    }
    private void OnDestroy()
    {
        DestroyBuffer(_ActBuffer);
        DestroyBuffer(_HumBuffer);
        DestroyBuffer(_CldBuffer);
        DestroyBuffer(_ActNextBuffer);
        DestroyBuffer(_HumNextBuffer);
        DestroyBuffer(_CldNextBuffer);
        DestroyBuffer(_CloudPositions);
        DestroyBuffer(_IntVariableBuffer);
    }

    private void DestroyBuffer(ComputeBuffer buffer)
    {
        buffer.Release();
        buffer = null;
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
        _Metrics.ClearMetrics();
        InitializeCA();
    }

    public void InitializeCA()
    {
        
        //reset all buffers with 0
        int[] initialInts = new int[TotalInts];
        for (uint idx = 0; idx < TotalInts; idx++)
            initialInts[idx] = 0x000000;
        
        //_ActBuffer.SetData(initialInts);
        //_HumBuffer.SetData(initialInts);
        _CldBuffer.SetData(initialInts);
        _ActNextBuffer.SetData(initialInts);
        _HumNextBuffer.SetData(initialInts);
        _CldNextBuffer.SetData(initialInts);

        //reset generation counter
        _IntVariableBuffer.SetData(new int[2] { 0 ,0});

        //set variables of compute shader that initialize kernel uses
        //_ComputeShader.SetFloat("_PActStart", _CASettings.ActProbabilityAtStart);
        //_ComputeShader.SetFloat("_PHumStart", _CASettings.HumProbabilityAtStart);
        //_ComputeShader.SetInt(_CellCountId, _CAGridSettings.TotalCells);
        //
        //_ComputeShader.SetFloat(_TimeSecondsId, UnityEngine.Random.Range(.1f, 10f));
        ////initialize kernel uses these buffers
        //_ComputeShader.SetBuffer(_InitCellsKernel, _HumBufferId, _HumBuffer);
        //_ComputeShader.SetBuffer(_InitCellsKernel, _ActBufferId, _ActBuffer);
        //_ComputeShader.SetBuffer(_InitCellsKernel, _GenCounterId, _GenCounter);
        //_ComputeShader.SetInt(_ColumnsId, _CAGridSettings.Columns);
        //_ComputeShader.SetInt(_RowsId, _CAGridSettings.Rows);
        //_ComputeShader.SetInt(_DepthId, _CAGridSettings.Depth);
        //
        ////Dispatch initialize kernel
        //_ComputeShader.Dispatch(_InitCellsKernel, 1,1,1);
        _Metrics.StartCalcTimer();
        bool[] initialHum = new bool[_CAGridSettings.TotalCells];
        bool[] initialAct = new bool[_CAGridSettings.TotalCells];
        for (int idx = 0; idx < _CAGridSettings.TotalCells; idx++)
        {
            bool humAtStart = UnityEngine.Random.Range(0f, 1f) <= _CASettings.HumProbabilityAtStart;
            initialHum[idx] = humAtStart;
            if (!humAtStart)
                initialAct[idx] = UnityEngine.Random.Range(0f, 1f) <= _CASettings.ActProbabilityAtStart;
        }

        BitArray tempBitArray = new BitArray(initialHum);
        tempBitArray.CopyTo(initialInts, 0);
        _HumBuffer.SetData(initialInts);

        tempBitArray = new BitArray(initialAct);
        tempBitArray.CopyTo(initialInts, 0);
        _ActBuffer.SetData(initialInts);

        _Metrics.StopCalcTimer();
    }

    //converts a compute ca state buffer to a bool array
    private void DebugCaArray(ComputeBuffer buffer)
    {
        int[] testInts = new int[TotalInts];
        buffer.GetData(testInts);
        BitArray testBitArray = new BitArray(testInts);
        bool[] testBools = new bool[TotalInts * 32];
        testBitArray.CopyTo(testBools, 0);
    }

    private void DebugIntArray(ComputeBuffer buffer, int size)
    {
        int[] testInts = new int[size];
        buffer.GetData(testInts);
    }
}
