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
        float speed = 8f;
        float lifeTime = 3f;

        public void Initialize(MaintenanceLevel owner, Transform source, Vector2 start, Vector2 heading, Sprite sprite)
        {
            level = owner;
            shooter = source;
            direction = heading.normalized;
            transform.position = start;
            transform.right = direction;

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.sprite = sprite;
        }

        void Update()
        {
            if (level == null || level.player == null || lifeTime <= 0)
            {
                Destroy(gameObject);
                return;
            }

            float distance = speed * Time.deltaTime;
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
            lifeTime -= Time.deltaTime;
        }
    }
}
