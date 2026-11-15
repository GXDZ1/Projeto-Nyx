using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace Nyx.Editor {
[InitializeOnLoad]
public static class MaintenanceValidation {
 static double next, deadline;
 static int step;
 static bool failed;
 static MaintenanceValidation() {
  if(SessionState.GetBool("NyxValidate",false)) EditorApplication.update+=Tick;
 }
 public static void Run() {
  EditorSceneManager.OpenScene("Assets/Nyx/Scenes/Manutencao_24.unity");
  SessionState.SetBool("NyxValidate",true);
  EditorApplication.update-=Tick; EditorApplication.update+=Tick;
  EditorApplication.isPlaying=true; deadline=EditorApplication.timeSinceStartup+90;
 }
 static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); Debug.Log("NYX_PASS "+message); }
 static void Tick() {
  if(!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
  if(deadline==0) deadline=EditorApplication.timeSinceStartup+90;
  if(EditorApplication.timeSinceStartup>deadline) { Finish(1); return; }
  if(Time.time<next) return;
  try {
   var l=UnityEngine.Object.FindAnyObjectByType<MaintenanceLevel>();
   if(l==null) return;
   switch(step++) {
    case 0: next=Time.time+1; break;
    case 1:
     Assert(l.player.Grounded,"player lands on floor");
     var animation=l.player.GetComponent<PrisonerSpriteAnimator>();
     Assert(animation!=null && animation.walkGun.Length==6 && animation.shoot.Length==3,"24px character animation frames assigned");
     Assert(l.player.sprite.sprite.texture.filterMode==FilterMode.Point,"pixel art uses crisp sampling");
     Assert(l.pads[0].pixelArt && l.pads[1].greenFrames.Length==4,"animated portal art assigned");
     InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
     InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
     InputSystem.AddDevice<Keyboard>().MakeCurrent();
     InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.D,Key.Space)); next=Time.time+.2f; break;
    case 2:
     Assert(l.player.transform.position.x>3.05f && l.player.body.linearVelocity.y>0,"keyboard movement and jump: "+l.player.transform.position+" velocity "+l.player.body.linearVelocity);
     InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
     l.PlacePortal(l.pads[0],1); l.PlacePortal(l.pads[1],2);
     l.player.Teleport(new Vector2(16,.15f)); next=Time.time+.4f; break;
    case 3:
     Assert(l.player.transform.position.x>31,"physics trigger teleports across gap");
     next=Time.time+.6f; break;
    case 4: l.energyCell.position=new Vector2(24,-5); next=Time.time+.2f; break;
    case 5: Assert(l.energyCell.position.x==36,"lost cell returns to safe ground");
     Assert(l.checkpointReached,"checkpoint saves after portal"); Assert(l.player.transform.position.x>31,"portal exit prevents teleport loop");
     l.player.Teleport(new Vector2(36,.15f)); l.Interact();
     Assert(l.Carrying,"cell can be picked up");
     l.Respawn(); Assert(!l.Carrying && l.energyCell.simulated,"fall restores carried cell");
     Assert(l.player.transform.position.x==33,"respawn uses checkpoint");
     l.player.Teleport(new Vector2(36,.15f)); l.Interact();
     l.player.Teleport(new Vector2(42,.15f)); l.Interact();
     Assert(l.powerOn && !l.securityDoor.activeSelf,"cell opens door");
     l.player.Teleport(new Vector2(58,.15f)); next=Time.time+.2f; break;
    case 6:
     Assert(l.completed,"exit completes level");
     Debug.Log("NYX_VALIDATION_OK"); Finish(0); break;
   }
  } catch(Exception e) { Debug.LogException(e); failed=true; Finish(1); }
 }
 static void Finish(int code) {
  SessionState.SetBool("NyxValidate",false); EditorApplication.update-=Tick;
  EditorApplication.Exit(failed?1:code);
 }
}
}