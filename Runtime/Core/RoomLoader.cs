using System;
using System.Collections.Generic;
using BDeshi.Logging;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Core
{
    public class RoomLoader: MonoBehaviour, ISubLoggerMixin<RoomLoadLogCategory>
    {
        private Dictionary<RoomData, RoomLoadHandle> _roomInteriorLoadHandles;
        public Dictionary<RoomData, RoomLoadHandle> RoomInteriorLoadHandles => _roomInteriorLoadHandles;
        private Dictionary<RoomData, RoomLoadHandle> _roomExteriorLoadHandles;
        public Dictionary<RoomData, RoomLoadHandle> RoomExteriorLoadHandles => _roomExteriorLoadHandles;
        private List<RoomData> _roomsToUnloadListCache;
        public event Action<RoomData> OnRoomExteriorLoaded;
        public event Action<RoomData> OnRoomInteriorLoaded;
        public event Action<RoomData> OnRoomExteriorUnloaded;
        public event Action<RoomData> OnRoomInteriorUnloaded;
        
        protected void Awake()
        {
            _roomInteriorLoadHandles = new Dictionary<RoomData, RoomLoadHandle>();
            _roomExteriorLoadHandles = new Dictionary<RoomData, RoomLoadHandle>();
            _roomsToUnloadListCache = new List<RoomData>();
        }

        public float GetExteriorLoadingPercentage(RoomData data)
        {
            if (_roomExteriorLoadHandles.TryGetValue(data, out var handle))
            {
                return handle.GetLoadingPercentage();
            }
            else
            {
                return 0;
            }
        }
        
        public float GetInteriorLoadingPercentage(RoomData data)
        {
            if (_roomInteriorLoadHandles.TryGetValue(data, out var handle))
            {
                return handle.GetLoadingPercentage();
            }
            else
            {
                return 0;
            }
        }


        public void UpdateRoomUnloadTimer()
        {
            _roomsToUnloadListCache.Clear();
            foreach (var (room, handle) in _roomExteriorLoadHandles)
            {
                //#TODO should be optimized into a per roomHandle hashset/bitfield
                //that is set before timer updats
                //instead of checking here
                if (handle.ShouldBeLoaded)
                {
                    handle.UnloadTimer.reset();
                }
                else
                {
                    if (handle.UnloadTimer.tryCompleteTimer(Time.deltaTime))
                    {
                        Logger.Log(RoomLoadLogCategory.Unload,$"{room} unload schedule");

                        _roomsToUnloadListCache.Add(room);
                    }
                }
            }

            foreach (var roomToUnload in _roomsToUnloadListCache)
            {
                ForceUnloadRoomExterior(roomToUnload).Forget();
            }
            
            _roomsToUnloadListCache.Clear();
            foreach (var (room, handle) in _roomInteriorLoadHandles)
            {
                //#TODO should be optimized into a per roomHandle hashset/bitfield
                //that is set before timer updats
                //instead of checking here
                if ((handle.ShouldBeLoaded))
                {
                    handle.UnloadTimer.reset();
                }
                else
                {
                    if (handle.UnloadTimer.tryCompleteTimer(Time.deltaTime))
                    {
                        Logger.Log(RoomLoadLogCategory.Unload,$"{room} unload schedule");

                        _roomsToUnloadListCache.Add(room);
                    }
                }
            }
            
            foreach (var roomToUnload in _roomsToUnloadListCache)
            {
                ForceUnloadRoomInterior(roomToUnload).Forget();
            }
        }

        private async UniTask ForceUnloadRoomExterior(RoomData room)
        {
            //#TODO what happens when this handle is loading a scene and we call this?
            await RemoveRoomExteriorLoadHandle(room).UnloadScene();
            room.HandleExteriorUnloaded();
            OnRoomExteriorUnloaded?.Invoke(room);
        }
        
        private async UniTask ForceUnloadRoomInterior(RoomData room)
        {
            await RemoveRoomInteriorLoadHandle(room).UnloadScene();
            
            room.HandleRoomInteriorUnloaded();
            OnRoomInteriorUnloaded?.Invoke(room);
        }

        
        private RoomLoadHandle GetOrCreateRoomInteriorLoadHandle(RoomLoadRequestData requestData, RoomFlag flags)
        {
            RoomLoadHandle handle;
            if (_roomInteriorLoadHandles.TryGetValue(requestData.RoomToLoad, out handle))
            {
                //#TODO in the case of an existing handle, we may want to update priority
                handle.AddFlag(flags);
                
                if (handle.AddFlag(flags))
                {
                    Logger.Log(RoomLoadLogCategory.AddFlag, $"{requestData.RoomToLoad} add flag {flags}", requestData.RoomToLoad);
                }
            }
            else
            {
                handle = RoomLoadHandle.AddressableLoad(
                    requestData.RoomToLoad,
                    true,
                    flags,
                    requestData.LoadSceneMode, 
                    requestData.ActivateOnLoad, 
                    requestData.Priority
                );
                _roomInteriorLoadHandles.Add(requestData.RoomToLoad, handle);
                Logger.Log(RoomLoadLogCategory.AddFlag, $"{requestData.RoomToLoad} add flag {flags}", requestData.RoomToLoad);
                ForceLoadRoomInterior(handle).Forget();
            }
            return handle;
        }
        
        private RoomLoadHandle GetOrCreateRoomExteriorLoadHandle(RoomLoadRequestData requestData, RoomFlag flags)
        {
            RoomLoadHandle handle;
            if (_roomExteriorLoadHandles.TryGetValue(requestData.RoomToLoad, out handle))
            {
                //#TODO in the case of an existing handle, we may want to update priority
                // Debug.Log(requestData.RoomToLoad + " get handle " + handle);

                if (handle.AddFlag(flags))
                {
                    Logger.Log(RoomLoadLogCategory.AddFlag, $"{requestData.RoomToLoad} add flag {flags}", requestData.RoomToLoad);
                }
            }
            else
            {
                handle = RoomLoadHandle.AddressableLoad(
                    requestData.RoomToLoad,
                    false,
                    flags,
                    requestData.LoadSceneMode, 
                    requestData.ActivateOnLoad, 
                    requestData.Priority
                );
                
                Logger.Log(RoomLoadLogCategory.AddFlag, $"{requestData.RoomToLoad} add flag {flags}", requestData.RoomToLoad);

                _roomExteriorLoadHandles.Add(requestData.RoomToLoad, handle);
                ForceLoadRoomExterior(handle).Forget();
            }

            return handle;
        }
        
    
        private async UniTask ForceLoadRoomInterior(RoomLoadHandle handle)
        {
            await handle.LoadScene();
            handle.Room.HandleRoomInteriorLoaded();
            OnRoomInteriorLoaded?.Invoke(handle.Room);
        }
        private async UniTask ForceLoadRoomExterior(RoomLoadHandle handle)
        {
            await handle.LoadScene();
            handle.Room.HandleRoomExteriorLoaded();
            OnRoomExteriorLoaded?.Invoke(handle.Room);
        }
        
        public RoomLoadHandle AddExteriorLoadRequest(RoomLoadRequestData loadRequest, RoomFlag flags)
        {
            return GetOrCreateRoomExteriorLoadHandle(loadRequest, flags);
        }
        

        public RoomLoadHandle AddInteriorLoadRequest(RoomLoadRequestData loadRequest, RoomFlag flags)
        {
            var handle = GetOrCreateRoomInteriorLoadHandle(loadRequest,flags);
            return handle;
        }
        
        public void RemoveRoomInteriorLoadFlag(RoomData room, RoomFlag flags)
        {
            if (_roomInteriorLoadHandles.TryGetValue(room, out var handle))
            {
                handle.RemoveFlag(flags);
            }
        }
        
        public void RemoveRoomExteriorLoadFlag(RoomData room, RoomFlag flags)
        {
            if (_roomExteriorLoadHandles.TryGetValue(room, out var handle))
            {
                handle.RemoveFlag(flags);
                Logger.Log(RoomLoadLogCategory.RemoveFlag, $"{room} remove Flag {flags}", room);
            }
        }

        
        private RoomLoadHandle RemoveRoomInteriorLoadHandle(RoomData room)
        {
            var handle = _roomInteriorLoadHandles[room];
            _roomInteriorLoadHandles.Remove(room);
            return handle;
        }
        
        private RoomLoadHandle RemoveRoomExteriorLoadHandle(RoomData room)
        {
            var handle = _roomExteriorLoadHandles[room];
            _roomExteriorLoadHandles.Remove(room);
            return handle;
        }

        public void AddHandleForAlreadyLoadedRoom(RoomData room, RoomFlag flags)
        {
            _roomInteriorLoadHandles.Add(room, RoomLoadHandle.ForAlreadyLoadedScene(room, flags,true));
            _roomExteriorLoadHandles.Add(room, RoomLoadHandle.ForAlreadyLoadedScene(room, flags,false));
        }


        private void Cleanup()
        {
            foreach (var (room, handle) in _roomExteriorLoadHandles)
            {
                handle.Cleanup();
            }

            foreach (var (room,handle) in _roomInteriorLoadHandles)
            {
                handle.Cleanup();
            }
        }

        [Button]
        void PrintAllRoomHandle()
        {
            foreach((var room, var handle ) in _roomExteriorLoadHandles)
            {
                Debug.Log($"Exterior {(handle)} ");
            }
            
            foreach((var room, var handle ) in _roomInteriorLoadHandles)
            {
                Debug.Log($"Interior {(handle)} ");
            }
        }

        public ICategoryLogger<RoomLoadLogCategory> Logger => RoomManager.Instance.Logger;
    }
}