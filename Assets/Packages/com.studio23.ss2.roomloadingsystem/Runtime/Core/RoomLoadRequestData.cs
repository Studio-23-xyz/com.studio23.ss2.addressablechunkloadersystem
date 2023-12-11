using System;
using UnityEngine.SceneManagement;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    [Serializable]
    public struct RoomLoadRequestData
    {
        public RoomData RoomToLoad;
        public LoadSceneMode LoadSceneMode;
        public bool ActivateOnLoad;
        public int Priority;

        public RoomLoadRequestData(RoomData roomToLoad = null)
        {
            RoomToLoad = roomToLoad;
            LoadSceneMode = LoadSceneMode.Additive;
            ActivateOnLoad = true;
            Priority = 100;
        }
    }
}