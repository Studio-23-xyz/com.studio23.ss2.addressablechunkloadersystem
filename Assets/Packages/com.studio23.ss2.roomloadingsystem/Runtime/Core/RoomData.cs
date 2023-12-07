using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    [CreateAssetMenu(menuName = "Studio-23/RoomLoadingSystem/RoomData", fileName = "RoomData")]
    public class RoomData:ScriptableObject
    {
        [FormerlySerializedAs("SceneInterior")] public AssetReferenceT<SceneAsset> InteriorScene;
        [FormerlySerializedAs("SceneExterior")] public AssetReferenceT<SceneAsset> ExteriorScene;
        public Vector3 WorldPosition;
        public float RoomLoadRadius = 4; 
        [ShowNativeProperty] public FloorData Floor { get; internal set; }

        [SerializeField] List<RoomData> _adjacentRooms;
        HashSet<RoomData> _adjacentRoomSet;

        //#TODO figure out how to include FMOD
        //List<FMODBank> banks

        public event Action<RoomData> OnRoomEntered;
        public event Action<RoomData> OnRoomExited;
        
        public event Action<RoomData> OnRoomExteriorLoaded;
        public event Action<RoomData> OnRoomExteriorUnloaded;
        
        public event Action<RoomData> OnRoomInteriorLoaded;
        public event Action<RoomData> OnRoomInteriorUnloaded;

        
        public async UniTask loadRoomExterior(LoadSceneMode loadSceneMode = LoadSceneMode.Additive, bool activateOnLoad = true, int priority = 100)
        {
            if (!ExteriorScene.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"{this} has no exterior scene to load", this);
            }
            else
            {
                Debug.Log($"{this} load exterior", this);

                await Addressables.LoadSceneAsync(ExteriorScene, loadSceneMode, activateOnLoad, priority);
            }
            OnRoomExteriorLoaded?.Invoke(this);
        }
        
        public async UniTask loadRoomInterior(LoadSceneMode loadSceneMode = LoadSceneMode.Additive, bool activateOnLoad = true, int priority = 100)
        {
            if (!InteriorScene.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"{this} has no interior scene to load", this);
            }
            else
            {
                Debug.Log($"{this} load interior", this);
                await Addressables.LoadSceneAsync(InteriorScene, loadSceneMode, activateOnLoad, priority);
            }
            
            OnRoomInteriorLoaded?.Invoke(this);
        }
        
        public async UniTask unloadRoomExterior()
        {
            if (!ExteriorScene.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"{this} has no exterior scene to unload", this);
            }
            else
            {
                Debug.Log($"{this} unload exterior", this);
                await ExteriorScene.UnLoadScene();
            }
            OnRoomExteriorUnloaded?.Invoke(this);
        }
        
        public async UniTask unloadRoomInterior()
        {
            if (!InteriorScene.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"{this} has no interior scene to unload", this);
            }
            else
            {
                Debug.Log($"{this} unload interior", this);
                await InteriorScene.UnLoadScene();
            }
            OnRoomInteriorUnloaded?.Invoke(this);
        }
        
        public void Initialize(FloorData floor)
        {
            Floor = floor;
            _adjacentRoomSet = _adjacentRooms.ToHashSet();
        }

        public void HandleRoomEntered()
        {
            OnRoomEntered?.Invoke(this);
        }
        
        public void HandleRoomExited()
        {
            OnRoomExited?.Invoke(this);
        }

        public bool IsAdjacentTo(RoomData room)
        {
            return _adjacentRooms.Contains(room);
        }
        public bool IsPosInLoadingRange(Vector3 position)
        {
            return (position - WorldPosition).magnitude <= RoomLoadRadius;
        }
    }
}