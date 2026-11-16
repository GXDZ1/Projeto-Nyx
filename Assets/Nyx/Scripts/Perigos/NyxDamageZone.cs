using UnityEngine;

namespace Nyx
{
    /// <summary>
    /// Região perigosa usada, por exemplo, sob um vão com ácido.
    /// </summary>
    public class NyxDamageZone : MonoBehaviour
    {
        public MaintenanceLevel level;
        public string deathMessage = "Você caiu no ácido.";
        Collider2D zone;

        void Awake()
        {
            zone = GetComponent<Collider2D>();
        }

        void FixedUpdate()
        {
            var player = level != null ? level.player : null;
            if (player == null || !player.body.simulated || zone == null || !zone.enabled)
                return;
            if (player.bodyCollider != null && zone.bounds.Intersects(player.bodyCollider.bounds))
                level.Respawn(deathMessage);
        }
    }
}
