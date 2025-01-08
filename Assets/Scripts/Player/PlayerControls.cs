using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Controls
{
    [DisallowMultipleComponent]
    public class PlayerControls : MonoBehaviour
    {
        [Header("Controls")]
        public KeyCode DropKey = KeyCode.X;

        public KeyCode SprintKey = KeyCode.LeftShift;
        public KeyCode JumpKey = KeyCode.Space;
        public KeyCode CrouchKey = KeyCode.LeftControl;
        public KeyCode LeanLeftKey = KeyCode.Q;
        public KeyCode LeanRightKey = KeyCode.E;
        public KeyCode ZoomKey = KeyCode.Z;
        public KeyCode InteractKey = KeyCode.F;

        public KeyCode Action1 = KeyCode.Y;
        public KeyCode CloseUIPrompt = KeyCode.KeypadEnter;
        public KeyCode PauseKey = KeyCode.Escape;

        public KeyCode LidarFire = KeyCode.Mouse0;
        public KeyCode ChangeLidarRadius = KeyCode.Mouse1;
    }
}
