using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using Studio23.SS2.AddressableChunkLoaderSystem.Core.RoomMemory;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Editor
{
    public class RoomInstanceMemorySaverEditorWindow : EditorWindow
    {
        private static RoomInstanceMemorySaver _selectedSaver;
        private TreeViewState treeViewState;
        private IRoomMemoryTreeView roomMemoryTreeView;
        [MenuItem("Window/Room Instance Memory Saver Editor")]
        public static void ShowWindow()
        {
            GetWindow<RoomInstanceMemorySaverEditorWindow>("Room Instance Memory Saver");
        }

        private void OnEnable()
        {
            // Search for RoomInstanceMemorySaver in the scene and set the static field
            _selectedSaver = FindObjectOfType<RoomInstanceMemorySaver>();
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            roomMemoryTreeView = new IRoomMemoryTreeView(treeViewState);
        }

        private void OnGUI()
        {
            GUILayout.Label("Room Instance Memory Saver", EditorStyles.boldLabel);

            // ObjectField for selecting a RoomInstanceMemorySaver
            var newSaver = (RoomInstanceMemorySaver)EditorGUILayout.ObjectField("Room Instance Memory Saver",
                _selectedSaver, typeof(RoomInstanceMemorySaver), true);
            
            if (roomMemoryTreeView == null)
            {
                roomMemoryTreeView = new IRoomMemoryTreeView(treeViewState);
            }
            if (_selectedSaver != newSaver)
            {
                roomMemoryTreeView.Reload();
                _selectedSaver = newSaver;
            }
            if (GUILayout.Button("Update Selected RoomInstanceMemorySaver GUID "))
            {
                ForceGenerateUniqueIDs(_selectedSaver);
            }

            if (GUILayout.Button("Update All RoomInstanceMemorySaver GUID"))
            {
                GenerateForAllSaversInLoadedScenes();
            }

 
            roomMemoryTreeView.OnGUI(new Rect(0, 120, position.width, position.height - 120));
        }

        
        public void ForceGenerateUniqueIDs(RoomInstanceMemorySaver saver)
        {
            HashSet<string> usedGUIDSet = new();
            foreach (var roomMemory in saver.GetRoomMemoriesInRoom())
            {
                string id = roomMemory.ID;
                while (string.IsNullOrEmpty(id) || usedGUIDSet.Contains(id))
                {
                    id = GUID.Generate().ToString();
                    // Debug.Log($"{roomMemory} id {id} {usedGUIDSet.Contains(id)}",roomMemory.gameObject);
                }

                usedGUIDSet.Add(id);
                
                if (roomMemory.ID != id)
                {
                    roomMemory.ID = id;
                    Debug.Log($"{roomMemory} set id {roomMemory.ID}-> {id}");
                    EditorUtility.SetDirty(saver);
                    EditorUtility.SetDirty(saver.gameObject);
                }
            }
        }
        
        public static void ForceUpdateUniqueID(RoomInstanceMemorySaver saver, IRoomMemory roomMemory)
        {
            saver.ClearRoomIDs();
            
            string id = roomMemory.ID;
            while (string.IsNullOrEmpty(id) || saver.IDToCountMap.ContainsKey(id))
            {
                id = GUID.Generate().ToString();
                // Debug.Log($"{roomMemory} id {id} {usedGUIDSet.Contains(id)}",roomMemory.gameObject);
            }

            Debug.Log($"{roomMemory} set id {roomMemory.ID}-> {id}");
            roomMemory.ID = id;
            saver.RegisterRoomMemoryUnderID(roomMemory, roomMemory.ID);
            if (roomMemory is Component roomMemoryCompoent)
            {
                EditorUtility.SetDirty(roomMemoryCompoent);
            }
            EditorUtility.SetDirty(roomMemory.gameObject);
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
    
    // TreeView for displaying IRoomMemory components
    public class IRoomMemoryTreeView : TreeView
    {
        private List<IRoomMemory> roomMemories = new List<IRoomMemory>();

        public IRoomMemoryTreeView(TreeViewState state) : base(state)
        {
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

            // Gather all IRoomMemory components across loaded scenes
            var roomMemorySavers = FindAllIRoomMemorySavers();
            roomMemories = new();
            int id = 1;
            foreach (var roomMemorySaver in roomMemorySavers)
            {
                var memories =  roomMemorySaver.GetRoomMemoriesInRoom();
                foreach (var roomMemory in memories)
                {
                    var item = new TreeViewItem { id = id++, displayName = roomMemory.GetType().Name };
                    root.AddChild(item);
                    roomMemories.Add(roomMemory);
                }
            }

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var roomMemory = roomMemories[args.item.id - 1];
            var duplicate = roomMemory.Saver.isDuplicateRoomMemory(roomMemory);

            Rect rowRect = args.rowRect;

            // Highlight the row red if there are multiple occurrences of the ID
            if (duplicate)
            {
                EditorGUI.DrawRect(rowRect, new Color(1f, 0.5f, 0.5f)); // Light red
            }

            // Display the type, GameObject name, and buttons
            var x = rowRect.x;
            var width = 200;
            EditorGUI.LabelField(new Rect(rowRect.x, rowRect.y, width, rowRect.height), roomMemory.GetType().Name);
            x += width;
            
            EditorGUI.LabelField(new Rect(x, rowRect.y, width, rowRect.height), roomMemory.gameObject.name);
            x += width;
            
            EditorGUI.LabelField(new Rect(x, rowRect.y, width, rowRect.height), roomMemory.ID);
            x += width;
            // Button to log the IRoomMemory name
            if (GUI.Button(new Rect(x, rowRect.y, width, rowRect.height), "add new ID"))
            {
                RoomInstanceMemorySaverEditorWindow.ForceUpdateUniqueID(roomMemory.Saver, roomMemory);
            }

            x += width;
            // Button to open the file if it exists
            if (File.Exists(roomMemory.GetPath()) && 
                GUI.Button(new Rect(x, rowRect.y, width, rowRect.height), $"open"))
            {
                Application.OpenURL(roomMemory.GetPath());
            }
        }

        protected override void SingleClickedItem(int id)
        {
            base.SingleClickedItem(id);
            var roomMemory = roomMemories[id - 1];
            Selection.activeGameObject = roomMemory.gameObject;
        }

        private List<RoomInstanceMemorySaver> FindAllIRoomMemorySavers()
        {
            List<RoomInstanceMemorySaver> memories = new ();
            foreach (var scene in SceneManager.GetAllScenes())
            {
                if (scene.isLoaded)
                {
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var root in rootObjects)
                    {
                        var foundMemories = root.GetComponentsInChildren<RoomInstanceMemorySaver>();
                        memories.AddRange(foundMemories);
                    }
                }
            }
            return memories;
        }
    }
}

