using Bdeshi.Helpers.Utility;
using System.Collections;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
    //we want to ensure that there is only one player regardless of
    //which and how many rooms are loaded
    public class TestPlayerManager : MonoBehaviourSingletonPersistent<TestPlayerManager>
    {
        public TestCharacterController player;

        protected override void Initialize()
        {
            player = GetComponent<TestCharacterController>();
        }
    }
}