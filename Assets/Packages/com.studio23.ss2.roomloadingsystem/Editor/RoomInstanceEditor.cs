using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEditor;
using UnityEngine;

namespace Editor
{

[CustomEditor(typeof(Studio23.SS2.RoomLoadingSystem.Core.RoomInstance))]
public class RoomInstanceEditor : UnityEditor.Editor
{
    private SerializedProperty roomDataProperty;
    private SerializedProperty roomLoadRadiusProperty;
    private RoomInstance _roomInstance;


    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        _roomInstance = (RoomInstance)target;

        if (_roomInstance.Room == null)
        {
            EditorGUILayout.HelpBox("RoomData is null. Please assign a RoomData scriptable object.", MessageType.Error);
            return;
        }


        // Draw the default inspector for the target script

        // Update the serialized object to reflect the current state of the target script
        serializedObject.Update();

    
        // Show the RoomLoadRadius field as a Vector3
        EditorGUI.BeginChangeCheck();
        var newRoomRadius = EditorGUILayout.FloatField("Load Radius",_roomInstance.Room.RoomLoadRadius);
        if (EditorGUI.EndChangeCheck())
        {
            // Set the RoomData's WorldPosition to the RoomInstance's transform.position
            _roomInstance.Room.RoomLoadRadius = newRoomRadius;

            // Mark the RoomData as dirty to ensure it gets saved
            MarkRoomDataDirty();
        }

        if (!_roomInstance.doesRoomPosMatch())
        {
            EditorGUILayout.HelpBox($"Room {_roomInstance.Room} worldpos {_roomInstance.Room.WorldPosition} doesn't match roominstance worldPos {_roomInstance.transform.position}", MessageType.Error);
        }
        // Display a button to update the RoomPosition
        if (GUILayout.Button("Update RoomPosition"))
        {
            UpdateRoomPosition(_roomInstance);
        }
        // Apply any changes to the serialized object
        serializedObject.ApplyModifiedProperties();
    }

    private void UpdateRoomPosition(RoomInstance roomInstance)
    {
        // Get the target RoomInstance script

        // Ensure the RoomData field is not null
        if (roomInstance.Room != null)
        {
            // Record the change for undo purposes
            Undo.RecordObject(roomInstance.Room, "Update RoomPosition");

            // Set the RoomData's WorldPosition to the RoomInstance's transform.position
            roomInstance.Room.WorldPosition = roomInstance.transform.position;

            // Mark the RoomData as dirty to ensure it gets saved
            MarkRoomDataDirty();
        }
    }

    private void MarkRoomDataDirty()
    {
        // Mark the RoomData as dirty to ensure it gets saved
        EditorUtility.SetDirty(_roomInstance.Room);
    }
}

}