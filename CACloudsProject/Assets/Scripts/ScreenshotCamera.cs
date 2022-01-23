using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
public class ScreenshotCamera : MonoBehaviour
{

    private Camera _Camera;
    public UnityAction TakeScreenshotAction;
    private string ScreenshotName => string.Format("Screenshots/Screenshot_{0}.png", Time.time);
    void Start()
    {
        _Camera = GetComponent<Camera>();
        TakeScreenshotAction += TakeScreenshot;
    }

    //https://answers.unity.com/questions/22954/how-to-save-a-picture-take-screenshot-from-a-camer.html
    private void TakeScreenshot()
    {
        ScreenCapture.CaptureScreenshot(ScreenshotName);
        Debug.Log("screenshot taken");
    }
}
