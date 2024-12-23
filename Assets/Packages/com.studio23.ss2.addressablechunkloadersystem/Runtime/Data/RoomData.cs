using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Data
{
    [CreateAssetMenu(menuName = "Studio-23/RoomLoadingSystem/RoomData", fileName = "RoomData")]
    public class RoomData:ScriptableObject
    {
        /// <summary>
        /// LoadEnabled Initial value comes from here
        /// </summary>
        public bool LoadEnabledAtStart = true;
        
        /// <summary>
        /// If false, never load
        /// </summary>
        [NonSerialized] public bool LoadEnabled = false;

        [FormerlySerializedAs("InteriorScene1")] 
        public AssetReference InteriorScene;
        [FormerlySerializedAs("ExteriorScene2")] 
        public AssetReference ExteriorScene;
            
        public Vector3 WorldPosition;
        public float RoomLoadRadius = 4;
        public float MinUnloadTimeout = 10;
        public bool SetExteriorAsActiveSceneOnLoad = false;
        
        [ShowNativeProperty] public FloorData Floor { get; internal set; }

        [SerializeField] List<RoomData> _alwaysLoadRooms = new();
        public List<RoomData> AlwaysLoadRooms => _alwaysLoadRooms;

        public event Action<RoomData> OnRoomEntered;
        public event Action<RoomData> OnRoomExited;
        public event Action<RoomData> OnRoomExteriorLoaded;
        public event Action<RoomData> OnRoomExteriorUnloaded;
        
        public event Action<RoomData> OnRoomInteriorLoaded;
        public event Action<RoomData> OnRoomInteriorUnloaded;
        public event Action<RoomData> OnRoomDependenciesLoaded;

        public void HandleRoomDependenciesLoaded()
        {
            OnRoomDependenciesLoaded?.Invoke(this);
        }
        public void HandleRoomExteriorLoaded()
        {
            OnRoomExteriorLoaded?.Invoke(this);
        }
        
        public void HandleRoomInteriorLoaded()
        {
            OnRoomInteriorLoaded?.Invoke(this);
        }
        
        public void HandleExteriorUnloaded()
        {
            OnRoomExteriorUnloaded?.Invoke(this);
        }
        
        public void HandleRoomInteriorUnloaded()
        {
            OnRoomInteriorUnloaded?.Invoke(this);
        }
        
        public void Initialize(FloorData floor)
        {
            Floor = floor;
            LoadEnabled = LoadEnabledAtStart;
        }

        public void HandleRoomEntered()
        {
            OnRoomEntered?.Invoke(this);
        }
        
        public void HandleRoomExited()
        {
            OnRoomExited?.Invoke(this);
        }

        public bool WantsToAlwaysLoad(RoomData room)
        {
            return _alwaysLoadRooms.Contains(room);
        }
        
        public bool CanBeLoaded(Vector3 playerPosition)
        {
            if (!LoadEnabled)
            {
                return false;
            }
            return CanBeLoadedInternal(playerPosition);
        }
        
        public virtual bool CanBeLoadedInternal(Vector3 playerPosition)
        {
            return IsPlayerInLoadingRange(playerPosition);
        }

        public virtual bool IsPlayerInLoadingRange(Vector3 playerPosition)
        {
            var dir = (playerPosition - WorldPosition);
            dir.y = 0;
            return dir.magnitude <= RoomLoadRadius;
        }
    }
}