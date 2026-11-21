using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyx.Editor
{
    [InitializeOnLoad]
    public static class ReloadInteractiveSceneRequest
    {
        const string ReloadRequest = "work/reload-interactive.request";
        const string ApplyRequest = "work/apply-percurso.request";
        const string ApplyResult = "work/apply-percurso.result";
        static bool applying;

        static ReloadInteractiveSceneRequest()
        {
            EditorApplication.update += CheckRequests;
        }

        static void CheckRequests()
        {
            if (applying || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            bool restore = File.Exists(ApplyRequest);
            if (!restore && !File.Exists(ReloadRequest))
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (restore)
                    EditorApplication.isPlaying = false;
                return;
            }
            if (!restore && SceneManager.GetActiveScene().isDirty)
                return;

            applying = true;
            try
            {
                if (restore)
                    RestoreTutorial();
                else
                    EditorSceneManager.OpenScene(ImportClaudeArt.ScenePath, OpenSceneMode.Single);
                if (File.Exists(ReloadRequest))
                    File.Delete(ReloadRequest);
                Debug.Log("NYX_INTERACTIVE_SCENE_RELOADED");
            }
            catch (Exception error)
            {
                File.WriteAllText(ApplyResult, "ERROR\n" + error);
                if (File.Exists(ApplyRequest))
                    File.Move(ApplyRequest, ApplyRequest + ".failed-" + DateTime.Now.Ticks);
                Debug.LogException(error);
            }
            finally
            {
                applying = false;
            }
        }

        static void RestoreTutorial()
        {
            string restoreFolder = File.ReadAllText(ApplyRequest).Trim();
            string[] scenes = { ImportClaudeArt.ScenePath, ImportClaudeArt.SecurityScenePath };
            foreach (string destination in scenes)
            {
                string source = Path.Combine(restoreFolder, Path.GetFileName(destination));
                if (!File.Exists(source) ||
                    !File.ReadAllText(source).Contains("Assembly-CSharp::Nyx.TutorialPercurso"))
                    throw new InvalidDataException("Cena sem percurso tutorial: " + source);
            }

            string backup = Path.Combine(restoreFolder, "antes-da-recarga-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(backup);
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                var loaded = SceneManager.GetSceneAt(index);
                if (loaded.isLoaded)
                    EditorSceneManager.SaveScene(loaded, Path.Combine(backup, "aberta-" + index + ".unity"), true);
            }
            foreach (string path in scenes)
                File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), true);

            // Descarrega a cena antiga antes de trocar os arquivos que serão abertos.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (string path in scenes)
            {
                File.Copy(Path.Combine(restoreFolder, Path.GetFileName(path)), path, true);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            var scene = EditorSceneManager.OpenScene(ImportClaudeArt.ScenePath, OpenSceneMode.Single);
            var level = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MaintenanceLevel>(true)).Single();
            var tutorial = level.GetComponent<TutorialPercurso>();
            if (tutorial == null || tutorial.trechos == null || tutorial.trechos.Length != 6 ||
                level.player == null || level.gameCamera == null)
                throw new InvalidDataException("A manutenção restaurada está incompleta.");
            OrganizarCenas.Aplicar();
            EditorSceneManager.SaveScene(scene);

            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.in2DMode = true;
                view.pivot = new Vector3(12f, 3f, 0f);
                view.size = 14f;
                view.Repaint();
            }
            Selection.activeGameObject = level.player.gameObject;
            string report = "OK\nscene=" + scene.path + "\ntutorialSections=" + tutorial.trechos.Length +
                "\nexitX=" + level.exit.position.x + "\nplayer=" + level.player.name +
                "\nbackup=" + backup;
            File.WriteAllText(ApplyResult, report);
            File.Delete(ApplyRequest);
            Debug.Log("NYX_CURRENT_TUTORIAL_APPLIED\n" + report);
        }
    }
}
