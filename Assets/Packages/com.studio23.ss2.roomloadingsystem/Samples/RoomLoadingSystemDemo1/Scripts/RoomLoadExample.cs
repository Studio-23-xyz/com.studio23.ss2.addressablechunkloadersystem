using System;
using Cysharp.Threading.Tasks;
using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
    public class RoomLoadExample:MonoBehaviour
    {
        public RoomData RoomData;
        public UnityEvent RoomEnteredAndLoadedEvent;
        public UnityEvent RoomDependenciesLoaded;

        private void OnEnable()
        {
            RoomData.OnRoomDependenciesLoaded += handleDependenciesLoaded;
        }

        private void handleRoomInteriorLoaded(RoomData obj)
        {
            Debug.Log($"ROOM {RoomData} Interior LOADED");
        }

        private void handleDependenciesLoaded(RoomData obj)
        {
            //do what you want when the room's dependencies are loaded
            Debug.Log($" ALL ROOM {RoomData} DEPENDENCIES LOADED");
            RoomDependenciesLoaded?.Invoke();
        }

        private void OnDisable()
        {
            RoomData.OnRoomDependenciesLoaded -= handleDependenciesLoaded;
        }

        private async UniTask LoadRoom()
        {
            await RoomManager.Instance.EnterRoom(RoomData, true);
            
            Debug.Log($"ROOM {RoomData} Entered And Loaded ");
            RoomEnteredAndLoadedEvent?.Invoke();
        }
        private void Start()
        {
            LoadRoom();
        }
    }
}