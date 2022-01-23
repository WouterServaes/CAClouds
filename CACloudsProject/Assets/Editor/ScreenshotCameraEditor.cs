using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScreenshotCamera))]
public class ScreenshotCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Take screenshot"))
            {
                ScreenshotCamera ssc = (ScreenshotCamera)target;
                ssc.TakeScreenshotAction.Invoke();
            }
        }
    }
}
