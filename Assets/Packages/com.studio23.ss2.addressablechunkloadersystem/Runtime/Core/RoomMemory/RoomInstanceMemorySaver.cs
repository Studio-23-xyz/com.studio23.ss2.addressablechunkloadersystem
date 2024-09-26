using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Core.RoomMemory
{
    /// <summary>
    /// Attatched to roominstance in scene
    /// </summary>
    public class RoomInstanceMemorySaver:MonoBehaviour
    {
        private RoomInstance _roomInstance;
        public List<IRoomMemory> RoomMemories = new();

        protected virtual void Awake()
        {
            _roomInstance = GetComponent<RoomInstance>();
            GetRoomMemoriesInRoom();
        }
        [ContextMenu("Get RoomMemoriesInRoom")]
        public List<IRoomMemory> GetRoomMemoriesInRoom()
        {
            GetComponentsInChildren<IRoomMemory>(true, RoomMemories);
            return RoomMemories;
        }
        
        [ContextMenu("log RoomMemoriesInRoom")]
        protected virtual void Start()
        {
            Debug.Log($"{gameObject} roomMemories X {RoomMemories.Count} {_roomInstance._room}", gameObject);
            foreach (var roomMemory in RoomMemories)
            {
                Debug.Log($"roomMemory {roomMemory}", gameObject);
            }
            Debug.Log($"{gameObject} load Room Memory {_roomInstance._room}", gameObject);

            LoadRoomMemory();
        }

        protected virtual void OnDestroy()
        {
            Debug.Log($"{gameObject} Save Room Memory {_roomInstance._room}", gameObject);
            SaveRoomMemory();
        }

        public virtual void SaveRoomMemory()
        {
            RoomManager.Instance.RoomMemorySaver.SaveAllMemoriesInRoom(_roomInstance._room, RoomMemories);
        }
        
        public virtual void LoadRoomMemory()
        {
            RoomManager.Instance.RoomMemorySaver.LoadAllMemoriesInRoom(_roomInstance._room, RoomMemories);
        }
    }
}