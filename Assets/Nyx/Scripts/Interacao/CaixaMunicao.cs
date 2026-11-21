using UnityEngine;

namespace Nyx
{
    public class CaixaMunicao : MonoBehaviour
    {
        public MaintenanceLevel fase;
        public int quantidade = 12;
        public SpriteRenderer visual;
        public Sprite[] quadros;
        public TextMesh dica;
        public int Restante { get; private set; }

        void Awake() => Restante = quantidade;

        void Update()
        {
            if (Restante > 0 && visual != null && quadros != null && quadros.Length > 0)
                visual.sprite = quadros[(int)(Time.time * 5f) % quadros.Length];
            bool perto = Restante > 0 && fase != null && fase.player != null &&
                Vector2.Distance(fase.player.transform.position, transform.position) < 1.8f;
            if (dica != null)
            {
                dica.gameObject.SetActive(perto);
                if (perto) dica.text = fase.inventario.Reserva >= fase.inventario.limiteReserva
                    ? "RESERVA CHEIA" : "+" + Restante + " MUNIÇÃO";
            }
        }

        void OnTriggerEnter2D(Collider2D outro) => ColetarAoEncostar(outro);

        void OnTriggerStay2D(Collider2D outro) => ColetarAoEncostar(outro);

        void ColetarAoEncostar(Collider2D outro)
        {
            if (Restante <= 0 || fase == null || fase.completed || !fase.weaponUnlocked)
                return;
            if (outro.GetComponentInParent<Prisoner>() != fase.player)
                return;
            var inventario = fase.inventario;
            if (inventario != null && inventario.Reserva < inventario.limiteReserva)
                Coletar(inventario);
        }

        public bool Coletar(InventarioJogador inventario)
        {
            if (Restante <= 0)
                return false;
            if (!fase.weaponUnlocked)
            {
                fase.Avisar("Pegue a pistola antes de recolher a munição.");
                return false;
            }
            int recebida = inventario.ReceberMunicao(Restante);
            if (recebida == 0)
            {
                fase.Avisar("A reserva está cheia.");
                return false;
            }
            Restante -= recebida;
            AplicarVisual();
            fase.Avisar("+" + recebida + " munições na reserva. Use R para abastecer o carregador.");
            return true;
        }

        public void Restaurar(int restante)
        {
            Restante = Mathf.Clamp(restante, 0, quantidade);
            AplicarVisual();
        }

        void AplicarVisual()
        {
            if (visual != null) visual.enabled = Restante > 0;
            if (dica != null) dica.gameObject.SetActive(false);
            var collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = Restante > 0;
        }
    }
}
