using System.Collections.Generic;
using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using Studio23.SS2.AddressableChunkLoaderSystem.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Sample1
{
    public class RoomSetLoader:MonoBehaviour
    {
        public List<AssetReferenceT<RoomData>> RoomLoadAssetSet;
        public List<RoomData> RoomLoadSet;

        public bool keepCurRoomLoaded = false;
        public bool shouldLockProxmityLoading = true;
        public void LoadRoomSet()
        {
            foreach (var assetReferenceT in RoomLoadAssetSet)
            {
                var r = assetReferenceT.LoadAssetAsync().WaitForCompletion();
                RoomLoadSet.Add(r);
            }
            RoomManager.Instance.LockRoomLoadSet(RoomLoadSet, keepCurRoomLoaded, shouldLockProxmityLoading);
        }

        public void UnlockRoomSet() => RoomManager.Instance.ToggleLockRoomLoading(transform);
    }
}