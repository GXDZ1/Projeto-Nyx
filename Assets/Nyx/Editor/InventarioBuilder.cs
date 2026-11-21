using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nyx.Editor
{
    public static partial class ImportClaudeArt
    {
        static Texture2D InventarioTexture(string nome) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(Extras + "Inventario/" + nome + ".png");

        static Texture2D[] InventarioFrames(string nome, int quantidade) => Enumerable.Range(0, quantidade)
            .Select(i => InventarioTexture(nome + i)).ToArray();

        static void ConfigurarInventario(MaintenanceLevel fase)
        {
            var inventario = fase.GetComponent<InventarioJogador>();
            if (inventario == null) inventario = fase.gameObject.AddComponent<InventarioJogador>();
            fase.inventario = inventario;
            inventario.fase = fase;
            inventario.intervaloTiro = .4f;
            inventario.somentePortal = InventarioTexture("inventario_so_portal");
            inventario.selecionadoPortal = InventarioTexture("inventario_portal_pistola_sel_portal");
            inventario.selecionadoPistola = InventarioTexture("inventario_portal_pistola_sel_pistola");
            inventario.animacaoTroca = InventarioFrames("inventario_troca_", 12);
            inventario.hudCheio = InventarioTexture("hud_pistola_cheio");
            inventario.hudParcial = InventarioTexture("hud_pistola_parcial");
            inventario.hudBaixo = InventarioFrames("hud_pistola_baixo_", 2);
            inventario.hudVazio = InventarioFrames("hud_pistola_vazio_", 2);
            inventario.hudSemMunicao = InventarioFrames("hud_pistola_sem_municao_", 2);
            inventario.hudRecarregando = InventarioFrames("hud_pistola_recarregando_", 10);
            inventario.teclaR = InventarioTexture("tecla_r_mini");
        }

        static void CriarCaixaMunicao(MaintenanceLevel fase, float x)
        {
            var visual = PlaceExtra("Inventario", "municao_caixa_0", x, 0f, 1.2f, 7);
            visual.gameObject.name = "Caixa de munição";
            visual.transform.position += Vector3.up * (.02f - visual.sprite.bounds.min.y * 1.2f);
            var colisor = visual.gameObject.AddComponent<BoxCollider2D>();
            colisor.isTrigger = true;
            colisor.size = visual.sprite.bounds.size;
            colisor.offset = visual.sprite.bounds.center;
            var caixa = visual.gameObject.AddComponent<CaixaMunicao>();
            caixa.fase = fase;
            caixa.visual = visual;
            caixa.quadros = ExtraFrames("Inventario", "municao_caixa_", 4);

            var aviso = new GameObject("Pegar munição");
            aviso.transform.SetParent(visual.transform, false);
            aviso.transform.localPosition = new Vector3(0f, 1f, 0f);
            caixa.dica = aviso.AddComponent<TextMesh>();
            caixa.dica.text = "+12 MUNIÇÃO";
            caixa.dica.anchor = TextAnchor.MiddleCenter;
            caixa.dica.alignment = TextAlignment.Center;
            caixa.dica.fontSize = 32;
            caixa.dica.characterSize = .055f;
            caixa.dica.color = new Color(.82f, 1f, .69f);
            aviso.GetComponent<MeshRenderer>().sortingOrder = 18;
            aviso.SetActive(false);
        }
    }
}
