using System;
using System.Collections.Generic;
using System.Linq;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Studio23.SS2.RoomLoadingSystem.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class RoomLoader: MonoBehaviour
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
                        Debug.Log($"{room} Try Unloading");

                        _roomsToUnloadListCache.Add(room);
                    }
                }
            }

            foreach (var roomToUnload in _roomsToUnloadListCache)
            {
                ForceUnloadRoomExterior(roomToUnload);
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
                        Debug.Log($"{room} unload Try Unloading");

                        _roomsToUnloadListCache.Add(room);
                    }
                }
            }
            
            foreach (var roomToUnload in _roomsToUnloadListCache)
            {
                ForceUnloadRoomInterior(roomToUnload);
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
                ForceLoadRoomInterior(handle);
            }
            return handle;
        }
        
        private RoomLoadHandle GetOrCreateRoomExteriorLoadHandle(RoomLoadRequestData requestData, RoomFlag flags)
        {
            RoomLoadHandle handle;
            if (_roomExteriorLoadHandles.TryGetValue(requestData.RoomToLoad, out handle))
            {
                //#TODO in the case of an existing handle, we may want to update priority
                handle.AddFlag(flags);
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
                _roomExteriorLoadHandles.Add(requestData.RoomToLoad, handle);
                ForceLoadRoomExterior(handle);
            }

            return handle;
        }
        
    
        private async UniTask ForceLoadRoomInterior(RoomLoadHandle handle)
        {
            await handle.LoadScene();
            handle.Room.HandleRoomExteriorLoaded();
            OnRoomExteriorLoaded?.Invoke(handle.Room);
        }
        private async UniTask ForceLoadRoomExterior(RoomLoadHandle handle)
        {
            await handle.LoadScene();
            handle.Room.HandleRoomInteriorLoaded();
            OnRoomInteriorLoaded?.Invoke(handle.Room);
        }
        


        // public void RemoveExteriorLoadRequest(RoomData room)
        // {
        //     if (_roomExteriorLoadHandles.TryGetValue(room, out var handle))
        //     {
        //         //#TODO in the case of an existing handle, we may want to update priority
        //         handle.RemoveRoomLoadRequester(loadRequester);
        //     }
        // }
        
        public RoomLoadHandle AddExteriorLoadRequest(RoomLoadRequestData loadRequest, RoomFlag flags)
        {
            return GetOrCreateRoomExteriorLoadHandle(loadRequest, flags);
        }
        
        // public void RemoveInteriorLoadRequest(RoomData room)
        // {
        //     if (_roomInteriorLoadHandles.TryGetValue(room, out var handle))
        //     {
        //         //#TODO in the case of an existing handle, we may want to update priority
        //         handle.RemoveRoomLoadRequester(loadRequester);
        //     }
        // }
        //
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

        public void AddHandleForAlreadyLoadedExterior(RoomData room, RoomFlag flags)
        {
            _roomExteriorLoadHandles.Add(room, RoomLoadHandle.ForAlreadyLoadedScene(room, flags,false));
        }

        private void OnDestroy()
        {
            Cleanup();
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

        public async UniTask UnloadAllRooms()
        {
            List<UniTask> handlesToUnload = new List<UniTask>();
            
            foreach (var (room, handle) in _roomInteriorLoadHandles)
            {
                handlesToUnload.Add(handle.UnloadScene());    
            }
            _roomInteriorLoadHandles.Clear();
            
            foreach (var (room, handle) in _roomExteriorLoadHandles)
            {
                handlesToUnload.Add(handle.UnloadScene());    
            }
            _roomExteriorLoadHandles.Clear();

            await UniTask.WhenAll(handlesToUnload);
        }
    }
}