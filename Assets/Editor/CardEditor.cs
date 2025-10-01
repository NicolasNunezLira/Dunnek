using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using DraftSystem;

[CustomEditor(typeof(Card))]
public class CardEditor : Editor
{
    private SerializedProperty effectsProp;
    private Type[] effectTypes;
    private string[] effectTypeNames;

    private void OnEnable()
    {
        effectsProp = serializedObject.FindProperty("effects");
        LoadEffectTypes();
    }

    private void LoadEffectTypes()
    {
        effectTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return new Type[0]; }
            })
            .Where(t =>
                typeof(ICardEffect).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract &&
                t.IsSerializable)
            .OrderBy(t => t.Name)
            .ToArray();

        effectTypeNames = effectTypes.Select(t => t.Name).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Resto de propiedades de la Card (excepto script y efectos)
        DrawPropertiesExcluding(serializedObject, "m_Script", "effects");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

        if (effectsProp == null)
        {
            EditorGUILayout.HelpBox("No se encontró la propiedad 'effects'.", MessageType.Error);
            return;
        }

        if (GUILayout.Button("Add Effect"))
        {
            ShowAddEffectMenu();
        }

        EditorGUILayout.Space();

        // Dibujar elementos de effects
        for (int i = 0; i < effectsProp.arraySize; i++)
        {
            SerializedProperty element = effectsProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            var instance = element.managedReferenceValue;
            string typeLabel = instance != null ? instance.GetType().Name : "<null>";
            EditorGUILayout.LabelField(typeLabel, EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Change Type"))
                ShowChangeTypeMenu(i);
            if (GUILayout.Button("Remove"))
            {
                element.managedReferenceValue = null;
                effectsProp.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            if (instance != null)
            {
                // Dibujar SOLO los hijos (campos del efecto concreto)
                SerializedProperty iterator = element.Copy();
                SerializedProperty endProperty = iterator.GetEndProperty();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
                {
                    if (iterator.propertyType == SerializedPropertyType.Generic)
                    {
                        // Asegura que listas como affectedBuildTypes se muestren correctamente
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    enterChildren = false;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Elemento sin instancia.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void ShowAddEffectMenu()
    {
        GenericMenu menu = new GenericMenu();
        foreach (var t in effectTypes)
        {
            menu.AddItem(new GUIContent(t.Name), false, () =>
            {
                effectsProp.arraySize++;
                var newElem = effectsProp.GetArrayElementAtIndex(effectsProp.arraySize - 1);
                newElem.managedReferenceValue = Activator.CreateInstance(t);
                serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }

    private void ShowChangeTypeMenu(int index)
    {
        GenericMenu menu = new GenericMenu();
        foreach (var t in effectTypes)
        {
            menu.AddItem(new GUIContent(t.Name), false, () =>
            {
                var elem = effectsProp.GetArrayElementAtIndex(index);
                elem.managedReferenceValue = Activator.CreateInstance(t);
                serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }
}
