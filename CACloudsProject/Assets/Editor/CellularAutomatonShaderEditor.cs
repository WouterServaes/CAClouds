using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CellularAutomatonShader))]
public class CellularAutomatonShaderEditor : Editor
{
    private string _PauseContinueButtonText = "Continue";
    private bool _IsCaPaused = true;
    private bool _HasStarted = false;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CellularAutomatonShader cas = (CellularAutomatonShader)target;
        if (Application.isPlaying)
        {
            if (_HasStarted)
            {
                PauseContinueButton(cas);
                ResetButton(cas);
            }
            else
            {
                StartButton(cas);
            }
        }
        CAInfo(cas);
    }

    private void StartButton(CellularAutomatonShader cas)
    {
        if (GUILayout.Button("Start"))
        {
            _HasStarted = true;
            cas.ResetAction.Invoke();
        }
    }

    private void PauseContinueButton(CellularAutomatonShader cas)
    {
        if (GUILayout.Button(_PauseContinueButtonText))
        {
            _IsCaPaused = !_IsCaPaused;
            if (_IsCaPaused) _PauseContinueButtonText = "Continue";
            else _PauseContinueButtonText = "Pause";

            cas.PauseContinueAction.Invoke(_IsCaPaused);
        }
    }

    private void ResetButton(CellularAutomatonShader cas)
    {
        if (GUILayout.Button("Reset CA"))
        {
            cas.ResetAction.Invoke();
        }
    }

    private void CAInfo(CellularAutomatonShader cas)
    {
        GUILayout.Space(10);
        GUILayout.Label(string.Format("CA generation count: {0}", cas.GenerationCount));
        GUILayout.Label(string.Format("CA cell count: {0}", cas.CellCount));
        GUILayout.Label(string.Format("Cloud count: {0}", cas.CloudCount));
        GUILayout.Label(string.Format("CA memory usage: {0} bits", cas.CAMemoryCount));
        GUILayout.Label(string.Format("CA memory usage both arrays: {0} bits", cas.CAMemoryCount * 2));
        GUILayout.Label(string.Format("Cell memory usage: {0} bits", cas.CellMemoryCount));
        GUILayout.Label(string.Format("Average calculation time: {0} seconds", cas.AvgCalcTime));
    }
}
