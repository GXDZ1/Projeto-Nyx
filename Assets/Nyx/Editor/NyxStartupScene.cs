using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Nyx.Editor {
[InitializeOnLoad]
public static class NyxStartupScene {
 static NyxStartupScene() { EditorApplication.delayCall+=OpenLevel; }
 [MenuItem("Nyx/Abrir fase de manutenção")]
 public static void OpenLevel() {
  if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
  var path=ImportClaudeArt.ScenePath;
  if(!System.IO.File.Exists(path)) { if(SceneManager.GetActiveScene().isDirty) return; ImportClaudeArt.Apply(); }
  if(SceneManager.GetActiveScene().path!=path) {
   if(SceneManager.GetActiveScene().isDirty) return;
   EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
  }
  if(Application.isBatchMode) return;
  var view=SceneView.lastActiveSceneView;
  if(view==null) return;
  view.in2DMode=true;
  view.pivot=new Vector3(14,3,0);
  view.size=14;
  view.Repaint();
 }
}
}