//These helped:
//https://catlikecoding.com/unity/tutorials/basics/compute-shaders/

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
    private int TotalInts => _CAGridSettings.TotalCells/32;
    //kernel IDs
    private int _ProcessCellsKernel;
    private int _InitCellsKernel;
    //other IDs
    private int _CloudCountID;


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
        if (!_IsPaused)
        {
            _ElapsedSec += Time.deltaTime;
            if (_ElapsedSec >= _CASettings.MinimumUpdateTime)
            {
                _ElapsedSec = 0f;
                UpdateCAShader();

            }
        }
    }

    private void InitializeComputeShader()
    {
        //buffers & variables
        _ActBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _HumBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _CldBuffer = new ComputeBuffer(TotalInts, sizeof(int));  
        _ActNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _HumNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));
        _CldNextBuffer = new ComputeBuffer(TotalInts, sizeof(int));  

        _CloudPositions = new ComputeBuffer(TotalInts, sizeof(float)*3);

        _ComputeShader.SetBuffer(0, "_Act", _ActBuffer);
        _ComputeShader.SetBuffer(0, "_Hum", _HumBuffer);
        _ComputeShader.SetBuffer(0, "_Cld", _CldBuffer);
        _ComputeShader.SetBuffer(0, "_ActNext", _ActNextBuffer);
        _ComputeShader.SetBuffer(0, "_HumNext", _HumNextBuffer);
        _ComputeShader.SetBuffer(0, "_CldNext", _CldNextBuffer);
        _ComputeShader.SetBuffer(0, "_CloudPositions", _CloudPositions);

        _ComputeShader.SetInt("_CellCount", _CAGridSettings.TotalCells);
        _ComputeShader.SetInt("_Columns", _CAGridSettings.Columns);
        _ComputeShader.SetInt("_Rows", _CAGridSettings.Rows);
        _ComputeShader.SetInt("_Depth", _CAGridSettings.Depth);

        //generation variables
        _ComputeShader.SetInt("_WindStartGen", _CASettings.WindStartGeneration);
        _ComputeShader.SetInt("_ExtStartGen", _CASettings.ExtStartGeneration);
        
        
        //probability variables
        _ComputeShader.SetFloat("_PActStart", _CASettings.ActProbabilityAtStart);
        _ComputeShader.SetFloat("_PHumStart", _CASettings.HumProbabilityAtStart);
        _ComputeShader.SetFloat("_PExt", _CASettings.ExtProbability);
        _ComputeShader.SetFloat("_PHum", _CASettings.HumProbability);
        _ComputeShader.SetFloat("_PAct", _CASettings.ActProbability);

        _CloudCountID = Shader.PropertyToID("_CloudCount");
        //kernels
        _ProcessCellsKernel = _ComputeShader.FindKernel("CSProcessCells");
        _InitCellsKernel = _ComputeShader.FindKernel("CSInitializeCells");
    }

    private void UpdateCAShader()
    {
        _ComputeShader.SetFloat("_RandomSeed", Time.time);
        _ComputeShader.Dispatch(_ProcessCellsKernel, 1,1,1);
    }

    private void OnDestroy()
    {
        _ActBuffer.Release();
        _HumBuffer.Release();
        _CldBuffer.Release();
        _ActNextBuffer.Release();
        _HumNextBuffer.Release();
        _CldNextBuffer.Release();
        _CloudPositions.Release();
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
            resetArray[idx] = 0x000000;
        }

        _ActBuffer.SetData(resetArray);
        _HumBuffer.SetData(resetArray);
        _CldBuffer.SetData(resetArray);
        _ActNextBuffer.SetData(resetArray);
        _HumNextBuffer.SetData(resetArray);
        _CldNextBuffer.SetData(resetArray);

        _ComputeShader.SetBuffer(_InitCellsKernel, "_Act", _ActBuffer);
        _ComputeShader.SetBuffer(_InitCellsKernel, "_Hum", _HumBuffer);

        _ComputeShader.SetFloat("_RandomSeed", Time.time);
        _ComputeShader.Dispatch(_InitCellsKernel, 1,1,1);
    }
}
