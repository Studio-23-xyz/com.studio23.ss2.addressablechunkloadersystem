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
        private bool _isLoading = false;
        
        public Button UnloadButton;
        [SerializeField] private SampleLoadingBar _loadingBar;
        
        protected override void Initialize()
        {
            UnloadButton.gameObject.SetActive(false);
        }

        public void LoadRoomNonAsync()
        {
            LoadRoomWithDependencies();
        }
        public async UniTask LoadRoomWithDependencies()
        {
            _isLoading = true;

            //non await call, will end when is loading ends 
            _loadingBar.UpdateLoadingBar(RoomData).Forget();
            await RoomManager.Instance.EnterRoom(RoomData, true, true);
            
            Debug.Log($"ROOM {RoomData} Entered And Loaded ");
            RoomEnteredAndLoadedEvent?.Invoke();
            Debug.Log($" ALL ROOM {RoomData} DEPENDENCIES LOADED");
            _isLoading = false;
            RoomDependenciesLoaded?.Invoke();

            await UniTask.Yield();
            await UniTask.NextFrame();
            
            //hack code for spawning player 
            var roomInstance = GameObject.FindObjectOfType<RoomInstance>();
            TestPlayerManager.Instance.Player.transform.position = roomInstance._defaultPlayerSpawnPoint.position + 1 * Vector3.up;
            TestPlayerManager.Instance.Player.gameObject.SetActive(true);
        }

        public void UnloadRoomAndReturn()
        {
            RoomManager.Instance.UnloadAllRooms();
            //hack code for disabling player 
            TestPlayerManager.Instance.Player.gameObject.SetActive(false);
        }


    }
}