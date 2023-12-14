using System;
using Studio23.SS2.RoomLoadingSystem.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
    public class RoomPlayerSpawner:MonoBehaviour
    {
        public RoomInstance roomInstance;
        public TestPlayerManager playerPrefab;

        private void Awake()
        {
            roomInstance = GetComponent<RoomInstance>();
        }

        private void Start()
        {
            if (TestPlayerManager.Instance == null)
            {
                Instantiate(playerPrefab, roomInstance.defaultPlayerSpawnPoint.position, quaternion.identity);
            }
        }
    }
}