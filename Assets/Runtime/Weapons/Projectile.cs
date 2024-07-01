using System.Linq;
using FishNet.Object;
using Runtime.Player;
using UnityEngine;

namespace Runtime.Weapons
{
    public class Projectile : MonoBehaviour
    {
        public const int IgnoreDamageLayer = 6;
        public const int LayerMask = ~(1 << IgnoreDamageLayer);
        
        public GameObject hitFX;
        
        private Vector3 velocity;
        private float age;
        private ProjectileSpawnArgs args;
        private NetworkObject owner;

        private void FixedUpdate()
        {
            var ray = new Ray(transform.position, velocity.normalized);

            var hits = Physics.RaycastAll(ray, velocity.magnitude * Time.fixedDeltaTime * 1.01f, LayerMask).OrderBy(e => e.distance);
            foreach (var hit in hits)
            {
                if (IsHitValid(hit))
                {
                    var health = hit.collider.GetComponentInParent<HealthController>();
                    if (health != null)
                    {
                        health.Damage(args.damage);
                    }
                    
                    Debug.DrawLine(transform.position, hit.point, Color.red, 1f);
                    Debug.DrawRay(hit.point, hit.normal * 0.1f, Color.green, 1f);
                    Instantiate(hitFX, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(gameObject);
                    return;
                }
            }

            Debug.DrawLine(transform.position, transform.position + velocity * Time.fixedDeltaTime, Color.red, 1f);

            transform.position += velocity * Time.fixedDeltaTime;
            velocity += Physics.gravity * Time.fixedDeltaTime;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), velocity.magnitude);
            
            age += Time.deltaTime;
            if (age > args.lifetime)
            {
                Destroy(gameObject);
            }
        }
        
        public void Spawn(Transform spawnpoint, Vector3 shooterVelocity, NetworkObject owner, ProjectileSpawnArgs args)
        {
            var instance = Instantiate(this);
            instance.Initialize(spawnpoint, shooterVelocity, owner, args);
        }

        private void Initialize(Transform spawnpoint, Vector3 shooterVelocity, NetworkObject owner, ProjectileSpawnArgs args)
        {
            transform.position = spawnpoint.position;
            transform.rotation = spawnpoint.rotation;

            velocity = transform.forward * args.speed + shooterVelocity;

            this.args = args;
            this.owner = owner;
        }

        private bool IsHitValid(RaycastHit hit)
        {
            if (hit.collider.transform.IsChildOf(owner.transform)) return false;
            return true;
        }

        [System.Serializable]
        public struct ProjectileSpawnArgs
        {
            public HealthController.DamageArgs damage;
            public float speed;
            public float lifetime;
        }
    }
}