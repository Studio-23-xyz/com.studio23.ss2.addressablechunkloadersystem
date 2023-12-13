using System.Linq;
using Bdeshi.Helpers.Utility;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class RoomLoadHandle
    {
        public RoomData Room { get; private set; }
        private bool _isInterior;
        AssetReference _scene;
        public FiniteTimer UnloadTimer;

        private RoomFlag _flags;
        public bool ShouldBeLoaded => _flags != RoomFlag.None;
        public bool UsesAddressable { get; private set; }

        /// <summary>
        /// The addressable async handle for loading the room.
        /// This can be null when this room is not loaded as an addressable
        /// </summary>
        public AsyncOperationHandle<SceneInstance> LoadHandle { get; private set; }
        /// <summary>
        /// The addressable async handle for unloading the room.
        /// This can be null when this room is not loaded as an addressable
        /// </summary>
        public AsyncOperationHandle<SceneInstance> UnloadHandle { get; private set; }
        
        
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
        public static RoomLoadHandle ForAlreadyLoadedScene(RoomData room,RoomFlag flags, bool isInterior)
        {
            return new RoomLoadHandle()
            {
                Room = room,
                _isInterior = isInterior,
                _flags = flags,
                _scene = isInterior? room.InteriorScene: room.ExteriorScene,
                UnloadTimer = new FiniteTimer(room.MinUnloadTimeout),
                UsesAddressable = false,
            };
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
            bool isInterior,
            RoomFlag flags,
            LoadSceneMode loadSceneMode, bool activateOnLoad, int priority)
        {
            var sceneAsset = isInterior ? room.InteriorScene : room.ExteriorScene;
            if (sceneAsset.RuntimeKeyIsValid())
            {
                return new RoomLoadHandle()
                {
                    Room = room,
                    _scene = sceneAsset,
                    _flags = flags,
                    _isInterior = isInterior,
                    UnloadTimer = new FiniteTimer(room.MinUnloadTimeout),
                    LoadHandle = Addressables.LoadSceneAsync(
                        sceneAsset,
                        loadSceneMode, activateOnLoad, priority
                    ),
                    UsesAddressable = true,
                };
            }
            else
            {
                return new RoomLoadHandle()
                {
                    Room = room,
                    _scene = sceneAsset,
                    _flags = flags,
                    _isInterior = isInterior,
                    UnloadTimer = new FiniteTimer(room.MinUnloadTimeout),
                    UsesAddressable = false,
                };
            }
        }

        public void AddFlag(RoomFlag flags)
        {
            _flags |= flags;
        }
        public void RemoveFlag(RoomFlag flags)
        {
            _flags  = (_flags & ~flags);
        }

        public async UniTask UnloadScene()
        {
            if (!_scene.RuntimeKeyIsValid())
            {
                Debug.LogWarning($"{Room} has no scene to unload", Room);
            }
            else
            {
                Debug.Log($"{Room} unload", Room);
                if (UsesAddressable)
                {
                    UnloadHandle =  Addressables.UnloadSceneAsync(LoadHandle);

                    await UnloadHandle;
                }
                else
                {
                    //#TODO ok this is a hack.
                    //we need to fetch the scene name from the asset ref
                    //and unload it manually
                    //But this situation shouldn't be ocurring in a build to begin with.
                    var locations = await Addressables.LoadResourceLocationsAsync(_scene);
                    var sceneID = locations.First().InternalId;
                    await SceneManager.UnloadSceneAsync(sceneID);
                }
            }
        }

        public bool IsLoaded()
        {
            if (!UsesAddressable)
            {
                return true;
            }

            return LoadHandle.IsDone;
        }

        public async UniTask LoadScene()
        {
            if (UsesAddressable)
            {
                await LoadHandle;
            }
        }

        public void Cleanup()
        {
            if (UsesAddressable)
            {
                if (LoadHandle.IsValid())
                {
                    Addressables.Release(LoadHandle);
                }
                
                if (UnloadHandle.IsValid())
                {
                    Addressables.Release(UnloadHandle);
                }
            }
        }
        
        public override string ToString()
        {
            var s = $"{Room.name}{(_isInterior ? "interior" : "exterior")} {_flags} UsesAddressable: {UsesAddressable} {(LoadHandle.IsDone ? "is loaded" : $"loading ")} unload timer:{UnloadTimer.Timer}/{UnloadTimer.MaxValue} ";

            return s;
        }

        public bool HasFlag(RoomFlag flags)
        {
            return (_flags & flags) == flags;
        }
    }
    
}