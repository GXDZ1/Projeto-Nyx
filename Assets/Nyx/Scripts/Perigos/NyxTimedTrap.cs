using UnityEngine;

namespace Nyx
{
    /// <summary>
    /// Alterna entre aviso, perigo ativo e pausa para dar ao jogador uma janela de passagem.
    /// </summary>
    public class NyxTimedTrap : MonoBehaviour
    {
        public MaintenanceLevel level;
        public SpriteRenderer dangerSprite;
        public SpriteRenderer warningSprite;
        public Sprite[] dangerFrames;
        public float warningSeconds = .55f;
        public float activeSeconds = .85f;
        public float restSeconds = 2.2f;
        public float phaseOffset;

        Collider2D dangerCollider;
        Collider2D barrierCollider;

        void Awake()
        {
            foreach (var collider in GetComponents<Collider2D>())
            {
                if (collider.isTrigger)
                    dangerCollider = collider;
                else
                    barrierCollider = collider;
            }

            if (dangerCollider != null)
                dangerCollider.isTrigger = true;
        }

        void Update()
        {
            float cycleLength = warningSeconds + activeSeconds + restSeconds;
            float cycleTime = (Time.time + phaseOffset) % cycleLength;
            bool warning = cycleTime >= restSeconds && cycleTime < restSeconds + warningSeconds;
            bool active = cycleTime >= restSeconds + warningSeconds;

            if (dangerCollider != null)
                dangerCollider.enabled = active;

            if (barrierCollider != null)
                barrierCollider.enabled = active;

            if (dangerSprite != null)
            {
                dangerSprite.enabled = active;
                if (active && dangerFrames != null && dangerFrames.Length > 0)
                {
                    int frame = Mathf.FloorToInt(Time.time * 10f) % dangerFrames.Length;
                    dangerSprite.sprite = dangerFrames[frame];
                }
            }

            if (warningSprite != null)
            {
                warningSprite.enabled = warning;
                if (warning)
                    warningSprite.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 4f, 1f));
                else
                    warningSprite.color = Color.white;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            HurtPlayer(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            HurtPlayer(other);
        }

        void HurtPlayer(Collider2D other)
        {
            if (dangerCollider != null && dangerCollider.enabled && level != null
                && other.GetComponentInParent<Prisoner>() == level.player)
            {
                level.Respawn();
            }
        }
    }
}
