using System;
using System.Runtime.InteropServices;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : NetworkBehaviour
    {
        public float mouseSensitivity = 0.3f;

        public float walkSpeed = 6f;
        public float sprintSpeed = 13f;
        public float crouchSpeed = 4f;
        public float accelerationTime = 0.4f;
        [Range(0f, 1f)]
        public float airAccelerationPenalty = 0.8f;
        public float jumpHeight = 0.8f;
        public float cameraHeight = 1.7f;
        public float crouchCameraHeight = 1.2f;
        public float cameraHeightSmoothing = 0.1f;
        public float fieldOfView = 100f;
        public float sprintFovMulti = 0.9f;
        public float crouchFovMulti = 1.1f;
        public float fieldOfViewSmoothing = 0.1f;
        public bool canCrouch = true;
        public bool canSprint = true;

        [Space]
        public float cameraSmoothing;
        
        [Space]
        public MoveState state;

        [Space]
        public Transform head;
        public GameObject[] firstPersonOnly;
        public GameObject[] thirdPersonOnly;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction crouchAction;
        private InputAction interactAction;
        private InputAction sprintAction;
        private InputAction shootAction;
        private InputAction aimAction;
        private InputAction reloadAction;

        private Camera mainCamera;
        private Rigidbody body;
        private Vector2 rotation;

        private ReplicationData replicateData;

        private float smoothedCameraHeight;
        private float smoothedFieldOfView;
        private bool onGround;
        private RaycastHit groundHit;
        private float lastJumpTime;

        private static PlayerController firstPersonViewer;

        private void Awake()
        {
            mainCamera = Camera.main;

            GetComponents();
            BindInputs();

            UnbindFirstPerson();
        }

        private void GetComponents() { body = GetComponent<Rigidbody>(); }

        private void BindInputs()
        {
            moveAction = bind("Move");
            jumpAction = bind("Jump");
            crouchAction = bind("Crouch");
            interactAction = bind("Interact");
            sprintAction = bind("Sprint");
            shootAction = bind("Shoot");
            aimAction = bind("Aim");
            reloadAction = bind("Reload");

            InputAction bind(string name) { return InputSystem.actions.FindAction(name); }
        }

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += OnTick;
            TimeManager.OnPostTick += OnPostTick;

            if (Owner.IsLocalClient)
            {
                BindFirstPerson();
            }
            
            name = $"{(Owner.IsLocalClient ? "[LOCAL] " : "")}Player.{Owner.ClientId}";
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= OnTick;
            TimeManager.OnPostTick -= OnPostTick;
        }

        private void OnTick()
        {
            if (IsOwner)
            {
                Replicate(replicateData);
                ResetInputs();
            }
            else
            {
                Replicate(default);
            }
        }

        [Replicate]
        private void Replicate(ReplicationData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            rotation += data.deltaLook;

            transform.rotation = Quaternion.Euler(0f, rotation.x, 0f);

            CheckForGround(data);
            ValidateMoveState(data);
            Move(data);
            Jump(data);
        }

        private void Jump(ReplicationData data)
        {
            if (onGround && data.jump.pressedThisTick)
            {
                body.linearVelocity += Vector3.up * Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight - body.linearVelocity.y);
                onGround = false;
                lastJumpTime = (float)TimeManager.TicksToTime(TickType.Tick);
            }
        }

        private void ValidateMoveState(ReplicationData data)
        {
            var canSprint = this.canSprint && data.movement.y > 0.5f;
            var canCrouch = this.canCrouch && onGround;

            if (data.sprint && canSprint) state = MoveState.Sprint;
            else if (data.crouch && canCrouch) state = MoveState.Crouch;
            else state = MoveState.Walk;
        }

        private void Move(ReplicationData data)
        {
            var speed = state switch
            {
                MoveState.Sprint => sprintSpeed,
                MoveState.Crouch => crouchSpeed,
                _ => walkSpeed
            };
            var acceleration = 2f / Mathf.Max(accelerationTime, Time.deltaTime);
            if (!onGround) acceleration *= 1f - airAccelerationPenalty;

            var direction = Vector3.ClampMagnitude(transform.TransformDirection(data.movement.x, 0f, data.movement.y), 1f);
            var maxForce = speed * acceleration;

            Vector3 force;
            if (onGround)
            {
                force = (direction * speed - body.linearVelocity) * acceleration;
            }
            else
            {
                force = direction * speed * acceleration;
                force *= 1f - Mathf.Clamp01(Vector3.Dot(body.linearVelocity, direction) / speed);
            }

            force.y = 0f;
            force = Vector3.ClampMagnitude(force, maxForce);

            body.linearVelocity += force * Time.deltaTime;
        }

        private void CheckForGround(ReplicationData data)
        {
            if (Time.time - lastJumpTime < 2f / 50f)
            {
                onGround = false;
                return;
            }

            const float length = 1f;
            const float skin = 0.05f;
            var ray = new Ray(body.position + Vector3.up * length, Vector3.down);
            onGround = Physics.Raycast(ray, out groundHit, length + skin);
            if (onGround)
            {
                body.position += Vector3.Project(groundHit.point - body.position, groundHit.normal);
                body.linearVelocity += groundHit.normal * Mathf.Max(0f, Vector3.Dot(groundHit.normal, -body.linearVelocity));
            }
        }


        private void ResetInputs()
        {
            replicateData.movement = default;
            replicateData.jump.Reset();
            replicateData.crouch.Reset();
            replicateData.interact.Reset();
            replicateData.sprint.Reset();
            replicateData.shoot.Reset();
            replicateData.aim.Reset();
            replicateData.reload.Reset();
            replicateData.deltaLook = default;
        }

        private void OnPostTick() { CreateReconcile(); }

        public override void CreateReconcile()
        {
            var reconciliationData = new ReconciliationData
            {
                position = body.position,
                velocity = body.linearVelocity,
                rotation = rotation,
                lastJumpTime = lastJumpTime,
            };
            Reconcile(reconciliationData);
        }

        [Reconcile]
        private void Reconcile(ReconciliationData data, Channel channel = Channel.Unreliable)
        {
            body.MovePosition(data.position);
            body.linearVelocity = data.velocity;
            rotation = data.rotation;
            lastJumpTime = data.lastJumpTime;
        }

        private void Update()
        {
            GetInputs();

            var isCrouching = state == MoveState.Crouch;
            var cameraHeight = isCrouching ? crouchCameraHeight : this.cameraHeight;
            smoothedCameraHeight += (cameraHeight - smoothedCameraHeight) / Mathf.Max(cameraHeightSmoothing, Time.deltaTime) * Time.deltaTime;

            var finalRotation = rotation + replicateData.deltaLook;
            
            var zoom = state switch
            {
                MoveState.Sprint => sprintFovMulti,
                MoveState.Crouch => crouchFovMulti,
                _ => 1f
            };
            var fov = fieldOfView;
            fov = Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) / zoom) * 2f * Mathf.Rad2Deg;
            smoothedFieldOfView += (fov - smoothedFieldOfView) / Mathf.Max(fieldOfViewSmoothing, Time.deltaTime) * Time.deltaTime;

            head.localPosition = Vector3.up * smoothedCameraHeight;
            head.rotation =  Quaternion.Euler(-finalRotation.y, finalRotation.x, 0f);
            
        }

        private void GetInputs()
        {
            if (!IsOwner) return;

            replicateData.movement = moveAction.ReadValue<Vector2>();
            replicateData.jump.Update(jumpAction);
            replicateData.crouch.Update(crouchAction);
            replicateData.interact.Update(interactAction);
            replicateData.sprint.Update(sprintAction);
            replicateData.shoot.Update(shootAction);
            replicateData.aim.Update(aimAction);
            replicateData.reload.Update(reloadAction);
            if (Cursor.lockState == CursorLockMode.Locked) replicateData.deltaLook += Mouse.current.delta.ReadValue() * mouseSensitivity;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;
                else Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void BindFirstPerson()
        {
            if (firstPersonViewer) firstPersonViewer.UnbindFirstPerson();
            firstPersonViewer = this;

            Cursor.lockState = CursorLockMode.Locked;

            foreach (var e in firstPersonOnly) e.SetActive(true);
            foreach (var e in thirdPersonOnly) e.SetActive(false);

            mainCamera.transform.SetParent(head);
            mainCamera.transform.localPosition = Vector3.zero;
            mainCamera.transform.localRotation = Quaternion.identity;
        }

        public void UnbindFirstPerson()
        {
            Cursor.lockState = CursorLockMode.None;

            foreach (var e in firstPersonOnly) e.SetActive(false);
            foreach (var e in thirdPersonOnly) e.SetActive(true);

            if (firstPersonViewer == this) firstPersonViewer = null;
            if (mainCamera.transform.parent == head) mainCamera.transform.SetParent(null);
        }

        [Serializable]
        public struct ReplicationData : IReplicateData
        {
            public Vector2 movement;
            public Button jump;
            public Button crouch;
            public Button interact;
            public Button sprint;
            public Button shoot;
            public Button aim;
            public Button reload;
            public Vector2 deltaLook;

            private uint tick;
            public void Dispose() { }
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;

            public struct Button
            {
                public bool pressed;
                public bool pressedThisTick;
                public bool releasedThisTick;

                public void Update(InputAction action)
                {
                    pressed = action.IsPressed();
                    if (action.WasPressedThisFrame()) pressedThisTick = true;
                    if (action.WasReleasedThisFrame()) releasedThisTick = true;
                }

                public void Reset()
                {
                    pressedThisTick = false;
                    releasedThisTick = false;
                }

                public static implicit operator bool(Button button) => button.pressed;
            }
        }

        public struct ReconciliationData : IReconcileData
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector2 rotation;
            public float lastJumpTime;

            private uint tick;
            public void Dispose() { }
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
        }

        public enum MoveState
        {
            Walk,
            Sprint,
            Crouch,
        }
    }
}