using System;
using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using Runtime.Utility;
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
        public Vector3 cameraSwayMovementOffset;
        public Vector3 cameraSwayMovementFrequency;
        public Vector3 cameraSwayMovementAmplitude;
        public AnimationCurve cameraLandDrop;

        [Space]
        public MoveState moveState;

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
        private Vector2 rotation;
        private float cameraSwayCounter;
        private float cameraSwayStrength;

        private float smoothedCameraHeight;
        private float smoothedFieldOfView;
        private float landTime;
        private bool wasOnGround;
        private RaycastHit groundHit;
        private float lastJumpTime;

        private static PlayerController firstPersonViewer;

        public InputData input { get; private set; }
        public Rigidbody body { get; private set; }
        public bool onGround { get; set; }
        public bool isMoving { get; set; }
        public bool isFirstPerson => firstPersonViewer == this;

        public static event Action<PlayerController, PlayerController> OnFirstPersonViewChanged;

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
            
            if (onGround && !wasOnGround)
            {
                landTime = Time.time;
            }
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
                Replicate(input);
            }
            else
            {
                Replicate(default);
            }
            
            
            if (onGround)
            {
                var speed = Mathf.Sqrt(body.linearVelocity.x * body.linearVelocity.x + body.linearVelocity.z * body.linearVelocity.z);
                cameraSwayCounter += speed * Time.deltaTime;
                cameraSwayStrength = speed / sprintSpeed;
            }
            else
            {
                cameraSwayStrength = 0f;
            }
        }

        [Replicate]
        private void Replicate(InputData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            if (!IsOwner) input = data;

            isMoving = data.movement.magnitude > 0.02f;

            rotation += data.deltaLook;
            rotation = new Vector2(rotation.x % 360f, Mathf.Clamp(rotation.y, -90f, 90f));

            transform.rotation = Quaternion.Euler(0f, rotation.x, 0f);

            CheckForGround();
            ValidateMoveState(data);
            Move(data);
            Jump(data);
        }

        private void Jump(InputData data)
        {
            if (onGround && data.jump.pressedThisTick)
            {
                body.linearVelocity += Vector3.up * Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight - body.linearVelocity.y);
                onGround = false;
                lastJumpTime = (float)TimeManager.TicksToTime(TickType.Tick);
            }
        }

        private void ValidateMoveState(InputData data)
        {
            var canSprint = this.canSprint && data.movement.y > 0.5f;
            var canCrouch = this.canCrouch && onGround;

            if (data.sprint && canSprint) moveState = MoveState.Sprint;
            else if (data.crouch && canCrouch) moveState = MoveState.Crouch;
            else moveState = MoveState.Walk;
        }

        private void Move(InputData data)
        {
            var speed = moveState switch
            {
                MoveState.Sprint => sprintSpeed,
                MoveState.Crouch => crouchSpeed,
                _ => walkSpeed
            };
            var acceleration = 2f / Mathf.Max(accelerationTime, Time.fixedDeltaTime);
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

            body.linearVelocity += force * Time.fixedDeltaTime;
        }

        private void CheckForGround()
        {
            wasOnGround = onGround;

            if (Time.time - lastJumpTime < 2f / 50f)
            {
                onGround = false;
            }
            else
            {
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
        }


        private void ResetInputs()
        {
            var input = this.input;

            input.movement = default;
            input.jump.Reset();
            input.crouch.Reset();
            input.interact.Reset();
            input.sprint.Reset();
            input.shoot.Reset();
            input.aim.Reset();
            input.reload.Reset();
            input.deltaLook = default;

            this.input = input;
        }

        private void OnPostTick()
        {
            CreateReconcile();
            if (IsOwner) ResetInputs();
        }

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
            body.position = data.position;
            body.linearVelocity = data.velocity;
            rotation = data.rotation;
            lastJumpTime = data.lastJumpTime;
        }

        private void Update()
        {
            GetInputs();

            var isCrouching = moveState == MoveState.Crouch;
            var cameraHeight = isCrouching ? crouchCameraHeight : this.cameraHeight;
            smoothedCameraHeight += (cameraHeight - smoothedCameraHeight) / Mathf.Max(cameraHeightSmoothing, Time.deltaTime) * Time.deltaTime;

            var finalRotation = rotation + input.deltaLook;
            finalRotation.y = Mathf.Clamp(rotation.y, -90f, 90f);

            var zoom = moveState switch
            {
                MoveState.Sprint => sprintFovMulti,
                MoveState.Crouch => crouchFovMulti,
                _ => 1f
            };
            var fov = fieldOfView;
            fov = Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) / zoom) * 2f * Mathf.Rad2Deg;
            smoothedFieldOfView += (fov - smoothedFieldOfView) / Mathf.Max(fieldOfViewSmoothing, Time.deltaTime) * Time.deltaTime;

            var animationPosition = Vector3.zero;
            var animationRotation = Quaternion.identity;
            GetCameraAnimationPose(ref animationPosition, ref animationRotation);
            
            head.localPosition = Vector3.up * smoothedCameraHeight + animationPosition;
            head.rotation = Quaternion.Euler(-finalRotation.y, finalRotation.x, 0f) * animationRotation;

            mainCamera.fieldOfView = smoothedFieldOfView;
        }

        private void GetCameraAnimationPose(ref Vector3 animationPosition, ref Quaternion animationRotation) 
        { 
            animationPosition += Vector3.up * cameraLandDrop.Evaluate(Time.time - landTime);

            animationRotation *= Quaternion.Euler(new Vector3()
            {
                x = Mathf.Sin(Mathf.PI * (cameraSwayMovementFrequency.x * cameraSwayCounter + cameraSwayMovementOffset.x)) * cameraSwayMovementAmplitude.x,
                y = Mathf.Sin(Mathf.PI * (cameraSwayMovementFrequency.y * cameraSwayCounter + cameraSwayMovementOffset.y)) * cameraSwayMovementAmplitude.y,
                z = Mathf.Sin(Mathf.PI * (cameraSwayMovementFrequency.z * cameraSwayCounter + cameraSwayMovementOffset.z)) * cameraSwayMovementAmplitude.z,
            });
        }

        private void GetInputs()
        {
            if (!IsOwner) return;

            var input = this.input;

            input.movement = moveAction.ReadValue<Vector2>();
            input.jump.Update(jumpAction);
            input.crouch.Update(crouchAction);
            input.interact.Update(interactAction);
            input.sprint.Update(sprintAction);
            input.shoot.Update(shootAction);
            input.aim.Update(aimAction);
            input.reload.Update(reloadAction);
            if (Cursor.lockState == CursorLockMode.Locked) input.deltaLook += Mouse.current.delta.ReadValue() * mouseSensitivity;

            this.input = input;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;
                else Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public void BindFirstPerson()
        {
            var lastViewer = firstPersonViewer;
            if (firstPersonViewer) firstPersonViewer.UnbindFirstPerson();
            firstPersonViewer = this;

            Cursor.lockState = CursorLockMode.Locked;

            foreach (var e in firstPersonOnly) e.SetActive(true);
            foreach (var e in thirdPersonOnly) e.SetActive(false);

            mainCamera.transform.SetParent(head);
            mainCamera.transform.localPosition = Vector3.zero;
            mainCamera.transform.localRotation = Quaternion.identity;

            OnFirstPersonViewChanged?.Invoke(lastViewer, this);
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
        public struct InputData : IReplicateData
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

                public static implicit operator bool(Button button) => button.pressed || button.pressedThisTick;
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