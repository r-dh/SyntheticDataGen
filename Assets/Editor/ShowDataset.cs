#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ShowDataset
{
    [MenuItem("Dataset/Reveal")]
    public static void Reveal()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);// + "/SyntheticDatagen/");

    }
}
#endif