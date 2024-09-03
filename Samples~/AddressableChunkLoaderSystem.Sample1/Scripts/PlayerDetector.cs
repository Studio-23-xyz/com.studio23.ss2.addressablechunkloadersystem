using System;
using Studio23.SS2.AddressableChunkLoaderSystem.Core;
using UnityEngine;

namespace Studio23.SS2.AddressableChunkLoaderSystem.Sample1
{
    /// <summary>
    /// exmaple class for setting room manager player ref
    /// </summary>
    public class PlayerDetector:MonoBehaviour
    {
        public Transform _player;
        void FindPlayer()
        {
            if (TestPlayerManager.Instance != null)
            {
                RoomManager.Instance.Player = _player = TestPlayerManager.Instance.transform;

            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            if (_player == null)
            {
                FindPlayer();
            }
        }
    }
}