using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CAGridSettings))]
public class GridEditorSettings : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CAGridSettings gridSettings = (CAGridSettings)target;
        if(GUILayout.Button("Refresh grid"))
        {
            gridSettings.UpdatedGridSettingsAction.Invoke();
        }
    }
}
