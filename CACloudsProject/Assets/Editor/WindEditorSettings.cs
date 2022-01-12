using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WindSettings))]
public class WindEditorSettings : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        RefreshWindSettings();


    }

    private void RefreshWindSettings()
    {
        if (Application.isPlaying)
        {
            WindSettings windSettings = (WindSettings)target;
            if (GUILayout.Button("Refresh wind settings"))
            {
                windSettings.UpdatedWind.Invoke();
            }
        }
    }
}
