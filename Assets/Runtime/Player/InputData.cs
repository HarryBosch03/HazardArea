using System;
using UnityEngine;

namespace Runtime.Player
{
    [Serializable]
    public struct InputData
    {
        public Vector2 movementDirection;
        public bool jump;
        public bool sprint;
        public bool crouch;
        public Vector2 lookDelta;
        public bool interact;
        public bool shoot;
        public bool aim;
        public bool reload;
    }
}