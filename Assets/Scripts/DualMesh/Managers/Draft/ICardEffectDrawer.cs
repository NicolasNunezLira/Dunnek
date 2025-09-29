using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DraftSystem;

[CustomPropertyDrawer(typeof(ICardEffect), true)]
public class ICardEffectDrawer : PropertyDrawer
{
    private static Type[] effectTypes;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (effectTypes == null)
        {
            effectTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(ICardEffect).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToArray();
        }

        EditorGUI.BeginProperty(position, label, property);

        if (property.managedReferenceValue == null)
        {
            var typeNames = effectTypes.Select(t => t.Name).ToArray();
            int selected = EditorGUI.Popup(position, "Select Effect", -1, typeNames);
            if (selected >= 0)
            {
                property.managedReferenceValue = Activator.CreateInstance(effectTypes[selected]);
            }
        }
        else
        {
            // Mostrar un popup para cambiar el tipo si se quiere
            var currentType = property.managedReferenceValue.GetType();
            var typeNames = effectTypes.Select(t => t.Name).ToArray();
            int currentIndex = Array.IndexOf(effectTypes, currentType);

            Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int newIndex = EditorGUI.Popup(typeRect, "Effect Type", currentIndex, typeNames);
            if (newIndex != currentIndex && newIndex >= 0)
            {
                property.managedReferenceValue = Activator.CreateInstance(effectTypes[newIndex]);
            }

            // Ahora dibujar los campos de la clase concreta
            EditorGUI.indentLevel++;
            var fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2,
                                     position.width, position.height - EditorGUIUtility.singleLineHeight - 2);
            EditorGUI.PropertyField(fieldRect, property, true);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }
}