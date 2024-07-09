using System;
using Runtime.Player;
using UnityEngine;

namespace Runtime.World
{
    public class Interactable : MonoBehaviour
    {
        public float interactTime = 0.5f;

        private float counter;

        public FPSController interactor { get; private set; }
        public Func<string> getDisplayText { get; set; }
        public float progress => counter / interactTime;
        
        public event Action<FPSController> InteractEvent;

        public bool blockInteraction { get; set; }

        private void Awake()
        {
            getDisplayText = () => name;
        }

        public bool CanInteract(FPSController player)
        {
            if (blockInteraction) return false;
            if (interactor == null) return true;
            else return interactor == player;
        }

        private void Update()
        {
            if (interactor != null)
            {
                counter += Time.deltaTime;
                if (counter > interactTime)
                {
                    InteractEvent?.Invoke(interactor);
                    interactor = null;
                    counter = 0f;
                }
            }
        }

        public void StartInteract(FPSController player)
        {
            if (!CanInteract(player)) return;
            interactor = player;
        }

        public void StopInteract(FPSController player)
        {
            if (interactor != player) return;

            interactor = null;
            counter = 0f;
        }
    }
}