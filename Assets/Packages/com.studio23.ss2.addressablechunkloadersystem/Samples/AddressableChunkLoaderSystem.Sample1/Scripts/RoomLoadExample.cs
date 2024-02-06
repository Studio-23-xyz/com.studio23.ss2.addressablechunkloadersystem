using System;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Sample1
{
    public class RoomLoadExample:MonoBehaviourSingletonPersistent<RoomLoadExample>
    {
        public RoomData RoomData;
        public UnityEvent RoomEnteredAndLoadedEvent;
        public UnityEvent RoomDependenciesLoaded;
        bool hasLoaded = false;
        private bool isLoading = false;
        public Button UnloadButton;

        
        protected override void Initialize()
        {
            UnloadButton.gameObject.SetActive(false);
        }
        private void Update()
        {
            if (isLoading)
            {
                Debug.Log("VAR"+ RoomManager.Instance.LoadingPercentageForRoom(RoomData, true, true));
            }
        }
        private void OnEnable()
        {
            RoomData.OnRoomDependenciesLoaded += handleDependenciesLoaded;
        }


        private void OnDisable()
        {
            RoomData.OnRoomDependenciesLoaded -= handleDependenciesLoaded;
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

            await UniTask.Yield();
            await UniTask.NextFrame();
            
            //hack code for spawning player 
            var roomInstance = GameObject.FindObjectOfType<RoomInstance>();
            TestPlayerManager.Instance.Player.transform.position = roomInstance._defaultPlayerSpawnPoint.position + 1 * Vector3.up;
            TestPlayerManager.Instance.Player.gameObject.SetActive(true);

        }

        public void unloadRoomAndReturn()
        {
            RoomManager.Instance.UnloadAllRooms();
            TestPlayerManager.Instance.Player.gameObject.SetActive(false);
        }


    }
}