//These helped:
//https://catlikecoding.com/unity/tutorials/basics/compute-shaders/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class CellularAutomatonShader : MonoBehaviour
{
    //compute shader
    [SerializeField] ComputeShader _ComputeShader = null;
    //buffers
    private ComputeBuffer _ActBuffer, _CldBuffer, _HumBuffer;
    private ComputeBuffer _ActNextBuffer, _CldNextBuffer, _HumNextBuffer;
    private ComputeBuffer _CloudPositions;
    private ComputeBuffer _GenCounter;
    private ComputeBuffer TestBuffer;
    private int TotalInts => _CAGridSettings.TotalCells/32;
    //kernel IDs
    private int _ProcessCellsKernel;
    private int _InitCellsKernel;
    //other IDs
    private int _CloudPositionsId;
    private int _RandomSeedId;
    private int _GenCounterId;
    private int _CldBufferId;

    //CA Settings
    [SerializeField] private CASettings _CASettings = null;
    [SerializeField] private Mesh _CloudMesh;
    [SerializeField] private Material _CloudMaterial;
    //grid settings
    [SerializeField] private CAGridSettings _CAGridSettings = null;

    //Invoked in CellularAutomatonEditor by pressing GUI buttons
    public UnityAction<bool> PauseContinueAction; //pauses and continues ca, true = paused | false = not paused

    public UnityAction ResetAction; //resets ca

    //Update timer
    private float _ElapsedSec = 0f;
    private bool _IsPaused = true;

    //Metric saver
    private Metrics _Metrics = null;

    private void Start()
    {
        InitializeComputeShader();
        InitializeCA();
        _Metrics = GetComponent<Metrics>();
        PauseContinueAction += PauseContinue;
        ResetAction += ResetCA;
        PauseContinue(false);
        
    }
    private void Update()
    {
        UpdateCAShader();
        DrawClouds();

        if (!_IsPaused)
        {
            _ElapsedSec += Time.deltaTime;
            if (_ElapsedSec >= _CASettings.MinimumUpdateTime)
            {
                _ElapsedSec = 0f;
               
                
            }
        }
        
    }

    //sets all the variables and buffers of the shader
    private void InitializeComputeShader()
    {
        //initializing buffers
        //CA buffers
        //_ActBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        //_HumBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _CldBuffer = new ComputeBuffer(TotalInts, sizeof(int));  
        //_ActNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        //_HumNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        //_CldNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));  

        //other buffers
        _CloudPositions = new ComputeBuffer(_CAGridSettings.TotalCells, sizeof(float)*3);
        _GenCounter = new ComputeBuffer(1, sizeof(int));
        TestBuffer = new ComputeBuffer(_CAGridSettings.TotalCells, sizeof(int));
        //generation variables
        //_ComputeShader.SetInt("_WindStartGen", _CASettings.WindStartGeneration);
        //_ComputeShader.SetInt("_ExtStartGen", _CASettings.ExtStartGeneration);

        //probability variables
        //_ComputeShader.SetFloat("_PActStart", _CASettings.ActProbabilityAtStart);
        //_ComputeShader.SetFloat("_PHumStart", _CASettings.HumProbabilityAtStart);
        //_ComputeShader.SetFloat("_PExt", _CASettings.ExtProbability);
        //_ComputeShader.SetFloat("_PHum", _CASettings.HumProbability);
        //_ComputeShader.SetFloat("_PAct", _CASettings.ActProbability);


        //saving Property IDs
        //CA buffers
        _CldBufferId = Shader.PropertyToID("_Cld");
        //kernels
        _ProcessCellsKernel = _ComputeShader.FindKernel("CSProcessCells");
        _InitCellsKernel = _ComputeShader.FindKernel("CSInitializeCells");
        //other
        _CloudPositionsId = Shader.PropertyToID("_CloudPositions");
        _RandomSeedId = Shader.PropertyToID("_RandomSeed");
        _GenCounterId = Shader.PropertyToID("_GenCounter");
    }

    private void UpdateCAShader()
    {
        //buffers
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _CloudPositionsId, _CloudPositions);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _GenCounterId, _GenCounter);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, _CldBufferId, _CldBuffer);
        _ComputeShader.SetBuffer(_ProcessCellsKernel, "TestBuffer", TestBuffer);
        //variables
        _ComputeShader.SetInt("_CellCount", _CAGridSettings.TotalCells);
        _ComputeShader.SetInt("_Columns", _CAGridSettings.Columns);
        _ComputeShader.SetInt("_Rows", _CAGridSettings.Rows);
        _ComputeShader.SetInt("_Depth", _CAGridSettings.Depth);
        _ComputeShader.SetFloat("_CellHeight", _CAGridSettings.CellHeight);
        _ComputeShader.SetFloat(_RandomSeedId, Time.time);
       

        _ComputeShader.Dispatch(_ProcessCellsKernel, 1,1,1);

        //generation counter
        int[] counter = new int[1];
        _GenCounter.GetData(counter);
        Debug.Log(counter[0]);

        int[] test = new int[_CAGridSettings.TotalCells];
        TestBuffer.GetData(test);
    }

    private void DrawClouds()
    {
        _CloudMaterial.SetBuffer(_CloudPositionsId, _CloudPositions);
        Graphics.DrawMeshInstancedProcedural(_CloudMesh, 0, _CloudMaterial
            , _CAGridSettings.GridBouds, _CloudPositions.count);
    }
    private void OnDestroy()
    {
        //DestroyBuffer(_ActBuffer);
        //DestroyBuffer(_HumBuffer);
        DestroyBuffer(_CldBuffer);
        DestroyBuffer(TestBuffer);
        //DestroyBuffer(_ActNextBuffer);
        //DestroyBuffer(_HumNextBuffer);
        //DestroyBuffer(_CldNextBuffer);
        DestroyBuffer(_CloudPositions);
        DestroyBuffer(_GenCounter);
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
        InitializeCA();
    }

    public void InitializeCA()
    {
        int[] resetArray = new int[TotalInts];
        for (int idx = 0; idx < TotalInts; idx++)
        {
            resetArray[idx] = 1;
            //resetArray[idx] = 0x000000;
        }

        BitArray b = new BitArray(resetArray);
        bool[] a = new bool[_CAGridSettings.TotalCells];
        b.CopyTo(a, 0);
        //_ActBuffer.SetData(resetArray);
        //_HumBuffer.SetData(resetArray);
        _CldBuffer.SetData(resetArray);
        //_ActNextBuffer.SetData(resetArray);
        //_HumNextBuffer.SetData(resetArray);
        //_CldNextBuffer.SetData(resetArray);
        _GenCounter.SetData(new int[1] { 0 });
    }
}
