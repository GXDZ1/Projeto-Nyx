using UnityEngine;
using UnityEngine.InputSystem;

namespace Nyx
{
    /// <summary>
    /// Controla o movimento e a mira do personagem.
    /// A animação visual fica em PrisonerSpriteAnimator.
    /// </summary>
    public class Prisoner : MonoBehaviour
    {
        public MaintenanceLevel level;
        public Transform visual, weapon, arm, portalWeapon, firearm;
        public SpriteRenderer sprite;
        public Rigidbody2D body;
        public Collider2D bodyCollider;
        public LayerMask groundMask = (1 << 0) | (1 << 8);

        public float moveSpeed = 5.5f;
        public float jumpSpeed = 8.6f;
        public float jumpBufferDuration = .16f;
        public float coyoteDuration = .14f;

        public bool Grounded { get; private set; }
        public bool HasJumped { get; private set; }
        public bool WalkingBackward { get; private set; }
        public int Facing { get; private set; } = 1;
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public int AimFacing { get; private set; } = 1;

        // O centro do colisor ajuda a calcular a saída correta do portal.
        public Vector2 BodyCenter => bodyCollider != null
            ? bodyCollider.bounds.center
            : body.position + Vector2.up * .9f;

        public Vector2 BodyCenterOffset => BodyCenter - body.position;

        float horizontalInput;
        float jumpBufferTime;
        float coyoteTimer;
        bool jumpButtonHeld;
        readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];

        void Awake()
        {
            // Busca os componentes no mesmo objeto caso eles não tenham sido ligados pelo Inspector.
            if (body == null)
                body = GetComponent<Rigidbody2D>();

            if (bodyCollider == null)
                bodyCollider = GetComponent<Collider2D>();

            if (portalWeapon == null)
                portalWeapon = weapon;

            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.WakeUp();
            }

