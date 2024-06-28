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
        public AnimationCurve aimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Camera viewportCamera;
        private PlayerController player;
        private Gun gun;
        private Animator animator;
        private Transform[] children;

        private void Awake()
        {
            gun = GetComponentInParent<Gun>();
            player = GetComponentInParent<PlayerController>();
            animator = GetComponent<Animator>();
            children = GetComponentsInChildren<Transform>();

            viewportCamera = GameObject.FindWithTag("ViewportCamera").GetComponent<Camera>();
        }

        private void OnEnable()
        {
            PlayerController.OnFirstPersonViewChanged += OnFirstPersonViewChanged;
            gun.OnShoot += OnShoot;
            gun.OnReload += OnReload;
            
            SetFirstPersonVisible(player.isFirstPerson);
        }

        private void OnDisable()
        {
            PlayerController.OnFirstPersonViewChanged -= OnFirstPersonViewChanged;
            gun.OnShoot -= OnShoot;
            gun.OnReload -= OnReload;
        }

        private void OnReload() { animator.Play("Reload", 0, 0f); }

        private void OnShoot() { animator.Play("Shoot", 0, 0f); }

        private void Update()
        {
            animator.SetBool("Sprinting", player.moveState == PlayerController.MoveState.Sprint);
            animator.SetBool("Moving", player.isMoving);
            animator.SetLayerWeight(1, gun.aimPercent);
        }

        private void LateUpdate()
        {
            var t = aimCurve.Evaluate(gun.aimPercent);
            
            #if UNITY_EDITOR
            if (forceAim)
            {
                t = 1f;
            }
            #endif
            
            if (viewportCamera)
            {
                viewportCamera.fieldOfView = Mathf.Lerp(viewportFieldOfView, aimViewportFieldOfView, t);
            }
        }

        private void OnFirstPersonViewChanged(PlayerController oldViewer, PlayerController newViewer)
        {
            if (newViewer == player) SetFirstPersonVisible(true);
            if (oldViewer == player) SetFirstPersonVisible(false);
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