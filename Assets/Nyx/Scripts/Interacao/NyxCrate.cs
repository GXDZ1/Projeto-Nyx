using UnityEngine;

namespace Nyx
{
    /// <summary>
    /// Ajusta a física de uma caixa que o jogador consegue empurrar.
    /// </summary>
    public class NyxCrate : MonoBehaviour
    {
        static PhysicsMaterial2D crateMaterial;
        Rigidbody2D crateBody;
        Collider2D crateCollider;
        PortalPad previousExit;
        float availableAt;
        float pushingUntil;
        float pushDirection;

        public Rigidbody2D Body => crateBody;
        public Collider2D Collider => crateCollider;

        void Awake()
        {
            crateBody = GetComponent<Rigidbody2D>();
            if (crateBody == null)
                crateBody = gameObject.AddComponent<Rigidbody2D>();
            crateCollider = GetComponent<Collider2D>();

            crateBody.bodyType = RigidbodyType2D.Dynamic;
            crateBody.mass = 1.4f;
            crateBody.gravityScale = 2f;
            crateBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            crateBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            crateBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            crateBody.linearDamping = 2.6f;
            crateBody.angularDamping = 1f;

            if (crateMaterial == null)
            {
                crateMaterial = new PhysicsMaterial2D("Nyx crate friction")
                {
                    friction = .72f,
                    bounciness = 0f
                };
            }

            // Todas as faces da caixa usam atrito e colisão sólida.
            var boxColliders = GetComponents<BoxCollider2D>();
            foreach (var box in boxColliders)
            {
                box.isTrigger = false;
                box.edgeRadius = .02f;
                box.sharedMaterial = crateMaterial;
            }

            gameObject.layer = 8;
        }

        void FixedUpdate()
        {
            if (crateBody == null)
                return;

            // Limita a velocidade para a caixa não disparar ao ser empurrada.
            Vector2 velocity = crateBody.linearVelocity;
            velocity.x = Mathf.Clamp(velocity.x, -3.5f, 3.5f);

            if (Mathf.Abs(velocity.x) < .035f)
                velocity.x = 0;

            crateBody.linearVelocity = velocity;
        }

        public void Push(float direction)
        {
            if (crateBody == null || Mathf.Abs(direction) < .01f)
                return;

            pushDirection = Mathf.Sign(direction);
            pushingUntil = Time.time + .14f;
            crateBody.WakeUp();
            crateBody.AddForce(Vector2.right * Mathf.Sign(direction) * 24f, ForceMode2D.Force);
        }

        public bool CanEnterPortal(PortalPad source)
        {
            if (crateBody == null || crateCollider == null || Time.time < availableAt)
                return false;

            if (previousExit != null)
            {
                var trigger = previousExit.GetComponent<Collider2D>();
                if (trigger.enabled && trigger.bounds.Intersects(crateCollider.bounds))
                    return false;
                previousExit = null;
            }

            Vector2 normal = source.normal;
            if (Vector2.Dot((Vector2)crateCollider.bounds.center - (Vector2)source.transform.position, normal) < 0f)
                return false;

            return Vector2.Dot(crateBody.linearVelocity, normal) < -.05f
                || (normal.y > .7f && crateBody.linearVelocity.y <= .05f)
                || (Time.time < pushingUntil && pushDirection * normal.x < -.5f);
        }

        public void ExitPortal(PortalPad destination, Vector2 point, Vector2 velocity)
        {
            previousExit = destination;
            availableAt = Time.time + .6f;
            pushingUntil = 0f;
            crateBody.position = point;
            transform.position = new Vector3(point.x, point.y, transform.position.z);
            crateBody.linearVelocity = velocity;
            crateBody.WakeUp();
        }
    }
}
