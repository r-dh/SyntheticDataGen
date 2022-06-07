#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shows the dataset in the Unity editor.
/// </summary>
public class Daer : EditorWindow
{
    /// <summary>
    /// Shows the dataset in the Unity editor.
    /// </summary>
    [MenuItem("Dataset/DAE Research")]
    static void Init()
    {
        Daer window = (Daer)GetWindow(typeof(Daer));
        window.minSize = new Vector2(1024, 244);
        window.maxSize = new Vector2(1024, 244);
    }

    private Texture2D m_Logo = null;
    void OnEnable()
    {
        m_Logo = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Editor/Images/DAER.png", typeof(Texture2D));
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        GUILayout.Label(m_Logo, GUILayout.MaxHeight(244));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();
    }
}
#endif