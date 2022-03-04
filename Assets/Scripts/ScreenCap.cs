#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ScreenshotGrabber
{
    [MenuItem("Screenshot/Grab")]
    public static void Grab()
    {
        ScreenCapture.CaptureScreenshot(@"C:\Users\remy.dheygere\Documents\Screenshot.png", 3);
    }
}
#endif
