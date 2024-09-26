using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Core.RoomMemory
{
    /// <summary>
    /// Attatched to roominstance in scene
    /// </summary>
    public class RoomInstanceMemorySaver : MonoBehaviour
    {
        public RoomInstance RoomInstance;
        public List<IRoomMemory> RoomMemories = new();
        public Dictionary<string, int> IDToCountMap = new();

        protected virtual void Awake()
        {
            GetRoomMemoriesInRoom();
        }

        public bool isDuplicateRoomMemory(IRoomMemory roomMemory)
        {
            return IDToCountMap[roomMemory.ID] > 1;
        }

        [ContextMenu("Get RoomMemoriesInRoom")]
        public List<IRoomMemory> GetRoomMemoriesInRoom()
        {
            RoomInstance = GetComponent<RoomInstance>();
            GetComponentsInChildren<IRoomMemory>(true, RoomMemories);
            ClearRoomIDs();
            foreach (var roomMemory in RoomMemories)
            {
                roomMemory.Saver = this;
                RegisterRoomMemoryUnderID(roomMemory, roomMemory.ID);
            }
            return RoomMemories;
        }

        public void ClearRoomIDs()
        {
            IDToCountMap.Clear();
        }

        public void RegisterRoomMemoryUnderID(IRoomMemory roomMemory, string id)
        {
            if (IDToCountMap.TryGetValue(roomMemory.ID, out var c))
            {
                IDToCountMap[id] = c++;
                Debug.LogWarning($"Duplicate ID {roomMemory.ID} in {roomMemory}", roomMemory.gameObject);
            }
            else
            {
                IDToCountMap[id] = 1;
            }
        }

        [ContextMenu("log RoomMemoriesInRoom")]
        protected virtual void Start()
        {
            Debug.Log($"{gameObject} roomMemories X {RoomMemories.Count} {RoomInstance._room}", gameObject);
            foreach (var roomMemory in RoomMemories)
            {
                Debug.Log($"roomMemory {roomMemory}", gameObject);
            }
            Debug.Log($"{gameObject} load Room Memory {RoomInstance._room}", gameObject);

            LoadRoomMemory();
        }

        protected virtual void OnDestroy()
        {
            Debug.Log($"{gameObject} Save Room Memory {RoomInstance._room}", gameObject);
            SaveRoomMemory();
        }

        public virtual void SaveRoomMemory()
        {
            RoomManager.Instance.RoomMemorySaver.SaveAllMemoriesInRoom(RoomInstance._room, RoomMemories);
        }
        
        public virtual void LoadRoomMemory()
        {
            RoomManager.Instance.RoomMemorySaver.LoadAllMemoriesInRoom(RoomInstance._room, RoomMemories);
        }
    }
}