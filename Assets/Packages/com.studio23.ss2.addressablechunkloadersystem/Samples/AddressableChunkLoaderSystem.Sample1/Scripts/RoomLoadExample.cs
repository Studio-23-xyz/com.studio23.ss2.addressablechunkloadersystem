using System;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Sample1
{
    public class RoomLoadExample:MonoBehaviourSingletonPersistent<RoomLoadExample>
    {
        public RoomData RoomData;
        public UnityEvent RoomEnteredAndLoadedEvent;
        public UnityEvent RoomDependenciesLoaded;
        bool hasLoaded = false;
        private bool isLoading = false;
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
            isLoading = false;
            RoomDependenciesLoaded?.Invoke();
        }
        private void Update()
        {
            if (isLoading)
            {
                Debug.Log("VAR"+ RoomManager.Instance.LoadingPercentageForRoom(RoomData, true, true));
            }
        }

        private void OnDisable()
        {
            RoomData.OnRoomDependenciesLoaded -= handleDependenciesLoaded;
        }

        public void LoadRoom1()
        {
            LoadRoom();
        }
        public async UniTask LoadRoom()
        {
            hasLoaded = false;
            isLoading = true;
            await RoomManager.Instance.EnterRoom(RoomData, true);
            
            Debug.Log($"ROOM {RoomData} Entered And Loaded ");
            hasLoaded = true;
            RoomEnteredAndLoadedEvent?.Invoke();
        }

        public void unloadRoomAndReturn()
        {
            RoomManager.Instance.ClearRoomLoads();
            SceneManager.LoadScene("Load From X room");
        }

        protected override void Initialize()
        {
            
        }
    }
}