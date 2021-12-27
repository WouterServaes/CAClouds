using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Metrics))]
public class MetricsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Metrics metrics = (Metrics)target;
        PrintMetricsButton(metrics);
    }

    private void PrintMetricsButton(Metrics metrics)
    {
        if (GUILayout.Button("Print metrics data"))
            metrics.PrintMetrics();
    }
}
