using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CellularAutomaton))]
public class CellularAutomatonEditor : Editor
{
    private string _PauseContinueButtonText = string.Format("Start");
    private bool _IsCaPaused = true;

    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CellularAutomaton ca = (CellularAutomaton)target;
        if (Application.isPlaying)
        {
            PauseContinueButton(ca);
            ResetButton(ca);
        }
            CAInfo(ca);
    }

    private void PauseContinueButton(CellularAutomaton ca)
    {
        if (GUILayout.Button(_PauseContinueButtonText))
        {
            _IsCaPaused = !_IsCaPaused;
            if (_IsCaPaused) _PauseContinueButtonText = string.Format("Continue");
            else _PauseContinueButtonText = string.Format("Pause");

            ca.PauseContinueAction.Invoke(_IsCaPaused);
        }
    }

    private void ResetButton(CellularAutomaton ca)
    {
        if(GUILayout.Button("Reset CA"))
        {
            ca.ResetAction.Invoke();
        }
    }

    private void CAInfo(CellularAutomaton ca)
    {
        GUILayout.Space(10);
        GUILayout.Label(string.Format("CA generation count: {0}", ca.GenerationCount));
        GUILayout.Label(string.Format("CA cell count: {0}", ca.CellCount));
        GUILayout.Label(string.Format("Cloud count: {0}", ca.CloudCount));
        GUILayout.Label(string.Format("CA memory usage: {0} bits", ca.CAMemoryCount));
        GUILayout.Label(string.Format("CA memory usage both arrays: {0} bits", ca.CAMemoryCount * 2));
        GUILayout.Label(string.Format("Cell memory usage: {0} bits", ca.CellMemoryCount));
        GUILayout.Label(string.Format("Average calculation time: {0} seconds", ca.AvgCalcTime));
    }
}
