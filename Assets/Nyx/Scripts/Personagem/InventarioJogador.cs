using System.Collections.Generic;
using UnityEngine;

namespace Nyx
{
    public class InventarioJogador : MonoBehaviour
    {
        const float EscalaHud = .75f;

        public MaintenanceLevel fase;
        public int capacidadeCarregador = 12;
        public int reservaInicial = 36;
        public int limiteReserva = 120;
        public float tempoRecarga = 1.1f;
        public float intervaloTiro = .4f;
        public Texture2D somentePortal, selecionadoPortal, selecionadoPistola;
        public Texture2D hudCheio, hudParcial;
        public Texture2D[] animacaoTroca, hudBaixo, hudVazio, hudSemMunicao, hudRecarregando;
        public Texture2D teclaR;

        public int Carregador { get; private set; }
        public int Reserva { get; private set; }
        public int TirosDisparados { get; private set; }
        public int MunicaoColetada { get; private set; }
        public bool JaRecarregou { get; private set; }
        public bool Recarregando { get; private set; }
        public float ProgressoRecarga => Recarregando
            ? Mathf.Clamp01((Time.time - inicioRecarga) / tempoRecarga) : 0f;
        public float AlturaHud => (fase != null && fase.weaponUnlocked ? 244f : 100f) * EscalaHud;

        float inicioRecarga, proximoTiro, avisoVazio, trocaAte;
        int salvoCarregador, salvaReserva;
        readonly Dictionary<CaixaMunicao, int> caixasSalvas = new Dictionary<CaixaMunicao, int>();
        GUIStyle numero, contadorReserva, legenda;

        void Awake()
        {
            capacidadeCarregador = Mathf.Max(1, capacidadeCarregador);
            limiteReserva = Mathf.Max(reservaInicial, limiteReserva);
            tempoRecarga = Mathf.Max(.15f, tempoRecarga);
            intervaloTiro = Mathf.Max(.4f, intervaloTiro);
            Carregador = capacidadeCarregador;
            Reserva = reservaInicial;
            salvoCarregador = Carregador;
            salvaReserva = Reserva;
        }

        void Update()
        {
            if (fase == null || fase.completed)
            {
                Recarregando = false;
                return;
            }
            if (Recarregando && Time.time - inicioRecarga >= tempoRecarga)
            {
                int quantidade = Mathf.Min(capacidadeCarregador - Carregador, Reserva);
                Carregador += quantidade;
                Reserva -= quantidade;
                Recarregando = false;
                JaRecarregou = true;
            }
        }

        public bool TentarAtirar()
        {
            if (fase == null || fase.completed || !fase.weaponUnlocked || !fase.firearmMode ||
                Recarregando || Time.time < proximoTiro)
                return false;
            if (Carregador <= 0)
            {
                if (Time.time >= avisoVazio)
                {
                    fase.Avisar(Reserva > 0 ? "Carregador vazio. Aperte R para recarregar."
                        : "Sem munição. Procure uma caixa verde ou use os portais.");
                    avisoVazio = Time.time + .8f;
                }
                return false;
            }
            Carregador--;
            TirosDisparados++;
            proximoTiro = Time.time + intervaloTiro;
            return true;
        }

        public bool TentarRecarregar()
        {
            if (fase == null || !fase.weaponUnlocked || Recarregando || fase.completed)
                return false;
            if (!fase.firearmMode)
            {
                fase.Avisar("Use Q para equipar a pistola antes de recarregar.");
                return false;
            }
            if (Carregador >= capacidadeCarregador)
            {
                fase.Avisar("O carregador já está cheio.");
                return false;
            }
            if (Reserva <= 0)
            {
                fase.Avisar("A reserva acabou. As caixas verdes fornecem mais munição.");
                return false;
            }
            inicioRecarga = Time.time;
            Recarregando = true;
            return true;
        }

        public void ArmaTrocada()
        {
            // A munição só é transferida no fim da recarga.
            Recarregando = false;
            trocaAte = Time.time + .32f;
        }

        public int ReceberMunicao(int quantidade)
        {
            if (fase == null || !fase.weaponUnlocked)
                return 0;
            int recebida = Mathf.Clamp(quantidade, 0, limiteReserva - Reserva);
            Reserva += recebida;
            MunicaoColetada += recebida;
            return recebida;
        }

