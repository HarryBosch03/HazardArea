using Runtime.Player;
using Runtime.Weapons;
using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    private Gun gun;

    public float response;
    public float spring;
    public float damping;
    public float maxAngle;
    public Transform pivot;
    public Vector3 pivotOffset;

    private PlayerController player;
    private Vector2 rotation;
    private Vector2 velocity;
    private Vector2 lastHeadRotation;

    private void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        gun = GetComponentInParent<Gun>();
    }

    private void LateUpdate()
    {
        var head = player.head;

        var headRotation = new Vector2(head.eulerAngles.y, -head.eulerAngles.x);
        var headVelocity = diffAngle(headRotation, lastHeadRotation) / Time.deltaTime;
        lastHeadRotation = headRotation;

        var force = -rotation * spring - velocity * damping;
        force += headVelocity * response * Time.deltaTime;

        rotation += velocity * Time.deltaTime;
        velocity += force * Time.deltaTime;

        if (rotation.magnitude > maxAngle)
        {
            var normal = rotation.normalized;
            rotation = normal * maxAngle;
            velocity -= normal * Mathf.Max(0f, Vector2.Dot(normal, velocity));
        }

        var orientation = Quaternion.Euler(-rotation.y, rotation.x, 0f);

        var pivotPoint = pivot ? transform.InverseTransformPoint(pivot.TransformPoint(pivotOffset)) : pivotOffset;
        transform.localPosition = pivotPoint - orientation * pivotPoint;
        transform.localRotation = orientation;

        Vector2 diffAngle(Vector2 a, Vector2 b) => new()
        {
            x = Mathf.DeltaAngle(b.x, a.x),
            y = Mathf.DeltaAngle(b.y, a.y),
        };
    }

    private float ApplySwayCurve(float length)
    {
        if (response <= float.Epsilon) return 0f;
        return (response / (-length - response) + 1f) * maxAngle;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = (pivot ? pivot : transform).localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(pivotOffset, 0.01f);
    }
}