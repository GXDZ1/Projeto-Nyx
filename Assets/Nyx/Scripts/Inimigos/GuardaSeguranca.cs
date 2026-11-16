using UnityEngine;

namespace Nyx
{
    /// <summary>
    /// Guarda que patrulha um corredor e, se estiver armado, atira quando vê o jogador.
    /// </summary>
    public class GuardaSeguranca : MonoBehaviour
    {
        public MaintenanceLevel level;
        public SpriteRenderer spriteRenderer;
        public Sprite[] idleFrames;
        public Sprite[] walkFrames;
        public Sprite[] aimFrames;
        public Sprite[] attackFrames;
        public Sprite bulletSprite;
        public float patrolStart;
        public float patrolEnd;
        public float walkSpeed = 1.4f;
        public float detectionRange = 10f;
        [Range(15f, 85f)] public float visionHalfAngle = 38f;
        public float shotCooldown = 2.2f;
        public float health = 50f;
        public bool usesGun;
        public bool frontArmor;
        public Vector2 pursuitRange;
        public bool startsFacingLeft;
        public TextMesh alertMark;
        public SpriteRenderer laserSight;
        public Sprite[] deathFrames;

        float nextShotTime;
        float attackAnimationEnds;
        int facing = 1;
        Collider2D guardCollider;
        bool dying;
        bool walking;
        bool hadSight;
        float meleeHitsAt = float.PositiveInfinity;
        ContactFilter2D sightFilter;
        readonly RaycastHit2D[] sightHits = new RaycastHit2D[12];

        void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            guardCollider = GetComponent<Collider2D>();
            if (guardCollider != null)
                guardCollider.isTrigger = true;

            facing = startsFacingLeft ? -1 : 1;
            sightFilter.useTriggers = false;
            sightFilter.SetLayerMask((1 << 0) | (1 << 8));
            if (alertMark != null)
                alertMark.gameObject.SetActive(false);
            nextShotTime = Time.time + 1.2f;
        }

        void Update()
        {
            if (dying || level == null || level.player == null || level.completed)
                return;

            bool seesPlayer = CanSeePlayer(out var playerPosition);
            if (alertMark != null)
                alertMark.gameObject.SetActive(seesPlayer);
            bool stationary = Mathf.Abs(patrolEnd - patrolStart) < .1f;
            if (seesPlayer && !hadSight && laserSight != null)
                nextShotTime = Mathf.Max(nextShotTime, Time.time + .77f);
            hadSight = seesPlayer;
            float distanceX = Mathf.Abs(playerPosition.x - transform.position.x);
            bool canShoot = usesGun && seesPlayer && (stationary || distanceX <= 5.2f);
            bool canStrike = !usesGun && seesPlayer && distanceX < 1.2f
                && Mathf.Abs(playerPosition.y - transform.position.y - 1.05f) < .8f;
            walking = false;
            UpdateLaserSight(seesPlayer, playerPosition);

            if (Time.time >= meleeHitsAt)
            {
                meleeHitsAt = float.PositiveInfinity;
                if (canStrike)
                    level.Respawn("O guarda acertou o golpe. Fique fora do alcance quando ele se preparar.");
            }

            if (canShoot)
            {
                AimAt(playerPosition);
                if (Time.time >= nextShotTime)
                {
                    ShootAt(playerPosition);
                    nextShotTime = Time.time + shotCooldown;
                    attackAnimationEnds = Time.time + .28f;
                }
                else if (!stationary && distanceX > 3.1f && Time.time >= attackAnimationEnds)
                {
                    walking = MoveAlongFloor(playerPosition.x, walkSpeed * 1.8f);
                }
            }
            else if (canStrike)
            {
                AimAt(playerPosition);
                if (Time.time >= nextShotTime)
                {
                    nextShotTime = Time.time + .75f;
                    attackAnimationEnds = Time.time + .36f;
                    meleeHitsAt = Time.time + .20f;
                }
            }
            else if (seesPlayer && !stationary)
            {
                AimAt(playerPosition);
                walking = MoveAlongFloor(playerPosition.x, walkSpeed * 1.8f);
            }
            else
            {
                Patrol();
            }

            UpdateAnimation(canShoot || canStrike);
        }

        public void TakeDamage(float amount, Vector2 shotDirection = default)
        {
            if (dying || amount <= 0f)
                return;

            bool hitsFront = frontArmor && shotDirection.x * facing < -.15f;
            health -= hitsFront ? amount * .25f : amount;
            if (health <= 0f)
                StartCoroutine(PlayDeath());
            else if (spriteRenderer != null)
                StartCoroutine(FlashHit());
        }

        System.Collections.IEnumerator FlashHit()
        {
            if (spriteRenderer == null)
                yield break;

            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(.08f);
            if (spriteRenderer != null)
                spriteRenderer.color = original;
        }

        System.Collections.IEnumerator PlayDeath()
        {
            dying = true;
            if (alertMark != null)
                alertMark.gameObject.SetActive(false);
            if (laserSight != null)
                laserSight.enabled = false;
            if (guardCollider != null)
                guardCollider.enabled = false;

            if (deathFrames != null && deathFrames.Length > 0 && spriteRenderer != null)
            {
                for (int i = 0; i < deathFrames.Length; i++)
                {
                    spriteRenderer.sprite = deathFrames[i];
                    yield return new WaitForSeconds(.09f);
                }
            }

            gameObject.SetActive(false);
        }