            if (groundMask.value == 0)
                groundMask = (1 << 0) | (1 << 8);
        }

        void Update()
        {
            if (level != null && level.completed)
                return;

            // Durante a travessia do portal, a posição é animada pela fase.
            // Não aceitamos movimento nem pulos até o personagem sair.
            if (body == null || !body.simulated)
            {
                horizontalInput = 0;
                jumpBufferTime = 0;
                jumpButtonHeld = false;
                return;
            }

            ReadMovementInput();
            UpdateAim();
            UpdateFacing();
        }

        void ReadMovementInput()
        {
            var keys = Keyboard.current;
            if (keys == null)
            {
                horizontalInput = 0;
                jumpButtonHeld = false;
                return;
            }

            bool pressRight = keys.dKey.isPressed || keys.rightArrowKey.isPressed;
            bool pressLeft = keys.aKey.isPressed || keys.leftArrowKey.isPressed;
            horizontalInput = (pressRight ? 1 : 0) - (pressLeft ? 1 : 0);

            // Guardamos o pulo por um instante para ele funcionar um pouco antes de tocar o chão.
            if (keys.spaceKey.wasPressedThisFrame)
                jumpBufferTime = jumpBufferDuration;

            jumpButtonHeld = keys.spaceKey.isPressed;
        }

        void UpdateAim()
        {
            var mouse = Mouse.current;
            if (mouse != null && level != null && level.gameCamera != null)
            {
                Vector2 pointer = level.gameCamera.ScreenToWorldPoint(mouse.position.ReadValue());
                AimAt(pointer);
                return;
            }

            if (Mathf.Abs(horizontalInput) > .01f)
                AimFacing = horizontalInput > 0 ? 1 : -1;

            UpdateWeapon();
        }

        void UpdateFacing()
        {
            Facing = AimFacing;
            WalkingBackward = Mathf.Abs(horizontalInput) > .01f && horizontalInput * Facing < -.01f;

            if (sprite != null)
                sprite.flipX = Facing < 0;
        }

        void FixedUpdate()
        {
            if (body == null || !body.simulated || (level != null && level.completed))
                return;

            Grounded = CheckGrounded();
            if (Grounded)
                coyoteTimer = coyoteDuration;
            else
                coyoteTimer = Mathf.Max(0, coyoteTimer - Time.fixedDeltaTime);

            Vector2 velocity = body.linearVelocity;

            // O pulo pode acontecer ao tocar o chão ou logo depois de sair dele.
            if (jumpBufferTime > 0 && coyoteTimer > 0)
            {
                velocity.y = jumpSpeed;
                Grounded = false;
                HasJumped = true;
                coyoteTimer = 0;
                jumpBufferTime = 0;
            }
            else
            {
                jumpBufferTime = Mathf.Max(0, jumpBufferTime - Time.fixedDeltaTime);
            }

            float acceleration;
            if (Grounded)
                acceleration = Mathf.Abs(horizontalInput) > .01f ? 38f : 46f;
            else
                acceleration = 24f;

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                horizontalInput * moveSpeed,
                acceleration * Time.fixedDeltaTime);

            if (Grounded && velocity.y < 0)
                velocity.y = -1.1f;

            body.linearVelocity = new Vector2(velocity.x, Mathf.Max(velocity.y, -22f));

            // Segurar o botão reduz a gravidade durante a subida do pulo.
            if (Grounded)
                body.gravityScale = 2f;
            else if (velocity.y > .15f)
                body.gravityScale = jumpButtonHeld ? 1.6f : 3.7f;
            else
                body.gravityScale = 3.1f;

            if (Grounded && Mathf.Abs(horizontalInput) > .01f)
                PushCrateAhead();
        }

        void PushCrateAhead()
        {
            if (bodyCollider == null)
                return;

            Bounds bounds = bodyCollider.bounds;
            Vector2 center = new Vector2(
                bounds.center.x + Mathf.Sign(horizontalInput) * (bounds.extents.x + .08f),
                bounds.center.y);

            var hits = Physics2D.OverlapBoxAll(
                center,
                new Vector2(.22f, bounds.size.y * .62f),
                0,
                1 << 8);

            foreach (var hit in hits)
            {
                if (hit == null)
                    continue;

                var crate = hit.GetComponentInParent<NyxCrate>();
                if (crate != null)
                {
                    crate.Push(horizontalInput);
                    return;
                }
            }
        }

        bool CheckGrounded()
        {
            if (bodyCollider == null || body.linearVelocity.y > .1f)
                return false;

            Bounds bounds = bodyCollider.bounds;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundMask);

            // Os raios saem de dentro da largura dos pés e só aceitam superfícies voltadas para cima.
            for (int foot = -1; foot <= 1; foot++)
            {
                var origin = new Vector2(bounds.center.x + foot * bounds.extents.x * .72f, bounds.min.y + .08f);
                int count = Physics2D.Raycast(origin, Vector2.down, filter, groundHits, .17f);
                for (int i = 0; i < count; i++)
                {
                    var hit = groundHits[i];
                    if (hit.fraction > 0f && hit.normal.y > .55f && hit.collider.attachedRigidbody != body)
                        return true;
                }
            }

            return false;
        }

        public bool IsMovingIntoPortal(Vector2 outwardNormal)
        {
            if (Vector2.Dot(body.linearVelocity, outwardNormal) < -.05f)
                return true;
            if (Mathf.Abs(outwardNormal.x) > .7f)
                return horizontalInput * outwardNormal.x < 0f;
            return outwardNormal.y > .7f && body.linearVelocity.y <= .05f;
        }

        void UpdateWeapon()
        {
            float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 shoulder = AimShoulder;
            Vector3 hand = AimHand;

            if (arm != null)
            {
                arm.localPosition = (shoulder + hand) * .5f;
                Vector3 reach = hand - shoulder;
                float armAngle = Mathf.Atan2(reach.y, reach.x) * Mathf.Rad2Deg;
                arm.localRotation = Quaternion.Euler(0f, 0f, armAngle);
                var armScale = arm.localScale;
                armScale.x = reach.magnitude + .04f;
                arm.localScale = armScale;
            }

            if (weapon == null)
                return;

            float scale = weapon == firearm ? .72f : .82f;
            weapon.localScale = new Vector3(scale, scale * AimFacing, 1f);
            weapon.localRotation = rotation;

            // Encaixa o cabo na mão, compensando o pivô que fica no centro do sprite.
            Vector3 grip = weapon == firearm
                ? WeaponSpritePoint(8.5f, 11.5f)
                : WeaponSpritePoint(10.5f, 12.5f);
            weapon.localPosition = hand - rotation * Vector3.Scale(grip, weapon.localScale);
        }

        Vector3 AimShoulder => new Vector3(AimFacing * .23f, .64f, 0f);

        // Ao mirar para cima, a mão abre para o lado e mantém a arma fora do rosto.
        Vector3 AimHand => AimShoulder + (Vector3)AimDirection * .30f
            + Vector3.right * (AimFacing * Mathf.Max(0f, AimDirection.y) * .24f);

        // Os dois sprites medem 32 × 16 pixels, com pivô central e 24 pixels por unidade.
        static Vector3 WeaponSpritePoint(float x, float y) => new Vector3((x - 16f) / 24f, (8f - y) / 24f, 0f);

        public void SetWeaponMode(bool useFirearm)
        {
            if (portalWeapon == null)
                portalWeapon = weapon;

            if (portalWeapon != null)
                portalWeapon.gameObject.SetActive(!useFirearm);
            if (firearm != null)
                firearm.gameObject.SetActive(useFirearm);

            weapon = useFirearm && firearm != null ? firearm : portalWeapon;
            UpdateWeapon();
        }

        public Vector2 WeaponTip => weapon != null
            ? (Vector2)weapon.TransformPoint(weapon == firearm
                ? WeaponSpritePoint(30f, 5.5f)
                : WeaponSpritePoint(31f, 7.5f))
            : (Vector2)transform.TransformPoint(AimHand);

        public void AimAt(Vector2 point)
        {
            float horizontalDistance = point.x - transform.position.x;
            if (Mathf.Abs(horizontalDistance) > .3f)
                AimFacing = horizontalDistance > 0 ? 1 : -1;

            Vector2 shoulder = transform.TransformPoint(AimShoulder);
            Vector2 direction = point - shoulder;
            if (direction.sqrMagnitude > .001f)
                AimDirection = direction.normalized;

            UpdateWeapon();
        }

        public void Teleport(Vector2 point, Vector2 velocity)
        {
            if (body == null)
                return;

            body.position = point;
            transform.position = new Vector3(point.x, point.y, transform.position.z);
            body.linearVelocity = velocity;
            body.WakeUp();

            jumpBufferTime = 0;
            coyoteTimer = 0;
            Grounded = false;

            if (Mathf.Abs(velocity.x) > .08f)
                Facing = velocity.x > 0 ? 1 : -1;
        }

        public void Teleport(Vector2 point)
        {
            Teleport(point, Vector2.zero);
        }

        public void Stop()
        {
            horizontalInput = 0;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }
    }
}
