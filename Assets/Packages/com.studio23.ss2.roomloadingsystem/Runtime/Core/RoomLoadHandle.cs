using Bdeshi.Helpers.Utility;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEditor;

namespace Studio23.SS2.RoomLoadingSystem.Runtime.Core
{
    public class RoomLoadHandle
    {
        public RoomData Room { get; private set; }
        public FiniteTimer UnloadTimer;
        public AsyncOperationHandle<SceneInstance> AsyncHandle { get; private set; }

        public RoomLoadHandle(RoomData room, AssetReferenceT<SceneAsset> sceneAsset,
            LoadSceneMode loadSceneMode, bool activateOnLoad, int priority)
        {
            Room = room;
            UnloadTimer = new FiniteTimer(room.MinUnloadTimeout);
            AsyncHandle =  Addressables.LoadSceneAsync(
                sceneAsset,
                loadSceneMode, activateOnLoad, priority
            );
        }
    }
}