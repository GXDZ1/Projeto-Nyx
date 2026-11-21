using System.Collections.Generic;
using UnityEngine;

namespace Nyx
{
    // O mapa é compartilhado pelos guardas. Cada ponto representa uma posição segura sobre um piso.
    internal sealed class MapaParkourGuardas
    {
        const float Raio = .36f;
        const float Altura = 1.4f;
        const float Gravidade = 20f;
        const float Impulso = 10.6f;
        readonly List<Bounds> blocos = new List<Bounds>();
        readonly List<Vector2> pontos = new List<Vector2>();
        readonly Dictionary<int, List<Ligacao>> ligacoes = new Dictionary<int, List<Ligacao>>();

        readonly struct Ligacao
        {
            public readonly int Destino;
            public readonly float Tempo;
            public readonly float Custo;

            public Ligacao(int destino, float tempo, float custo)
            {
                Destino = destino;
                Tempo = tempo;
                Custo = custo;
            }
        }

        internal readonly struct Passo
        {
            public readonly Vector2 Ponto;
            public readonly float Salto;

            public Passo(Vector2 ponto, float salto)
            {
                Ponto = ponto;
                Salto = salto;
            }
        }

        public MapaParkourGuardas(MaintenanceLevel fase)
        {
            foreach (var piso in Object.FindObjectsByType<BoxCollider2D>())
            {
                if (piso.gameObject.scene != fase.gameObject.scene || piso.gameObject.layer != 8 ||
                    piso.isTrigger || piso.attachedRigidbody != null || !piso.enabled)
                    continue;
                blocos.Add(piso.bounds);
            }

            foreach (var piso in blocos)
            {
                float larguraUtil = piso.size.x - Raio * 2f;
                if (larguraUtil < .14f) continue;
                int trechos = Mathf.Max(1, Mathf.CeilToInt(larguraUtil / 1.2f));
                for (int i = 0; i <= trechos; i++)
                {
                    var ponto = new Vector2(piso.min.x + Raio + larguraUtil * i / trechos, piso.max.y + .02f);
                    if (CorpoLivre(ponto)) pontos.Add(ponto);
                }
            }
        }

        bool CorpoLivre(Vector2 pes)
        {
            foreach (var bloco in blocos)
                if (Ocupa(pes, bloco))
                    return false;
            return true;
        }

        static bool Ocupa(Vector2 pes, Bounds bloco) =>
            pes.x + Raio > bloco.min.x && pes.x - Raio < bloco.max.x &&
            pes.y + Altura > bloco.min.y && pes.y + .02f < bloco.max.y;

        bool TemPiso(Vector2 pes)
        {
            foreach (var bloco in blocos)
                if (pes.x > bloco.min.x && pes.x < bloco.max.x && Mathf.Abs(pes.y - bloco.max.y) < .12f)
                    return true;
            return false;
        }

