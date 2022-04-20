#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shows the dataset in the Unity editor.
/// </summary>
public class ShowDataset
{
    /// <summary>
    /// Shows the dataset in the Unity editor.
    /// </summary>
    [MenuItem("Dataset/Reveal")]
    public static void Reveal()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);// + "/SyntheticDatagen/");
    }
}
#endif