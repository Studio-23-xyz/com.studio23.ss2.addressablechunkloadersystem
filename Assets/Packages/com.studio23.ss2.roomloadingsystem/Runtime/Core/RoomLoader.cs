using System;
using System.Collections.Generic;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using Studio23.SS2.RoomLoadingSystem.Runtime.Core;
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
                if (RoomManager.Instance.CheckIfRoomExteriorShouldBeLoaded(room))
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
                if (RoomManager.Instance.CheckIfRoomInteriorShouldBeLoaded(room))
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
            if (!room.ExteriorScene.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"{this} has no exterior scene to unload", this);
            }
            else
            {
                Debug.Log($"{this} unload exterior", this);
                //#TODO what happens when this handle is loading a scene and we call this?
                await RemoveRoomExteriorLoadHandle(room).UnloadScene();
            }

            room.HandleExteriorUnloaded();
        }
        
        private async UniTask ForceUnloadRoomInterior(RoomData room)
        {
            if (!room.InteriorScene.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"{room} has no interior scene to unload", room);
            }
            else
            {
                Debug.Log($"{room} unload interior", room);

                await RemoveRoomInteriorLoadHandle(room).UnloadScene();
            }
        }

        
        private RoomLoadHandle GetOrCreateRoomInteriorLoadHandle(RoomLoadRequestData requestData)
        {
            RoomLoadHandle handle;
            if (_roomInteriorLoadHandles.TryGetValue(requestData.RoomToLoad, out handle))
            {
                //#TODO in the case of an existing handle, we may want to update priority
            }
            else
            {
                handle = RoomLoadHandle.AddressableLoad(
                    requestData.RoomToLoad,
                    requestData.RoomToLoad.InteriorScene,
                    requestData.LoadSceneMode, 
                    requestData.ActivateOnLoad, 
                    requestData.Priority
                );
                _roomInteriorLoadHandles.Add(requestData.RoomToLoad, handle);
            }
            return handle;
        }
        
        private RoomLoadHandle GetOrCreateRoomExteriorLoadHandle(RoomLoadRequestData requestData)
        {
            RoomLoadHandle handle;
            if (_roomExteriorLoadHandles.TryGetValue(requestData.RoomToLoad, out handle))
            {
                //#TODO in the case of an existing handle, we may want to update priority
            }
            else
            {
                handle = RoomLoadHandle.AddressableLoad(
                    requestData.RoomToLoad,
                    requestData.RoomToLoad.ExteriorScene,
                    requestData.LoadSceneMode, 
                    requestData.ActivateOnLoad, 
                    requestData.Priority
                );
                _roomExteriorLoadHandles.Add(requestData.RoomToLoad, handle);
            }

            return handle;
        }

        // public void RemoveExteriorLoadRequest(RoomData room)
        // {
        //     if (_roomExteriorLoadHandles.TryGetValue(room, out var handle))
        //     {
        //         //#TODO in the case of an existing handle, we may want to update priority
        //         handle.RemoveRoomLoadRequester(loadRequester);
        //     }
        // }
        
        public RoomLoadHandle AddExteriorLoadRequest(RoomLoadRequestData loadRequest)
        {
            return GetOrCreateRoomExteriorLoadHandle(loadRequest);
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
        public RoomLoadHandle AddInteriorLoadRequest(RoomLoadRequestData loadRequest)
        {
            var handle = GetOrCreateRoomInteriorLoadHandle(loadRequest);
            return handle;
        }

        
        public RoomLoadHandle RemoveRoomInteriorLoadHandle(RoomData room)
        {
            var handle = _roomInteriorLoadHandles[room];
            _roomInteriorLoadHandles.Remove(room);
            return handle;
        }
        
        public RoomLoadHandle RemoveRoomExteriorLoadHandle(RoomData room)
        {
            var handle = _roomExteriorLoadHandles[room];
            _roomExteriorLoadHandles.Remove(room);
            return handle;
        }

        public void addHandleForAlreadyLoadedExterior(RoomData room)
        {
            _roomExteriorLoadHandles.Add(room, RoomLoadHandle.ForAlreadyLoadedScene(room, room.ExteriorScene));
        }
        
        [Button]
        void PrintAllRoomHandle()
        {
            foreach((var room, var handle ) in _roomExteriorLoadHandles)
            {
                Debug.Log($" {(handle)} ");
            }
        }

    }
}