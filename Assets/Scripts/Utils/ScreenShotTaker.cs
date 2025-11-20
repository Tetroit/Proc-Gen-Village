using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ScreenShotTaker : MonoBehaviour
{
    public string name;
    [ContextMenu("Take Screenshot")]
    public void TakeScreenShot()
    {
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filePath = $"Recordings/{name}.png";
        ScreenCapture.CaptureScreenshot(filePath);
        Debug.Log("Screenshot saved to: " + filePath);
    }
}
