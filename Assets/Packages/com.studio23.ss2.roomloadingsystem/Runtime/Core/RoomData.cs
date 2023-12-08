using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
namespace Studio23.SS2.RoomLoadingSystem.Core
{
    [CreateAssetMenu(menuName = "Studio-23/RoomLoadingSystem/RoomData", fileName = "RoomData")]
    public class RoomData:ScriptableObject
    {
        public AssetReferenceT<SceneAsset> InteriorScene;
        public AssetReferenceT<SceneAsset> ExteriorScene;
        public Vector3 WorldPosition;
        public float RoomLoadRadius = 4;
        public float MinUnloadTimeout = 10;
        [ShowNativeProperty] public FloorData Floor { get; internal set; }

        [SerializeField] List<RoomData> _alwaysLoadRooms;
        public List<RoomData> AlwaysLoadRooms => _alwaysLoadRooms;

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
                await RoomLoadingManager.Instance.GetOrCreateRoomExteriorLoadHandle(
                    this,
                    loadSceneMode, activateOnLoad, priority)
                    .LoadScene();   
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
                await RoomLoadingManager.Instance.GetOrCreateRoomInteriorLoadHandle(this, 
                    loadSceneMode, activateOnLoad, priority)
                    .LoadScene();
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
                //#TODO what happens when this handle is loading a scene and we call this?
                await RoomLoadingManager.Instance.RemoveRoomExteriorLoadHandle(this)
                    .UnloadScene();
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

                await RoomLoadingManager.Instance.RemoveRoomInteriorLoadHandle(this)
                    .UnloadScene();
            }
            OnRoomInteriorUnloaded?.Invoke(this);
        }
        
        public void Initialize(FloorData floor)
        {
            Floor = floor;
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
            return _alwaysLoadRooms.Contains(room);
        }
        public bool IsPosInLoadingRange(Vector3 position)
        {
            return (position - WorldPosition).magnitude <= RoomLoadRadius;
        }
    }
}