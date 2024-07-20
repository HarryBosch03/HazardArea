using System;
using FishNet.Object;
using Runtime.Utility;
using Runtime.Weapons;
using Runtime.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(HealthController))]
    public class FPSController : NetworkBehaviour
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
        public float cameraPivotDistance = 0.1f;
        public float crouchCameraHeight = 1.2f;
        public float cameraHeightSmoothing = 0.1f;
        public float fieldOfView = 100f;
        public float sprintFovMulti = 0.9f;
        public float crouchFovMulti = 1.1f;
        public float fieldOfViewSmoothing = 0.1f;
        public bool canCrouch = true;
        public bool canSprint = true;

        [Space]
        public float interactionDistance = 2f;

        [Space]
        public Weapon[] equippedWeapons = new Weapon[2];

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
        public Renderer[] firstPersonRenderers;
        public GameObject[] thirdPersonOnly;
        public Renderer[] thirdPersonRenderers;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction crouchAction;
        private InputAction interactAction;
        private InputAction sprintAction;
        private InputAction shootAction;
        private InputAction aimAction;
        private InputAction reloadAction;
        private InputAction weapon1Action;
        private InputAction weapon2Action;

        private Camera mainCamera;
        private HealthController health;
        private float cameraSwayCounter;
        private float cameraSwayStrength;

        private float smoothedCameraHeight;
        private float smoothedFieldOfView;
        private float landTime;
        private bool wasOnGround;
        private RaycastHit groundHit;
        private float lastJumpTime;
        private Weapon[] registeredWeapons;

        private static FPSController firstPersonViewer;

        public Interactable currentInteractable { get; private set; }
        public Interactable lookingAt { get; private set; }
        public Vector2 rotation { get; set; }
        public Weapon currentWeapon { get; private set; }
        public InputData input { get; private set; }
        public Rigidbody body { get; private set; }
        public bool onGround { get; set; }
        public bool isMoving { get; set; }
        public bool isControlling { get; set; }
        public bool isFirstPerson => firstPersonViewer == this;

        public static event Action<FPSController, FPSController> OnFirstPersonViewChanged;

        private void Awake()
        {
            mainCamera = Camera.main;
            
            GetComponents();
            BindInputs();

            UnbindFirstPerson();

            registeredWeapons = GetComponentsInChildren<Weapon>();
            isControlling = true;
        }

        private void GetComponents()
        {
            body = GetComponent<Rigidbody>();
            health = GetComponent<HealthController>();
        }

        private void BindInputs()
        {
            var map = InputSystem.actions.FindActionMap("Player");

            moveAction = bind("Move");
            jumpAction = bind("Jump");
            crouchAction = bind("Crouch");
            interactAction = bind("Interact");
            sprintAction = bind("Sprint");
            shootAction = bind("Shoot");
            aimAction = bind("Aim");
            reloadAction = bind("Reload");
            weapon1Action = bind("Weapon1");
            weapon2Action = bind("Weapon2");

            InputAction bind(string name) { return map.FindAction(name); }
        }

        public override void OnStartNetwork()
        {
            if (Owner.IsLocalClient)
            {
                BindFirstPerson();
            }

            name = $"{(Owner.IsLocalClient ? "[LOCAL] " : "")}Player.{Owner.ClientId}";

            if (onGround && !wasOnGround)
            {
                landTime = Time.time;
            }
            
            ChangeWeapon(equippedWeapons[0]);
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                CommitInputs();
            }
            
            if (health.isAlive.Value)
            {
                body.isKinematic = false;
                isMoving = input.movement.magnitude > 0.02f;

                rotation += input.deltaLook;
                rotation = new Vector2(rotation.x % 360f, Mathf.Clamp(rotation.y, -90f, 90f));

                transform.rotation = Quaternion.Euler(0f, rotation.x, 0f);

                if (currentInteractable == null)
                {
                    var ray = new Ray(head.position, head.forward);
                    if (Physics.Raycast(ray, out var hit, interactionDistance))
                    {
                        lookingAt = hit.collider.GetComponentInParent<Interactable>();
                        if (lookingAt != null && lookingAt.CanInteract(this))
                        {
                            if (input.interact)
                            {
                                currentInteractable = lookingAt;
                                lookingAt.StartInteract(this);
                            }
                        }
                    }
                    else
                    {
                        lookingAt = null;
                    }
                }
                else
                {
                    if (!input.interact || currentInteractable.interactor != this || !isControlling)
                    {
                        currentInteractable.StopInteract(this);
                        currentInteractable = null;
                    }
                    else
                    {
                        var input = new InputData();
                        this.input = input;
                    }
                }

                CheckForGround();
                ValidateMoveState();
                
                Move();
                Jump();

                if (input.weapon1.pressedThisTick) SwitchEquippedWeapons(0);
                if (input.weapon2.pressedThisTick) SwitchEquippedWeapons(1);

                for (var i = 0; i < registeredWeapons.Length; i++)
                {
                    registeredWeapons[i].gameObject.SetActive(registeredWeapons[i] == currentWeapon);
                }
            }
            else
            {
                body.isKinematic = true;
                isMoving = false;
            }
            
            if (IsOwner) ResetInputs();
            
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

        private void CommitInputs()
        {
            var input = this.input;
            input.jump.CommitValues();
            input.crouch.CommitValues();
            input.interact.CommitValues();
            input.sprint.CommitValues();
            input.shoot.CommitValues();
            input.aim.CommitValues();
            input.reload.CommitValues();
            input.weapon1.CommitValues();
            input.weapon2.CommitValues();
            this.input = input;
        }

        private void Jump()
        {
            if (onGround && input.jump.pressedThisTick)
            {
                body.linearVelocity += Vector3.up * Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight - body.linearVelocity.y);
                onGround = false;
                lastJumpTime = TimeUtil.tickTime;
            }
        }

        private void ValidateMoveState()
        {
            if (currentInteractable != null)
            {
                moveState = MoveState.Walk;
                return;
            }

            if (input.sprint.pressedThisTick) moveState = MoveState.Sprint;
            if (input.sprint.releasedThisTick && moveState == MoveState.Sprint) moveState = MoveState.Walk;

            if (input.crouch.pressedThisTick) moveState = MoveState.Crouch;
            if (input.crouch.releasedThisTick && moveState == MoveState.Crouch) moveState = MoveState.Walk;

            if (input.aim.pressedThisTick && moveState == MoveState.Sprint) moveState = MoveState.Walk;

            var canSprint = this.canSprint && input.movement.y > 0.5f;
            var canCrouch = this.canCrouch && onGround;

            if (moveState == MoveState.Sprint && !canSprint) moveState = MoveState.Walk;
            if (moveState == MoveState.Crouch && !canCrouch) moveState = MoveState.Walk;
        }

        private void Move()
        {
            var speed = moveState switch
            {
                MoveState.Sprint => sprintSpeed,
                MoveState.Crouch => crouchSpeed,
                _ => walkSpeed
            };
            var acceleration = walkSpeed / Mathf.Max(accelerationTime, Time.fixedDeltaTime);
            if (!onGround) acceleration *= 1f - airAccelerationPenalty;

            var direction = Vector3.ClampMagnitude(transform.TransformDirection(input.movement.x, 0f, input.movement.y), 1f);
            var velocity = body.linearVelocity;
            velocity.y = 0f;

            Vector3 force;
            if (onGround)
            {
                var difference = direction * speed - velocity;
                force = difference * Mathf.Min(acceleration / difference.magnitude, 1f / Time.fixedDeltaTime);
            }
            else
            {
                var difference = direction.normalized * speed - velocity;
                force = difference * Mathf.Min(acceleration / difference.magnitude, 1f / Time.fixedDeltaTime) * direction.magnitude;
            }

            force.y = 0f;
            body.linearVelocity += force * Time.fixedDeltaTime;
        }

        private void CheckForGround()
        {
            wasOnGround = onGround;

            if (TimeUtil.tickTime - lastJumpTime < 5f / 50f)
            {
                onGround = false;
            }
            else
            {
                const float length = 1f;
                const float skin = 0.1f;
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
            input.deltaLook = default;
            this.input = input;
        }

        private void Update()
        {
            if (!health.isAlive.Value) return;

            GetInputs();

            var isCrouching = moveState == MoveState.Crouch;
            var cameraHeight = isCrouching ? crouchCameraHeight : this.cameraHeight;
            smoothedCameraHeight += (cameraHeight - smoothedCameraHeight) / Mathf.Max(cameraHeightSmoothing, Time.deltaTime) * Time.deltaTime;

            var finalRotation = rotation + input.deltaLook;
            finalRotation.y = Mathf.Clamp(finalRotation.y, -90f, 90f);

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

            var headRotation = Quaternion.Euler(-finalRotation.y, finalRotation.x, 0f) * animationRotation;

            head.localPosition = Vector3.up * (smoothedCameraHeight - cameraPivotDistance) + animationPosition + headRotation * Vector3.up * cameraPivotDistance;
            head.rotation = headRotation;

            if (isFirstPerson)
            {
                mainCamera.fieldOfView = smoothedFieldOfView;
                if (currentWeapon is Gun gun) mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, gun.aimFieldOfView, gun.smoothedAimPercent);
            }
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
            if (!isControlling) return;
            
            var input = this.input;

            input.movement = moveAction.ReadValue<Vector2>();
            input.jump.Update(jumpAction.IsPressed());
            input.crouch.Update(crouchAction.IsPressed());
            input.interact.Update(interactAction.IsPressed());
            input.sprint.Update(sprintAction.IsPressed());
            input.shoot.Update(shootAction.IsPressed());
            input.aim.Update(aimAction.IsPressed());
            input.reload.Update(reloadAction.IsPressed());
            input.weapon1.Update(weapon1Action.IsPressed());
            input.weapon2.Update(weapon2Action.IsPressed());
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
            foreach (var e in firstPersonRenderers) e.enabled = true;
            foreach (var e in thirdPersonRenderers) e.enabled = false;

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
            foreach (var e in firstPersonRenderers) e.enabled = false;
            foreach (var e in thirdPersonRenderers) e.enabled = true;

            if (firstPersonViewer == this) firstPersonViewer = null;
            if (mainCamera.transform.parent == head) mainCamera.transform.SetParent(null);
        }

        public void PickUpWeapon(Weapon weapon)
        {
            for (var i = 0; i < equippedWeapons.Length; i++)
            {
                if (equippedWeapons[i] == null)
                {
                    equippedWeapons[i] = weapon;
                    return;
                }
            }

            for (var i = 0; i < equippedWeapons.Length; i++)
            {
                if (equippedWeapons[i] == currentWeapon)
                {
                    DropWeapon(i);
                    equippedWeapons[i] = weapon;
                    return;
                }
            }

            DropWeapon(0);
            equippedWeapons[0] = weapon;
        }

        private void DropWeapon(int index) { }

        public void SwitchEquippedWeapons(int index) => ChangeWeapon(equippedWeapons[index]);

        public void ChangeWeapon(Weapon weapon)
        {
            currentWeapon = weapon;
        }

        [Serializable]
        public struct InputData
        {
            public Vector2 movement;
            public Button jump;
            public Button crouch;
            public Button interact;
            public Button sprint;
            public Button shoot;
            public Button aim;
            public Button reload;
            public Button weapon1;
            public Button weapon2;
            public Vector2 deltaLook;

            [Serializable]
            public struct Button
            {
                public bool nextValue;
                public bool value;
                public bool lastValue;
                public bool pressedThisTick;
                public bool releasedThisTick;

                public void Update(bool value)
                {
                    nextValue = value;
                }

                public void CommitValues()
                {
                    lastValue = value;
                    value = nextValue;
                    pressedThisTick = value && !lastValue;
                    releasedThisTick = !value && lastValue;
                }

                public static implicit operator bool(Button button) => button.value || button.pressedThisTick;
            }
        }

        public enum MoveState
        {
            Walk,
            Sprint,
            Crouch,
        }
    }
}