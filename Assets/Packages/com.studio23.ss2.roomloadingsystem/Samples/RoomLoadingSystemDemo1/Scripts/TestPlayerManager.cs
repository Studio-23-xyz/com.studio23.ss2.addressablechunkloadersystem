using System;
using Bdeshi.Helpers.Utility;
using System.Collections;
using Cysharp.Threading.Tasks;
using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEngine;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
    //we want to ensure that there is only one player regardless of
    //which and how many rooms are loaded
    public class TestPlayerManager : MonoBehaviourSingletonPersistent<TestPlayerManager>
    {
        public TestCharacterController player;
        public TestCharacterInputs inputs;
        public LayerMask doorLayer;
        private Camera cam;
        private bool isInteracting = false;
        
        
        protected override void Initialize()
        {
            player = GetComponent<TestCharacterController>();
            inputs = GetComponent<TestCharacterInputs>();
            cam = Camera.main;
            
        }

        private void OnEnable()
        {
            if(Instance == this)
                inputs.OnInteractPressed += handleInteractPressed;
        }

        private void handleInteractPressed()
        {
            tryEnterDoor();
        }

        private void OnDisable()
        {
            if(Instance == this)
                inputs.OnInteractPressed += handleInteractPressed;
        }


        private async UniTask tryEnterDoor()
        {
            if (isInteracting)
            {
                return;
            }

            if (Physics.Raycast(transform.position, cam.transform.forward, out var hit, 100, doorLayer))
            {
                var c = hit.collider;
                var door = c.GetComponent<Door>();

                if (door != null)
                {
                    isInteracting = true;

                    var positionAfterEntry = door.getPosAfterDoorOpen();
                    player.Toggle(false);
                    await door.Open();
                    player.transform.position = positionAfterEntry;
                    await door.doorCloseAnim();
                    player.Toggle(true);
                    
                    isInteracting = false;
                }
            }
        }


    }
}