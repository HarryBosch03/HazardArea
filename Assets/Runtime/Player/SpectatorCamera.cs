using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Player
{
    public class SpectatorCamera : MonoBehaviour
    {
        public float mouseSensitivity = 0.3f;
        public float moveSpeed = 10.0f;
        public float accelerationTime = 0.3f;

        private Camera mainCam;
        
        private Vector3 velocity;
        private Vector2 rotation;

        private InputAction moveAction;
        private InputAction verticalAction;
        
        private void Awake()
        {
            var map = InputSystem.actions.FindActionMap("Spectator");
            moveAction = map.FindAction("Move");
            verticalAction = map.FindAction("Vertical");

            mainCam = Camera.main;
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        private void FixedUpdate()
        {
            var moveValue = moveAction.ReadValue<Vector2>();
            var verticalValue = verticalAction.ReadValue<float>();
            
            var target = Vector3.ClampMagnitude(transform.TransformVector(moveValue.x, verticalValue, moveValue.y), 1f) * moveSpeed;
            var force = (target - velocity) * 2f / accelerationTime;

            transform.position += velocity * Time.deltaTime;
            velocity += force * Time.deltaTime;
        }
 
        private void Update()
        {
            var lookDelta = Mouse.current.delta.ReadValue();
            rotation += lookDelta * mouseSensitivity;

            rotation.x %= 360f;
            rotation.y = Mathf.Clamp(rotation.y, -90f, 90f);
            
            transform.rotation = Quaternion.Euler(-rotation.y, rotation.x, 0f);

            mainCam.transform.position = transform.position + velocity * (Time.time - Time.fixedTime);
            mainCam.transform.rotation = transform.rotation;
        }
    }
}