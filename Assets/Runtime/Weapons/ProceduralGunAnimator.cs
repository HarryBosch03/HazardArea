using Runtime.Player;
using Runtime.Rendering;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Runtime.Weapons
{
    public class ProceduralGunAnimator : MonoBehaviour
    {
        public Vector3 centerOfMass;
        
        [Space]
        [Header("POSE")]
        public Vector3 idlePosition;
        public Vector3 idleRotation;
        public Vector3 aimPosition;
        public Vector3 aimRotation;
        public float idleFov = 50f;
        public float aimFov = 20f;

        [Space]
        [Header("RECOIL")]
        public Vector3 recoilForceMin = new Vector3(-0.1f, 0.04f, -1f);
        public Vector3 recoilForceMax = new Vector3(0.1f, 0.06f, -1.2f);
        public Vector2 recoilRoll = new Vector2(2f, 5f);
        public float recoilSpring = 400f;
        public float recoilDamping = 25f;

        [Space]
        public float weaponSwayFrequency = 0.25f;
        public Vector2 weaponSwayTranslation = new Vector2(0.1f, 0.08f);
        public Vector2 weaponSwayRotation = new Vector2(4f, 4f);
        public float weaponSwayEaseTime = 0.1f;
        
        [Space]
        [Header("RELOADING")]
        public Vector3 reloadPosition = new Vector3(0.05f, -0.14f, 0.42f);
        public Vector3 reloadRotation = new Vector3(17f, -31f, 0.83f);
        public float reloadEaseTime = 0.5f;
        public AnimationCurve reloadEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Space]
        public float weaponSwayMagnitude = -0.01f;
        public float weaponSwaySmoothing = 0.1f;
        public float weaponSwayClamp = 5f;
        
        #if UNITY_EDITOR
        public bool forceAim;
        public bool forceReload;
        #endif

        private Vector3 recoilPosition;
        private Vector3 recoilVelocity;
        private Vector2 sway;
        private Vector2 lastParentRotation;
        private float weaponSwayBlend;
        private float enableTime;
        private float swayCounter;
        
        private Gun gun;
        private FPSController fps;

        private void Awake()
        {
            gun = GetComponentInParent<Gun>();
            fps = GetComponentInParent<FPSController>();
        }

        private void OnEnable()
        {
            gun.OnFire += OnFire;
            enableTime = Time.time;
        }

        private void OnDisable()
        {
            gun.OnFire -= OnFire;
        }

        private void OnFire()
        {
            recoilVelocity += new Vector3
            {
                x = Random.Range(recoilForceMin.x, recoilForceMax.x),
                y = Random.Range(recoilForceMin.y, recoilForceMax.y),
                z = Random.Range(recoilForceMin.z, recoilForceMax.z),
            };
        }

        private void LateUpdate()
        {
            var aim = gun.smoothedAimPercent;
            #if UNITY_EDITOR
            if (forceAim) aim = 1f;
            #endif

            transform.localPosition = idlePosition;
            transform.localRotation = Quaternion.Euler(idleRotation);

            ApplyHeadBob();
            ApplyWeaponSway();
            
            transform.localPosition = Vector3.Lerp(transform.localPosition, aimPosition, aim);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(aimRotation), aim);
            
            var reloadTime = reloadEaseCurve.Evaluate(1f - (Time.time - enableTime) / reloadEaseTime);
            transform.localPosition = Vector3.LerpUnclamped(transform.localPosition, reloadPosition, reloadTime);
            transform.localRotation = Quaternion.SlerpUnclamped(transform.localRotation, Quaternion.Euler(reloadRotation), reloadTime);
            
#if UNITY_EDITOR
            if (forceReload)
            {
                transform.localPosition = reloadPosition;
                transform.localRotation = Quaternion.Euler(reloadRotation);
            }
#endif
            if (gun.isReloading)
            {
                var t = gun.data.reloadTimer;
                var tMax = gun.reloadTime;
                var p = 0f;
                if (tMax < reloadEaseTime)
                {
                    var tNorm = t / tMax;
                    p = tNorm < 0.5f ? Mathf.SmoothStep(0f, 1f, tNorm * 2f) : Mathf.SmoothStep(1f, 0f, tNorm * 2f - 1f);
                }
                else
                {
                    if (t < reloadEaseTime)
                    {
                        p = reloadEaseCurve.Evaluate(t / reloadEaseTime);
                    }
                    else if (t > tMax - reloadEaseTime)
                    {
                        p = 1f - reloadEaseCurve.Evaluate((t - tMax + reloadEaseTime) / reloadEaseTime);
                    }
                    else p = 1f;
                }

                transform.localPosition = Vector3.LerpUnclamped(transform.localPosition, reloadPosition, p);
                transform.localRotation = Quaternion.SlerpUnclamped(transform.localRotation, Quaternion.Euler(reloadRotation), p);
            }
            
            transform.localPosition += recoilPosition;
            var recoilForce = -recoilPosition * recoilSpring - recoilVelocity * recoilDamping;
            recoilPosition += recoilVelocity * Time.deltaTime;
            recoilVelocity += recoilForce * Time.deltaTime;

            var recoilRotation = Quaternion.Euler(recoilVelocity.z * recoilRoll.x, recoilVelocity.x * recoilRoll.y, 0f);
            transform.localRotation *= recoilRotation;
            transform.localPosition += centerOfMass - recoilRotation * centerOfMass;
            
            ViewportCamera.fieldOfView = Mathf.Lerp(idleFov, aimFov, aim);
        }

        private void ApplyWeaponSway()
        {
            var parentRotation = new Vector2(transform.parent.eulerAngles.y, -transform.parent.eulerAngles.x);
            var delta = new Vector2()
            {
                x = Mathf.DeltaAngle(lastParentRotation.x, parentRotation.x),
                y = Mathf.DeltaAngle(lastParentRotation.y, parentRotation.y),
            } * weaponSwayMagnitude / Time.deltaTime;

            sway = Vector2.Lerp(sway, delta, Time.deltaTime / Mathf.Max(Time.deltaTime, weaponSwaySmoothing));
            sway = Vector2.ClampMagnitude(sway, weaponSwayClamp);
            transform.localRotation = Quaternion.Euler(-sway.y, sway.x, 0f) * transform.localRotation;
            
            lastParentRotation = parentRotation;
        }
        
        private void ApplyHeadBob()
        {
            var velocity = fps.onGround ? fps.body.linearVelocity : default;
            var movement = new Vector2(velocity.x, velocity.z).magnitude;
            swayCounter += movement * Time.deltaTime;
            weaponSwayBlend = Mathf.Lerp(weaponSwayBlend, movement, Time.deltaTime / Mathf.Max(Time.deltaTime, weaponSwayEaseTime));

            var x = swayCounter * weaponSwayFrequency;
            var translation = new Vector3(Mathf.Cos(Mathf.PI * x) * weaponSwayTranslation.x, Mathf.Sin(2f * Mathf.PI * x) * 0.5f * weaponSwayTranslation.y, 0f);
            var rotation = new Vector3(Mathf.Sin(Mathf.PI * x) * weaponSwayRotation.x, Mathf.Cos(2f * Mathf.PI * x) * weaponSwayRotation.y, 0f);
            transform.localPosition += translation * weaponSwayBlend / 100f;
            transform.localRotation = Quaternion.Euler(new Vector3(rotation.y, rotation.x, 0f) * weaponSwayBlend / 100f) * transform.localRotation;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawSphere(centerOfMass, 0.01f);
        }
    }
}