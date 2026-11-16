using UnityEngine;

namespace Nyx
{
    /// <summary>
    /// Salva um ponto seguro quando o jogador passa pelo marcador.
    /// </summary>
    public class NyxCheckpoint : MonoBehaviour
    {
        public MaintenanceLevel level;
        public SpriteRenderer markerVisual;
        public Sprite activeSprite;
        public Vector2 spawnOffset = new Vector2(0, .08f);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (level == null || other.GetComponentInParent<Prisoner>() == null)
                return;

            level.SetCheckpoint((Vector2)transform.position + spawnOffset);
            if (markerVisual != null && activeSprite != null)
                markerVisual.sprite = activeSprite;
        }
    }
}