        public bool PodeAndar(Vector2 inicio, Vector2 fim)
        {
            if (Mathf.Abs(inicio.y - fim.y) > .12f) return false;
            int trechos = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(fim.x - inicio.x) / .3f));
            for (int i = 0; i <= trechos; i++)
            {
                var ponto = Vector2.Lerp(inicio, fim, (float)i / trechos);
                if (!CorpoLivre(ponto) || !TemPiso(ponto)) return false;
            }
            return true;
        }

        public static Vector2 PosicaoSalto(Vector2 inicio, Vector2 fim, float tempo, float duracao)
        {
            return new Vector2(Mathf.Lerp(inicio.x, fim.x, tempo / duracao),
                inicio.y + Impulso * tempo - Gravidade * tempo * tempo * .5f);
        }

        public bool PodePular(Vector2 inicio, Vector2 fim, out float duracao, Bounds? obstaculo = null)
        {
            duracao = 0f;
            float altura = fim.y - inicio.y;
            float distancia = Mathf.Abs(fim.x - inicio.x);
            float raiz = Impulso * Impulso - 2f * Gravidade * altura;
            if (raiz < 0f || altura < -11f || distancia < .3f || distancia > 5.4f || !TemPiso(fim))
                return false;
            duracao = (Impulso + Mathf.Sqrt(raiz)) / Gravidade;
            if (distancia > duracao * 5.1f) return false;
            int amostras = Mathf.CeilToInt(duracao / .028f);
            for (int i = 1; i < amostras; i++)
            {
                var ponto = PosicaoSalto(inicio, fim, duracao * i / amostras, duracao);
                if (!CorpoLivre(ponto) || (obstaculo.HasValue && Ocupa(ponto, obstaculo.Value)))
                    return false;
            }
            return CorpoLivre(fim);
        }

        int PontoMaisProximo(Vector2 posicao, bool origem)
        {
            int melhor = -1;
            float menor = float.PositiveInfinity;
            for (int i = 0; i < pontos.Count; i++)
            {
                Vector2 delta = pontos[i] - posicao;
                if (Mathf.Abs(delta.x) > (origem ? 1.6f : 3f) ||
                    (origem ? Mathf.Abs(delta.y) > .12f : delta.y > .45f || delta.y < -8f))
                    continue;
                float custo = Mathf.Abs(delta.x) + Mathf.Abs(delta.y) * 1.6f;
                if (custo >= menor || (origem && !PodeAndar(posicao, pontos[i]))) continue;
                melhor = i;
                menor = custo;
            }
            return melhor;
        }

        List<Ligacao> Saidas(int origem)
        {
            if (ligacoes.TryGetValue(origem, out var lista)) return lista;
            lista = new List<Ligacao>();
            var inicio = pontos[origem];
            for (int i = 0; i < pontos.Count; i++)
            {
                if (i == origem) continue;
                var fim = pontos[i];
                float distancia = Mathf.Abs(fim.x - inicio.x);
                if (distancia > 5.4f) continue;
                if (PodeAndar(inicio, fim))
                {
                    if (distancia <= 1.65f) lista.Add(new Ligacao(i, 0f, distancia + .02f));
                }
                else if (PodePular(inicio, fim, out float tempo))
                    lista.Add(new Ligacao(i, tempo, distancia + 1.5f + Mathf.Abs(fim.y - inicio.y)));
            }
            ligacoes.Add(origem, lista);
            return lista;
        }

        public void Encontrar(Vector2 origem, Vector2 destino, Queue<Passo> rota)
        {
            rota.Clear();
            if (PodeAndar(origem, destino))
            {
                rota.Enqueue(new Passo(destino, 0f));
                return;
            }
            int inicio = PontoMaisProximo(origem, true);
            int fim = PontoMaisProximo(destino, false);
            if (inicio < 0 || fim < 0) return;

            var custos = new float[pontos.Count];
            var anteriores = new int[pontos.Count];
            var saltos = new float[pontos.Count];
            var fechados = new bool[pontos.Count];
            for (int i = 0; i < custos.Length; i++) { custos[i] = float.PositiveInfinity; anteriores[i] = -1; }
            custos[inicio] = 0f;
            while (true)
            {
                int atual = -1;
                float melhor = float.PositiveInfinity;
                for (int i = 0; i < custos.Length; i++)
                {
                    float estimativa = custos[i] + Mathf.Abs(pontos[i].x - pontos[fim].x);
                    if (fechados[i] || estimativa >= melhor) continue;
                    atual = i;
                    melhor = estimativa;
                }
                if (atual < 0 || atual == fim) break;
                fechados[atual] = true;
                foreach (var saida in Saidas(atual))
                {
                    int proximo = saida.Destino;
                    float custo = custos[atual] + saida.Custo;
                    if (fechados[proximo] || custo >= custos[proximo]) continue;
                    custos[proximo] = custo;
                    anteriores[proximo] = atual;
                    saltos[proximo] = saida.Tempo;
                }
            }
            if (float.IsPositiveInfinity(custos[fim])) return;

            var caminho = new Stack<Passo>();
            for (int i = fim; i != inicio; i = anteriores[i])
                caminho.Push(new Passo(pontos[i], saltos[i]));
            rota.Enqueue(new Passo(pontos[inicio], 0f));
            foreach (var passo in caminho) rota.Enqueue(passo);
        }
    }

    internal sealed class RotaGuarda
    {
        readonly Queue<MapaParkourGuardas.Passo> passos = new Queue<MapaParkourGuardas.Passo>();
        Vector2 ultimoDestino;
        float revisarEm;
        Vector2 saida, chegada;
        float tempoNoAr, duracao;
        public bool Pulando => duracao > 0f;

        public bool Mover(GuardaSeguranca guarda, Vector2 destino, float velocidade, out Vector2 posicao)
        {
            posicao = guarda.transform.position;
            var mapa = guarda.level.MapaGuardas;
            if (Time.time >= revisarEm && (passos.Count == 0 || (destino - ultimoDestino).sqrMagnitude > 1f))
            {
                mapa.Encontrar(posicao, destino, passos);
                ultimoDestino = destino;
                revisarEm = Time.time + .3f;
            }
            while (passos.Count > 0)
            {
                var passo = passos.Peek();
                if (passo.Salto > 0f)
                {
                    passos.Dequeue();
                    if (!mapa.PodePular(posicao, passo.Ponto, out float tempo))
                    {
                        passos.Clear();
                        return false;
                    }
                    saida = posicao;
                    chegada = passo.Ponto;
                    tempoNoAr = 0f;
                    duracao = tempo;
                    return true;
                }
                if ((passo.Ponto - posicao).sqrMagnitude < .0016f)
                {
                    posicao = passo.Ponto;
                    passos.Dequeue();
                    continue;
                }
                var proximidade = Vector2.MoveTowards(posicao, passo.Ponto, velocidade * Time.deltaTime);
                Vector2 movimento = proximidade - posicao;
                if (guarda.EncontrarBloqueio(posicao, movimento, out var barreira))
                {
                    // Uma caixa que se move não faz parte do mapa fixo. Pode ser saltada se houver piso atrás.
                    if (barreira.GetComponentInParent<NyxCrate>() == null) return false;
                    float x = movimento.x > 0f ? barreira.bounds.max.x + .45f : barreira.bounds.min.x - .45f;
                    var depoisDaCaixa = new Vector2(x, posicao.y);
                    if (!mapa.PodePular(posicao, depoisDaCaixa, out float salto, barreira.bounds)) return false;
                    saida = posicao;
                    chegada = depoisDaCaixa;
                    duracao = salto;
                    tempoNoAr = 0f;
                    passos.Clear();
                    return true;
                }
                posicao = proximidade;
                return true;
            }
            return false;
        }

        public Vector2 AvancarSalto()
        {
            tempoNoAr += Time.deltaTime;
            if (tempoNoAr >= duracao)
            {
                duracao = 0f;
                return chegada;
            }
            return MapaParkourGuardas.PosicaoSalto(saida, chegada, tempoNoAr, duracao);
        }
    }
}
