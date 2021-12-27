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
}
