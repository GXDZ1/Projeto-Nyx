using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
namespace Nyx.Editor {
[InitializeOnLoad]
public static class NyxStartupScene {
 static NyxStartupScene() {
  EditorApplication.delayCall+=OpenIfNoScene;
  EditorApplication.update+=HandlePlayRequest;
 }
 const string PlayRequest="work/jogar-nyx.request";
 const string PlayReport="work/jogar-nyx.result";
 static double reportAt;

 [MenuItem("Nyx/Jogar/01 - Manutenção")]
 public static void PlayMaintenance() { File.WriteAllText(PlayRequest,ImportClaudeArt.ScenePath); }
 [MenuItem("Nyx/Jogar/02 - Ala de segurança")]
 public static void PlaySecurityWing() { File.WriteAllText(PlayRequest,ImportClaudeArt.SecurityScenePath); }
 [MenuItem("Nyx/Jogar/03 - Núcleo de manutenção")]
 public static void PlayCore() { File.WriteAllText(PlayRequest,ImportClaudeArt.CoreScenePath); }

 static void HandlePlayRequest() {
  if(EditorApplication.isCompiling||EditorApplication.isUpdating) return;
  if(File.Exists(PlayRequest)) {
   if(EditorApplication.isPlayingOrWillChangePlaymode) {EditorApplication.isPlaying=false;return;}
   string path=File.ReadAllText(PlayRequest).Trim();
   if(path!=ImportClaudeArt.ScenePath&&path!=ImportClaudeArt.SecurityScenePath&&path!=ImportClaudeArt.CoreScenePath) {
    File.Move(PlayRequest,PlayRequest+".invalid-"+System.DateTime.Now.Ticks);
    return;
   }
   const string refreshFlag="NyxAssetsRefreshedForPlay";
   if(!SessionState.GetBool(refreshFlag,false)) {
    SessionState.SetBool(refreshFlag,true);
    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    return;
   }
   var current=SceneManager.GetActiveScene();
   if(current.isDirty) {
    string backup="work/cena-aberta-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
    EditorSceneManager.SaveScene(current,backup,true);
   }
   EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
   File.Delete(PlayRequest);
   SessionState.EraseBool(refreshFlag);
   SessionState.SetBool("NyxReportRunningGame",true);
   var gameType=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
   if(gameType!=null) {
    var game=EditorWindow.GetWindow(gameType);
    game.Show();
    game.Focus();
    game.Repaint();
   }
   EditorApplication.EnterPlaymode();
   return;
  }
  if(!SessionState.GetBool("NyxReportRunningGame",false)||!EditorApplication.isPlaying) return;
  if(reportAt==0) {reportAt=EditorApplication.timeSinceStartup+2;return;}
  if(EditorApplication.timeSinceStartup<reportAt) return;
  reportAt=0;
  SessionState.SetBool("NyxReportRunningGame",false);
  var level=Object.FindFirstObjectByType<MaintenanceLevel>();
  var tutorial=level!=null?level.GetComponent<TutorialPercurso>():null;
  string report="scene="+SceneManager.GetActiveScene().path+
   "\nplaying="+EditorApplication.isPlaying+
   "\ntutorialSections="+(tutorial!=null?tutorial.trechos.Length:0)+
   "\nplayer="+(level!=null&&level.player!=null?level.player.transform.position.ToString():"missing")+
   "\nvisibleSprites="+Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r=>r.enabled&&r.sprite!=null&&r.isVisible)+
   "\ncamera="+(level!=null&&level.gameCamera!=null?level.gameCamera.transform.position.ToString():"missing")+
   "\nshotInterval="+(level!=null&&level.inventario!=null?level.inventario.intervaloTiro.ToString("0.0"):"missing")+
   "\nammoBoxes="+Object.FindObjectsByType<CaixaMunicao>(FindObjectsSortMode.None).Count()+
   "\nguardHealth="+string.Join(",",Object.FindObjectsByType<GuardaSeguranca>(FindObjectsSortMode.None).Select(g=>g.health.ToString("0")));
  File.WriteAllText(PlayReport,report);
  ScreenCapture.CaptureScreenshot("work/nyx-jogo-recuperado.png");
  Debug.Log("NYX_LIVE_GAME_VISIBLE\n"+report);
 }
 static void OpenIfNoScene() {
  if(!Application.isBatchMode&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) OpenLevel();
 }
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
