using Runtime.Player;
using UnityEngine;

namespace Runtime.Weapons
{
    [RequireComponent(typeof(Animator))]
    public class GunModel : MonoBehaviour
    {
        public const int DefaultModelLayer = 0;
        public const int FirstPersonModelLayer = 3;

#if UNITY_EDITOR
        public bool forceAim;
#endif

        public float viewportFieldOfView;
        public float aimViewportFieldOfView;

        private Camera viewportCamera;
        private FPSController fps;
        private Gun gun;
        private Animator animator;
        private Transform[] children;

        private void Awake()
        {
            gun = GetComponentInParent<Gun>();
            fps = GetComponentInParent<FPSController>();
            animator = GetComponent<Animator>();
            children = GetComponentsInChildren<Transform>();

            var viewportCameraGameObject = GameObject.FindWithTag("ViewportCamera");
            viewportCamera = viewportCameraGameObject ? viewportCameraGameObject.GetComponent<Camera>() : null;
        }

        private void OnEnable()
        {
            FPSController.OnFirstPersonViewChanged += OnFirstPersonViewChanged;
            gun.OnFire += OnFire;
            gun.OnReload += OnReload;
            
            SetFirstPersonVisible(fps.isFirstPerson);
        }

        private void OnDisable()
        {
            FPSController.OnFirstPersonViewChanged -= OnFirstPersonViewChanged;
            gun.OnFire -= OnFire;
            gun.OnReload -= OnReload;
        }

        private void OnReload() { animator.Play("Reload", 0, 0f); }

        private void OnFire() { animator.Play("Shoot", 0, 0f); }

        private void Update()
        {
            animator.SetBool("Sprinting", fps.moveState == FPSController.MoveState.Sprint);
            animator.SetBool("Moving", fps.isMoving);
            animator.SetLayerWeight(1, gun.smoothedAimPercent);
        }

        private void LateUpdate()
        {
            var t = gun.smoothedAimPercent;
            
            #if UNITY_EDITOR
            if (forceAim)
            {
                t = 1f;
            }
            #endif
            
            if (viewportCamera && fps.isFirstPerson)
            {
                viewportCamera.fieldOfView = Mathf.Lerp(viewportFieldOfView, aimViewportFieldOfView, t);
            }
        }

        private void OnFirstPersonViewChanged(FPSController oldViewer, FPSController newViewer)
        {
            if (newViewer == fps) SetFirstPersonVisible(true);
            if (oldViewer == fps) SetFirstPersonVisible(false);
        }

        public void SetFirstPersonVisible(bool visible)
        {
            foreach (var child in children)
            {
                child.gameObject.layer = visible ? FirstPersonModelLayer : DefaultModelLayer;
            }
        }
    }
}