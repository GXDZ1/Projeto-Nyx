using UnityEngine;

namespace Nyx
{
    /// <summary>
    /// Esmagador que avisa, desce, espera e volta para cima em ciclos.
    /// </summary>
    public class NyxCrusherTrap : MonoBehaviour
    {
        public MaintenanceLevel level;
        public SpriteRenderer warningLight;
        public SpriteRenderer rod;
        public float rodAnchorY;
        public float topY = 3.6f;
        public float bottomY = .55f;
        public float restSeconds = 1.8f;
        public float warningSeconds = .55f;
        public float dropSeconds = .3f;
        public float holdSeconds = .48f;
        public float riseSeconds = .36f;
        public float phaseOffset;

        Collider2D dangerCollider;
        bool isDangerous;

        void Awake()
        {
            dangerCollider = GetComponent<Collider2D>();
            if (dangerCollider != null)
                dangerCollider.isTrigger = true;
            UpdateRod();
        }

        void Update()
        {
            float cycleLength = restSeconds + warningSeconds + dropSeconds + holdSeconds + riseSeconds;
            float timeInCycle = (Time.time + phaseOffset) % cycleLength;
            float y = topY;
            isDangerous = false;

            if (timeInCycle < restSeconds)
            {
                HideWarning();
            }
            else if (timeInCycle < restSeconds + warningSeconds)
            {
                if (warningLight != null)
                {
                    warningLight.enabled = true;
                    warningLight.color = Color.Lerp(Color.white, Color.red, Mathf.PingPong(Time.time * 5f, 1f));
                }
            }
            else if (timeInCycle < restSeconds + warningSeconds + dropSeconds)
            {
                float dropProgress = (timeInCycle - restSeconds - warningSeconds) / dropSeconds;
                dropProgress = Mathf.Clamp01(dropProgress);
                y = Mathf.Lerp(topY, bottomY, dropProgress * dropProgress);
                isDangerous = true;
                HideWarning();
            }
            else if (timeInCycle < restSeconds + warningSeconds + dropSeconds + holdSeconds)
            {
                y = bottomY;
                isDangerous = true;
                HideWarning();
            }
            else
            {
                float riseStart = restSeconds + warningSeconds + dropSeconds + holdSeconds;
                float riseProgress = (timeInCycle - riseStart) / riseSeconds;
                y = Mathf.SmoothStep(bottomY, topY, riseProgress);
                isDangerous = riseProgress < .7f;
                HideWarning();
            }
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            UpdateRod();
            if (dangerCollider != null)
                dangerCollider.enabled = isDangerous;
        }

        void UpdateRod()
        {
            if (rod == null || rod.sprite == null)
                return;

            float headTop = transform.position.y + .42f;
            float length = Mathf.Max(.02f, rodAnchorY - headTop);
            rod.transform.position = new Vector3(transform.position.x, rodAnchorY - length * .5f, transform.position.z);
            Vector3 scale = rod.transform.localScale;
            rod.transform.localScale = new Vector3(scale.x, length / rod.sprite.bounds.size.y, scale.z);
        }

        void HideWarning()
        {
            if (warningLight == null)
                return;

            warningLight.enabled = false;
            warningLight.color = Color.white;
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
            if (isDangerous && level != null && other.GetComponentInParent<Prisoner>() == level.player)
                level.Respawn("O esmagador pegou você. Espere a haste subir antes de passar.");
        }
    }
}
