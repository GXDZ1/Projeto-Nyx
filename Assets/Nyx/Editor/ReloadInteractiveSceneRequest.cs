using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyx.Editor {
 [InitializeOnLoad]
 public static class ReloadInteractiveSceneRequest {
  const string RequestPath = "work/reload-interactive.request";
  static ReloadInteractiveSceneRequest() { EditorApplication.delayCall += TryReload; }

  static void TryReload() {
   if (!File.Exists(RequestPath)) return;
   if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) {
    EditorApplication.delayCall += TryReload;
    return;
   }
   if (SceneManager.GetActiveScene().isDirty) {
    Debug.LogWarning("Nyx interactive scene reload skipped because the current scene has unsaved changes.");
    return;
   }
   File.Delete(RequestPath);
   EditorSceneManager.OpenScene(ImportClaudeArt.ScenePath, OpenSceneMode.Single);
   Debug.Log("NYX_INTERACTIVE_SCENE_RELOADED");
  }
 }
}
