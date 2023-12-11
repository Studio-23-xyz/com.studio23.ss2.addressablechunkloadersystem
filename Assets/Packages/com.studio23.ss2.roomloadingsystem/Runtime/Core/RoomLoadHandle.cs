using Bdeshi.Helpers.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEditor;
using Cysharp.Threading.Tasks;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;
using UnityEngine.Networking;
using System.Linq;

namespace Studio23.SS2.RoomLoadingSystem.Runtime.Core
{
    public class RoomLoadHandle
    {
        public RoomData Room { get; private set; }
        AssetReference _scene;
        public FiniteTimer UnloadTimer;
        bool UsesAddressable;
        public AsyncOperationHandle<SceneInstance> LoadHandle { get; private set; }
        public AsyncOperationHandle<SceneInstance> UnloadHandle { get; private set; }
        private HashSet<IRoomLoadSubSystem> _loadRequesters = new HashSet<IRoomLoadSubSystem>();
        public bool ShouldBeLoaded => _loadRequesters.Count > 0;

        private RoomLoadHandle() { }


        /// <summary>
        /// Use this when this room will be not loaded as an addressable.
        /// Ex: As the initial scene you have when you hit playmode
        /// </summary>
        /// <param name="room"></param>
        /// <param name="sceneAsset"></param>
        /// <param name="loadSceneMode"></param>
        /// <param name="activateOnLoad"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static RoomLoadHandle ForAlreadyLoadedScene(RoomData room,
            AssetReferenceT<SceneAsset> sceneAsset)
        {
            return new RoomLoadHandle()
            {
                Room = room,
                _scene = sceneAsset,
                UnloadTimer = new FiniteTimer(room.MinUnloadTimeout),
                UsesAddressable = false,
            };
        }

        public void AddRoomLoadRequester(IRoomLoadSubSystem loadRequester)
        {
            _loadRequesters.Add(loadRequester);
        }
        
        public void RemoveRoomLoadRequester(IRoomLoadSubSystem loadRequester)
        {
            _loadRequesters.Remove(loadRequester);
        }
        
        
        /// <summary>
        /// Use this when this room will be  loaded as an addressable.
        /// </summary>
        /// <param name="room"></param>
        /// <param name="sceneAsset"></param>
        /// <param name="loadSceneMode"></param>
        /// <param name="activateOnLoad"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static RoomLoadHandle AddressableLoad(RoomData room,
            AssetReferenceT<SceneAsset> sceneAsset,
            LoadSceneMode loadSceneMode, bool activateOnLoad, int priority)
        {

            return new RoomLoadHandle()
            {
                Room = room,
                _scene = sceneAsset,
                UnloadTimer = new FiniteTimer(room.MinUnloadTimeout),
                LoadHandle = Addressables.LoadSceneAsync(
                    sceneAsset,
                    loadSceneMode, activateOnLoad, priority
                ),
                UsesAddressable = true,
            };
        }

        public async UniTask UnloadScene()
        {
            if (UsesAddressable)
            {
                UnloadHandle =  Addressables.UnloadSceneAsync(LoadHandle);

                await UnloadHandle;
            }
            else
            {
                //#TODO ok this is a hack.
                //But this situation shouldn't be ocurring in a build to begin with.
                var locations = await Addressables.LoadResourceLocationsAsync(_scene);
                var sceneID = locations.First().InternalId;
                await SceneManager.UnloadSceneAsync(sceneID);
                Addressables.Release(locations);
            }
        }

        public async UniTask LoadScene()
        {
            if (UsesAddressable)
            {
                await LoadHandle;
            }
        }
        
        public override string ToString()
        {
            var s = $"{Room} {(LoadHandle.IsDone ? "is loaded" : "loading")} {UsesAddressable} {UsesAddressable}";

            if (_loadRequesters.Count > 0)
            {
                s += "\n Requested by: ";
                foreach (var loadRequester in _loadRequesters)
                {
                    s += $"\n {loadRequester}";
                }
            }

            return s;
        }
    }
}