using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Core
{
    /// <summary>
    /// Interface that should be implemented for Any subsystem that wants to load rooms
    /// </summary>
    public interface IRoomLoadSubSystem
    {
        public GameObject gameObject { get; }
    }
}