        public void SalvarPontoDeRetorno()
        {
            salvoCarregador = Carregador;
            salvaReserva = Reserva;
            caixasSalvas.Clear();
            foreach (var caixa in FindObjectsByType<CaixaMunicao>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                caixasSalvas[caixa] = caixa.Restante;
        }

        public void RestaurarPontoDeRetorno()
        {
            Recarregando = false;
            Carregador = salvoCarregador;
            Reserva = salvaReserva;
            proximoTiro = 0f;
            foreach (var caixa in FindObjectsByType<CaixaMunicao>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                caixa.Restaurar(caixasSalvas.TryGetValue(caixa, out var restante) ? restante : caixa.quantidade);
        }

        void OnGUI()
        {
            if (fase == null || fase.player == null)
                return;
            if (numero == null)
            {
                numero = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight,
                    padding = new RectOffset(), normal = { textColor = Color.white }
                };
                contadorReserva = new GUIStyle(numero) { fontSize = 17, fontStyle = FontStyle.Normal };
                legenda = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white }
                };
            }
            var matrizAnterior = GUI.matrix;
            var corAnterior = GUI.color;
            float escala = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(22f * escala, Screen.height - 22f * escala, 0f),
                Quaternion.identity, new Vector3(escala * EscalaHud, escala * EscalaHud, 1f));
            GUI.color = Color.white;

            Texture2D inventario = somentePortal;
            if (fase.weaponUnlocked)
            {
                inventario = fase.firearmMode ? selecionadoPistola : selecionadoPortal;
                if (Time.time < trocaAte && animacaoTroca != null && animacaoTroca.Length >= 12)
                {
                    int frame = Mathf.Clamp((int)((1f - (trocaAte - Time.time) / .32f) * 6f), 0, 5);
                    inventario = animacaoTroca[(fase.firearmMode ? 0 : 6) + frame];
                }
            }
            if (inventario != null)
            {
                float largura = fase.weaponUnlocked ? 256f : 116f;
                float altura = fase.weaponUnlocked ? 124f : 100f;
                var area = new Rect(0f, -altura, largura, altura);
                GUI.DrawTexture(area, inventario, ScaleMode.StretchToFill, true);
                if (fase.weaponUnlocked)
                {
                    Painel(new Rect(area.x + 211f, area.y + 7f, 32f, 22f), new Color(.057f, .076f, .127f));
                    GUI.color = fase.firearmMode ? new Color(1f, .8f, .35f) : new Color(.52f, .59f, .68f);
                    GUI.Label(new Rect(area.x + 211f, area.y + 5f, 32f, 24f), Carregador.ToString("00"), contadorReserva);
                }
            }
            if (fase.weaponUnlocked)
                DesenharMunicao(new Rect(0f, -220f, 256f, 88f));
            GUI.matrix = matrizAnterior;
            GUI.color = corAnterior;
        }

        void DesenharMunicao(Rect area)
        {
            Texture2D imagem = Carregador == capacidadeCarregador ? hudCheio : hudParcial;
            bool vazio = Carregador == 0;
            if (Recarregando) imagem = Quadro(hudRecarregando, ProgressoRecarga);
            else if (vazio) imagem = Piscar(Reserva == 0 ? hudSemMunicao : hudVazio);
            else if (Carregador <= 3) imagem = Piscar(hudBaixo);
            GUI.color = Color.white;
            if (imagem != null) GUI.DrawTexture(area, imagem);
            else Painel(area, new Color(.057f, .076f, .127f));

            // Os sprites têm números de exemplo. Estes campos mostram o estado real.
            Painel(new Rect(area.x + 82f, area.y + 6f, 114f, 45f), new Color(.057f, .076f, .127f));
            GUI.color = Carregador <= 3 ? new Color(1f, .25f, .29f) : Color.white;
            GUI.Label(new Rect(area.x + 83f, area.y + 3f, 47f, 44f), Carregador.ToString("00"), numero);
            GUI.color = new Color(.65f, .7f, .78f);
            GUI.Label(new Rect(area.x + 132f, area.y + 15f, 61f, 29f), "/ " + Reserva, contadorReserva);
            if (Recarregando)
            {
                Painel(new Rect(area.x + 91f, area.y + 74f, 148f, 9f), new Color(.12f, .16f, .23f));
                Painel(new Rect(area.x + 93f, area.y + 76f, 144f * ProgressoRecarga, 5f), new Color(.15f, .88f, .82f));
            }
            else if (Carregador > 3)
            {
                Painel(new Rect(area.x + 86f, area.y + 54f, 153f, 25f), new Color(.057f, .076f, .127f));
                for (int i = 0; i < capacidadeCarregador; i++)
                    Painel(new Rect(area.x + 92f + i * 11f, area.y + 60f, 8f, 13f),
                        i < Carregador ? new Color(1f, .69f, .23f) : new Color(.22f, .26f, .34f));
            }
            GUI.color = new Color(.85f, .89f, .96f);
            GUI.Label(new Rect(area.x, area.y - 24f, 256f, 20f),
                "R recarrega  •  Q troca de arma", legenda);
        }

        static Texture2D Quadro(Texture2D[] frames, float progresso)
        {
            return frames != null && frames.Length > 0
                ? frames[Mathf.Min(frames.Length - 1, (int)(progresso * frames.Length))] : null;
        }

        static Texture2D Piscar(Texture2D[] frames) => Quadro(frames, Mathf.PingPong(Time.unscaledTime * 2f, .99f));

        static void Painel(Rect area, Color cor)
        {
            GUI.color = cor;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
        }
    }
}
