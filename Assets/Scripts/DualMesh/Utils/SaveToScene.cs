#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

public class SaveToScene : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Save Generated Objects To Scene")]
    void Save()
    {
        foreach (Transform child in transform)
        {
            EditorUtility.SetDirty(child.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}