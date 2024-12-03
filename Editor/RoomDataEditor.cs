using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Editor
{

    [CustomEditor(typeof(RoomData))]
    public class RoomDataEditor : UnityEditor.Editor
    {
        private RoomData _roomData;


        public override void OnInspectorGUI()
        {
            // Reference to serialized object
            serializedObject.Update();

            // Draw toggle for "LoadEnabledAtStart" using SerializedProperty
            SerializedProperty loadEnabledAtStartProp = serializedObject.FindProperty(nameof(RoomData.LoadEnabledAtStart));
            EditorGUILayout.PropertyField(loadEnabledAtStartProp, new GUIContent("Load Enabled At Start"));

            // Draw readonly toggle for "LoadEnabled"
            RoomData roomData = (RoomData)target;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Load Enabled", roomData.LoadEnabled);
            EditorGUI.EndDisabledGroup();

            // Draw remaining fields
            SerializedProperty iterator = serializedObject.GetIterator();
            iterator.NextVisible(true); // Skip "m_Script"

            while (iterator.NextVisible(false))
            {
                if (iterator.name != "LoadEnabledAtStart" && iterator.name != "LoadEnabled")
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        
    }

}