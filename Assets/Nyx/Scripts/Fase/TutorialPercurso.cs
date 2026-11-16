using System;
using UnityEngine;

namespace Nyx
{
    public enum AssuntoTutorial
    {
        Orientacao, Portais, Celula, Terminal, Porta, Equipamento
    }

    [Serializable]
    public class TrechoTutorial
    {
        public float inicio, fim;
        public string setor, titulo;
        [TextArea] public string dica;
        public AssuntoTutorial assunto;
        public Texture2D[] quadros;
    }

    public class TutorialPercurso : MonoBehaviour
    {
        public MaintenanceLevel fase;
        public TrechoTutorial[] trechos;
        public Texture2D teclaE, mouseEsquerdo, mouseDireito;
        public Texture2D[] trocarArma;

        TrechoTutorial trechoAtual;
        float entrada;
        float mensagemAte;
        string mensagem;
        GUIStyle legenda, titulo, texto;

        public void Avisar(string aviso)
        {
            mensagem = aviso;
            mensagemAte = Time.unscaledTime + 3f;
        }

        void OnGUI()
        {
            if (fase == null || fase.player == null)
                return;

            float agora = Time.unscaledTime;
            var trecho = fase.completed ? null : AcharTrecho(fase.player.transform.position.x);
            bool temMensagem = agora < mensagemAte;
            if (trecho == null && !temMensagem)
                return;
            if (trechoAtual != trecho)
            {
                trechoAtual = trecho;
                entrada = agora;
            }

            if (texto == null)
                CriarEstilos();

            Matrix4x4 matrizOriginal = GUI.matrix;
            Color corOriginal = GUI.color;
            float escala = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
            if (trecho != null)
                DesenharTrecho(trecho, agora - entrada);

            if (temMensagem)
            {
                float alturaTexto = texto.CalcHeight(new GUIContent(mensagem), 432f);
                float altura = alturaTexto + 16f;
                float y = Screen.height / escala - altura - 22f;
                Fundo(new Rect(22f, y, 462f, altura), new Color(.027f, .045f, .075f, .94f));
                GUI.color = new Color(1f, .86f, .66f);
                GUI.Label(new Rect(37f, y + 8f, 432f, alturaTexto), mensagem, texto);
            }
            GUI.matrix = matrizOriginal;
            GUI.color = corOriginal;
        }

        TrechoTutorial AcharTrecho(float x)
        {
            if (trechos == null)
                return null;
            foreach (var trecho in trechos)
                if (x >= trecho.inicio && x < trecho.fim)
                    return trecho;
            return null;
        }

        void DesenharTrecho(TrechoTutorial trecho, float tempo)
        {
            string chamada = trecho.titulo;
            string dica = trecho.dica;
            Texture2D arte = Quadro(trecho.quadros, tempo);
            switch (trecho.assunto)
            {
                case AssuntoTutorial.Portais:
                    dica = DicaDosPortais(out arte);
                    break;
                case AssuntoTutorial.Celula:
                    if (fase.Carrying)
                        dica = "Peguei a célula. O terminal fica mais adiante, à direita.";
                    if (fase.powerOn)
                        dica = "O terminal já tem energia. A saída está à direita.";
                    break;
                case AssuntoTutorial.Terminal:
                    if (fase.powerOn)
                        dica = "A luz ficou verde. A porta deve estar liberada.";
                    else if (!fase.Carrying)
                        dica = "Falta a célula. Ela está na bancada que você acabou de passar.";
                    break;
                case AssuntoTutorial.Porta:
                    if (!fase.powerOn)
                        dica = "A porta ainda está trancada. Volte ao terminal com a célula.";
                    break;
                case AssuntoTutorial.Equipamento:
                    if (fase.weaponUnlocked)
                    {
                        chamada = "Duas ferramentas";
                        dica = fase.firearmMode
                            ? "A pistola está na mão. Use <b>Q</b> para voltar aos portais."
                            : "O lançador está na mão. Use <b>Q</b> para sacar a pistola.";
                        arte = Quadro(trocarArma, tempo);
                    }
                    break;
            }

            if (arte == null && (trecho.assunto == AssuntoTutorial.Celula ||
                trecho.assunto == AssuntoTutorial.Terminal || trecho.assunto == AssuntoTutorial.Porta))
                arte = teclaE;

            const float x = 22f, y = 24f, largura = 496f;
            float larguraArte = arte == null ? 0f : Mathf.Min(212f, Mathf.Max(38f, arte.width));
            float larguraTexto = largura - 34f - (larguraArte > 0f ? larguraArte + 16f : 0f);
            float alturaTexto = texto.CalcHeight(new GUIContent(dica), larguraTexto);
            float altura = Mathf.Max(70f + alturaTexto, larguraArte > 38f ? 100f : 0f);
            float opacidade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tempo / .18f));
            Fundo(new Rect(x, y, largura, altura), new Color(.027f, .045f, .075f, .94f * opacidade));
            Fundo(new Rect(x, y, 4f, altura), new Color(.68f, .44f, 1f, opacidade));
            GUI.color = new Color(1f, 1f, 1f, opacidade);
            GUI.Label(new Rect(x + 16f, y + 9f, larguraTexto, 19f), trecho.setor, legenda);
            GUI.Label(new Rect(x + 16f, y + 28f, larguraTexto, 26f), chamada, titulo);
            GUI.Label(new Rect(x + 16f, y + 55f, larguraTexto, alturaTexto), dica, texto);
            if (arte != null)
                GUI.DrawTexture(new Rect(x + largura - 14f - larguraArte, y + 11f, larguraArte, 74f),
                    arte, ScaleMode.ScaleToFit, true);
        }

        string DicaDosPortais(out Texture2D arte)
        {
            arte = null;
            if (fase.FirstPortal != null && fase.SecondPortal != null)
                return "Os dois estão ligados. Pise na abertura do chão e siga à direita ao sair pelo teto.";
            var ativo = fase.FirstPortal != null ? fase.FirstPortal : fase.SecondPortal;
            bool direita = fase.FirstPortal != null;
            arte = direita ? mouseDireito : mouseEsquerdo;
            if (ativo == null)
                return "Mire na placa branca do chão e abra o portal roxo com o <b>clique esquerdo</b>.";
            string placa = ativo.normal.y > .7f ? "teto do outro lado" : "chão";
            string clique = direita ? "direito" : "esquerdo";
            return $"Falta a outra ponta. Mire na placa do {placa} e use o <b>clique {clique}</b>.";
        }

        static Texture2D Quadro(Texture2D[] imagens, float tempo)
        {
            return imagens != null && imagens.Length > 0
                ? imagens[(int)(Mathf.Max(0f, tempo) * 7f) % imagens.Length] : null;
        }

        void CriarEstilos()
        {
            legenda = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.79f, .71f, 1f) }
            };
            titulo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21, fontStyle = FontStyle.Bold, normal = { textColor = Color.white }
            };
            texto = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16, wordWrap = true, richText = true, normal = { textColor = new Color(.88f, .92f, 1f) }
            };
        }

        static void Fundo(Rect area, Color cor)
        {
            GUI.color = cor;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
        }
    }
}
