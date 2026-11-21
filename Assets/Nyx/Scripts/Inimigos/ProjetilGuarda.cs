using UnityEngine;

namespace Nyx
{
    /// <summary>
    /// Projétil simples lançado pelo guarda armado.
    /// </summary>
    public class ProjetilGuarda : MonoBehaviour
    {
        MaintenanceLevel level;
        Transform shooter;
        Vector2 direction;
        const float Speed = 8f;
        const float MaxRange = 18f;
        float remainingRange;

        public void Initialize(MaintenanceLevel owner, Transform source, Vector2 start, Vector2 heading, Sprite sprite)
        {
            level = owner;
            shooter = source;
            direction = heading.normalized;
            remainingRange = MaxRange;
            transform.position = start;
            transform.right = direction;
            transform.localScale = Vector3.one * 1.25f;

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.sprite = sprite;
        }

        void Update()
        {
            if (level == null || level.player == null || remainingRange <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float distance = Mathf.Min(Speed * Time.deltaTime, remainingRange);
            Vector2 start = transform.position;
            var hits = Physics2D.RaycastAll(start, direction, distance);

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger)
                    continue;

                if (shooter != null && hit.collider.transform.IsChildOf(shooter))
                    continue;

                var player = hit.collider.GetComponentInParent<Prisoner>();
                if (player == level.player)
                    level.Respawn();

                // Paredes, caixas e o jogador encerram o projétil.
                Destroy(gameObject);
                return;
            }

            transform.position = start + direction * distance;
            remainingRange -= distance;
            if (remainingRange <= 0f)
                Destroy(gameObject);
        }
    }
}
