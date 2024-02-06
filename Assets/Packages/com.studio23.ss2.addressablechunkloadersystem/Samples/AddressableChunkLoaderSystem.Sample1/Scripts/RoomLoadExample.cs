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
        public Slider LoadSlider;
        
        protected override void Initialize()
        {
            UnloadButton.gameObject.SetActive(false);
            LoadSlider.gameObject.SetActive(false);
        }
        async UniTask UpdateLoadingBar()
        {
            LoadSlider.gameObject.SetActive(true);
            LoadSlider.value = 0;
            while (_isLoading)
            {

                await UniTask.Yield();
                await UniTask.NextFrame();
                var percentage = RoomManager.Instance.LoadingPercentageForRoom(RoomData, true, true);
                Debug.Log($"loading bar percentage {percentage}");
                
                LoadSlider.value = percentage;
            }
            Debug.Log("loading bar done. hiding in 1 sec");
            //keep loading bar active for a bit to not miss it
            await UniTask.Delay(TimeSpan.FromSeconds(1));
            Debug.Log("loading bar hidden ");
            LoadSlider.gameObject.SetActive(false);
        }
        
        private void handleRoomInteriorLoaded(RoomData obj)
        {
            Debug.Log($"ROOM {RoomData} Interior LOADED");
        }
        

        public void LoadRoomNonAsync()
        {
            LoadRoomWithDependencies();
        }
        public async UniTask LoadRoomWithDependencies()
        {
            _isLoading = true;

            //non await call, will end when is loading ends 
            UpdateLoadingBar().Forget();
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

        public void unloadRoomAndReturn()
        {
            RoomManager.Instance.UnloadAllRooms();
            TestPlayerManager.Instance.Player.gameObject.SetActive(false);
        }


    }
}