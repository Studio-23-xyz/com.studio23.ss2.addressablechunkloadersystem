using System;
using Bdeshi.Helpers.Utility;
using System.Collections;
using Cysharp.Threading.Tasks;
using Studio23.SS2.RoomLoadingSystem.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Studio23.SS2.RoomLoadingSystem.Samples.Demo1
{
    //we want to ensure that there is only one player regardless of
    //which and how many rooms are loaded
    public class TestPlayerManager : MonoBehaviourSingletonPersistent<TestPlayerManager>
    {
        [FormerlySerializedAs("player")] public TestCharacterController Player;
        [FormerlySerializedAs("inputs")] public TestCharacterInputs Inputs;
        [FormerlySerializedAs("doorLayer")] public LayerMask DoorLayer;
        public Camera _cam;
        private bool _isInteracting = false;
        private Door prevDoor;
        public float MaxRayDistance = 4;

        protected override void Initialize()
        {
            Player = GetComponent<TestCharacterController>();
            Inputs = GetComponent<TestCharacterInputs>();
        }

        private void OnEnable()
        {
            if(Instance == this)
                Inputs.OnInteractPressed += HandleInteractPressed;
        }

        private void HandleInteractPressed()
        {
            TryEnterDoor();
        }

        private void OnDisable()
        {
            if(Instance == this)
                Inputs.OnInteractPressed += HandleInteractPressed;
        }

        private void Update()
        {
            if (_isInteracting)
            {
                return;
            }
            
            if(prevDoor != null )
                prevDoor.HandleInteractHoverEnd();
            
            
            if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out var hit, MaxRayDistance, DoorLayer))
            {
                var c = hit.collider;
                var door = c.GetComponent<Door>();

                if (door != null)
                {
                    door.HandleInteractHoverStart();
                    prevDoor = door;
                }
            }
        }


        private async UniTask TryEnterDoor()
        {
            if (_isInteracting)
            {
                return;
            }

            if (_cam == null)
            {
                _cam = Camera.main;
            }

            if (Physics.Raycast(_cam.transform.position, _cam.transform.forward, out var hit, MaxRayDistance, DoorLayer))
            {
                var c = hit.collider;
                var door = c.GetComponent<Door>();

                if (door != null)
                {
                    _isInteracting = true;

                    var positionAfterEntry = door.GetPosAfterDoorOpen();
                    positionAfterEntry.y = transform.position.y;
                    Player.Toggle(false);
                    await door.Open();
                    Player.transform.position = positionAfterEntry;
                    await door.DoorCloseAnim();
                    Player.Toggle(true);
                    
                    _isInteracting = false;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(_cam.transform.position, MaxRayDistance * _cam.transform.forward);
        }
    }
}