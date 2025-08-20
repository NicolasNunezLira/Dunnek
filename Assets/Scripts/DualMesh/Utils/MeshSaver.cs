#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MeshSaver
{
#if UNITY_EDITOR
    public static void SaveMeshAsAsset(Mesh mesh, string path = "Assets/GeneratedMesh.asset")
    {
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#endif
}