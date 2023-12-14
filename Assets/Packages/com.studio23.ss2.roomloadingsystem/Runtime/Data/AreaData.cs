using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Studio23.SS2.RoomLoadingSystem.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class AreaData:ScriptableObject
    {
        [Tooltip("The Master scene that should be loaded first for the area. Ex: the one where the lightmaps are baked")]
        public AssetReferenceT<SceneAsset> MasterScene;
        public List<FloorData> Floors;

        public async UniTask LoadMasterSceneAdditively()
        {
            var handle = Addressables.LoadSceneAsync(MasterScene, LoadSceneMode.Additive, true);
            await handle;
            SceneManager.SetActiveScene(handle.Result.Scene);
        }
        
        public async UniTask LoadMasterSceneSingle()
        {
            var handle = Addressables.LoadSceneAsync(MasterScene, LoadSceneMode.Single, true);
            await handle;
            //on single mode, it will automatically be active scene
        }
    }
}