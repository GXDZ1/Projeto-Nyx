using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nyx.Editor
{
    [InitializeOnLoad]
    public static class OrganizarCenas
    {
        const string Pedido = "work/organizar-cenas.request";
        const string Resultado = "work/organizar-cenas.result";
        static readonly EditorBuildSettingsScene[] Fases =
        {
            new EditorBuildSettingsScene("Assets/Nyx/Scenes/Manutencao_Interativa.unity", true),
            new EditorBuildSettingsScene("Assets/Nyx/Scenes/Fuga_02.unity", true)
        };

        static OrganizarCenas()
        {
            EditorApplication.update += ConferirPedido;
        }

        [MenuItem("Nyx/Organizar cenas das fases")]
        public static void Aplicar()
        {
            foreach (var fase in Fases)
            {
                if (!File.Exists(fase.path))
                    throw new FileNotFoundException("Cena da fase não encontrada", fase.path);
            }

            EditorBuildSettings.scenes = Fases;
            AssetDatabase.SaveAssets();
            Debug.Log("NYX_PHASE_SCENES_READY");
        }

        static void ConferirPedido()
        {
            if (!File.Exists(Pedido) || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            File.Delete(Pedido);
            try
            {
                Aplicar();
                File.WriteAllText(Resultado, "OK");
            }
            catch (System.Exception erro)
            {
                File.WriteAllText(Resultado, erro.ToString());
                Debug.LogException(erro);
            }
        }
    }
}