        bool CanSeePlayer(out Vector2 playerPosition)
        {
            playerPosition = level.player.BodyCenter;
            Vector2 eyes = (Vector2)transform.position + Vector2.up * (laserSight != null ? .2f : 1.2f);
            Vector2 difference = playerPosition - eyes;
            float distance = difference.magnitude;

            if (!level.player.body.simulated || distance > detectionRange || distance < .01f)
                return false;
            if (pursuitRange.y > pursuitRange.x &&
                (playerPosition.x < pursuitRange.x || playerPosition.x > pursuitRange.y))
                return false;

            Vector2 direction = difference / distance;
            float visionLimit = Mathf.Cos(visionHalfAngle * Mathf.Deg2Rad);
            if (direction.x * facing < visionLimit)
                return false;

            int count = Physics2D.Raycast(eyes, direction, sightFilter, sightHits, distance);
            for (int i = 0; i < count; i++)
            {
                var collider = sightHits[i].collider;
                if (collider == null || collider.transform.IsChildOf(transform))
                    continue;
                return collider.attachedRigidbody == level.player.body;
            }

            return true;
        }

        void AimAt(Vector2 target)
        {
            if (Mathf.Abs(target.x - transform.position.x) > .05f)
                facing = target.x > transform.position.x ? 1 : -1;
        }

        void Patrol()
        {
            float left = Mathf.Min(patrolStart, patrolEnd);
            float right = Mathf.Max(patrolStart, patrolEnd);

            if (right - left < .1f)
                return;

            if (transform.position.x <= left + .04f)
                facing = 1;
            else if (transform.position.x >= right - .04f)
                facing = -1;
            walking = MoveAlongFloor(facing > 0 ? right : left, walkSpeed);
            if (!walking)
                facing *= -1;
        }

        bool MoveAlongFloor(float destination, float speed)
        {
            if (pursuitRange.y > pursuitRange.x)
                destination = Mathf.Clamp(destination, pursuitRange.x, pursuitRange.y);
            Vector2 current = transform.position;
            float delta = destination - current.x;
            if (Mathf.Abs(delta) < .025f)
                return false;
            float side = Mathf.Sign(delta);
            float nextX = Mathf.MoveTowards(current.x, destination, speed * Time.deltaTime);
            float step = Mathf.Abs(nextX - current.x);

            int blockers = Physics2D.Raycast(current + Vector2.up * .6f, Vector2.right * side,
                sightFilter, sightHits, step + .38f);
            for (int i = 0; i < blockers; i++)
                if (sightHits[i].collider.attachedRigidbody != level.player.body)
                    return false;

            var foot = new Vector2(nextX + side * .24f, current.y + .22f);
            int floorHits = Physics2D.Raycast(foot, Vector2.down, sightFilter, sightHits, .45f);
            for (int i = 0; i < floorHits; i++)
            {
                var hit = sightHits[i];
                if (hit.fraction <= 0f || hit.normal.y < .55f || hit.collider.attachedRigidbody == level.player.body)
                    continue;
                transform.position = new Vector3(nextX, current.y, transform.position.z);
                return true;
            }
            return false;
        }

        void ShootAt(Vector2 target)
        {
            if (bulletSprite == null)
                return;

            Vector2 start = MuzzlePosition();
            Vector2 direction = (target - start).normalized;

            var bulletObject = new GameObject("Tiro do guarda");
            var bulletRenderer = bulletObject.AddComponent<SpriteRenderer>();
            bulletRenderer.sprite = bulletSprite;
            bulletRenderer.sortingOrder = 15;

            var bullet = bulletObject.AddComponent<ProjetilGuarda>();
            bullet.Initialize(level, transform, start, direction, bulletSprite);
        }

        Vector2 MuzzlePosition() => (Vector2)transform.position +
            new Vector2(facing * .55f, laserSight != null ? .2f : 1.15f);

        void UpdateLaserSight(bool seesPlayer, Vector2 target)
        {
            if (laserSight == null || laserSight.sprite == null)
                return;
            laserSight.enabled = seesPlayer;
            if (!seesPlayer)
                return;

            Vector2 start = MuzzlePosition();
            Vector2 direction = target - start;
            laserSight.transform.SetPositionAndRotation(start + direction * .5f,
                Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            Vector2 size = laserSight.sprite.bounds.size;
            laserSight.transform.localScale = new Vector3(direction.magnitude / size.x, .045f / size.y, 1f);
        }

        void UpdateAnimation(bool aiming)
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.flipX = facing < 0;

            Sprite[] frames;
            float framesPerSecond;
            if (Time.time < attackAnimationEnds && attackFrames != null && attackFrames.Length > 0)
            {
                frames = attackFrames;
                framesPerSecond = 14f;
            }
            else if (walking && walkFrames != null && walkFrames.Length > 0)
            {
                frames = walkFrames;
                framesPerSecond = 8f;
            }
            else if (aiming && aimFrames != null && aimFrames.Length > 0)
            {
                frames = aimFrames;
                framesPerSecond = 4f;
            }
            else if (aiming && idleFrames != null && idleFrames.Length > 0)
            {
                frames = idleFrames;
                framesPerSecond = 3f;
            }
            else
            {
                frames = idleFrames;
                framesPerSecond = 2f;
            }

            if (frames == null || frames.Length == 0)
                return;

            int frameIndex = Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length;
            spriteRenderer.sprite = frames[frameIndex];
        }

    }
}
