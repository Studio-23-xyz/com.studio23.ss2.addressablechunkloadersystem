using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    public class AreaData:ScriptableObject
    {
        [Tooltip("The Master scene that should be loaded first for the area. Ex: the one where the lightmaps are baked")]
        public SceneAsset MainScene;
        public List<FloorData> Floors;
        
    }
}