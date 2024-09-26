using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Studio23.SS2.AddressableChunkLoaderSystem.Core.RoomMemory;
using UnityEditor.SceneManagement;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Editor
{
    public class RoomInstanceMemorySaverEditorWindow : EditorWindow
    {
        private static RoomInstanceMemorySaver _selectedSaver;

        [MenuItem("Window/Room Instance Memory Saver Editor")]
        public static void ShowWindow()
        {
            GetWindow<RoomInstanceMemorySaverEditorWindow>("Room Instance Memory Saver");
        }

        private void OnEnable()
        {
            // Search for RoomInstanceMemorySaver in the scene and set the static field
            _selectedSaver = FindObjectOfType<RoomInstanceMemorySaver>();
        }

        private void OnGUI()
        {
            GUILayout.Label("Room Instance Memory Saver", EditorStyles.boldLabel);

            // ObjectField for selecting a RoomInstanceMemorySaver
            _selectedSaver = (RoomInstanceMemorySaver)EditorGUILayout.ObjectField("Room Instance Memory Saver",
                _selectedSaver, typeof(RoomInstanceMemorySaver), true);

            if (GUILayout.Button("Update Selected RoomInstanceMemorySaver GUID "))
            {
                ForceGenerateUniqueIDs(_selectedSaver);
            }

            if (GUILayout.Button("Update All RoomInstanceMemorySaver GUID"))
            {
                GenerateForAllSaversInLoadedScenes();
            }
        }

        
        public void ForceGenerateUniqueIDs(RoomInstanceMemorySaver saver)
        {
            HashSet<string> usedGUIDSet = new();
            foreach (var roomMemory in saver.GetRoomMemoriesInRoom())
            {
                string id = roomMemory.ID;
                do
                {
                    id = GUID.Generate().ToString();
                } while (usedGUIDSet.Add(id));

                if (roomMemory.ID != id)
                {
                    roomMemory.ID = id;
                    Debug.Log($"{roomMemory} set id {roomMemory.ID}-> {id}");
                }
            }
        }
        
        private void GenerateForAllSaversInLoadedScenes()
        {
            var savers = new List<RoomInstanceMemorySaver>();

            
            int sceneCount = EditorSceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);

                // Iterate through the root game objects of the scene
                foreach (var rootObject in scene.GetRootGameObjects())
                {
                    savers.AddRange(rootObject.GetComponentsInChildren<RoomInstanceMemorySaver>(true));
                }
            }

            foreach (var saver in savers)
            {
                ForceGenerateUniqueIDs(saver);
            }
        }
    }
}

