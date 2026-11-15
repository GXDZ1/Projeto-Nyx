using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace Nyx {
 public class MaintenanceLevel : MonoBehaviour {
  public Prisoner player;
  public Camera gameCamera;
  public PortalPad[] pads;
  public Rigidbody2D energyCell;
  public Transform handCell, socket, exit;
  public GameObject securityDoor;
  public SpriteRenderer socketIndicator;
  public Sprite boltSprite;
  public Sprite[] purplePortalFrames, greenPortalFrames;
  public bool powerOn, completed, checkpointReached;
  public int falls;
  public bool Carrying {get;private set;}
  public PortalPad FirstPortal {get;private set;}
  public PortalPad SecondPortal {get;private set;}
  Vector2 checkpoint=new Vector2(3,.15f), cellHome=new Vector2(36,.5f);
  float teleportReady,toastUntil;
  bool portalTraversalActive;
  string toast="Mire na parede e use a arma de portais.";
  void Start() {
   foreach(var p in pads) p.level=this;
   if(pads.Length>1) { pads[0].normal=Vector2.left; pads[1].normal=Vector2.right; }
   toastUntil=Time.time+5;
  }
  void Update() {
   var keys=Keyboard.current; var mouse=Mouse.current;
   if(keys!=null&&keys.rKey.wasPressedThisFrame) { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return; }
   if(completed) return;
   if(player.transform.position.y < -3) Respawn();
   if(!powerOn&&!Carrying&&energyCell.position.y < -3) ResetCell();
   if(!checkpointReached&&player.transform.position.x>31&&player.Grounded) { checkpointReached=true; checkpoint=new Vector2(33,.15f); Notify("Ponto de retorno salvo."); }
   if(Carrying&&handCell!=null) {
    handCell.position=(Vector2)player.transform.position+new Vector2(player.Facing*.48f,1.08f);
    handCell.localScale=new Vector3(player.Facing*.52f,.62f,1);
   }
   if(keys!=null&&keys.eKey.wasPressedThisFrame) Interact();
   if(mouse!=null&&mouse.leftButton.wasPressedThisFrame) Shoot(1);
   if(mouse!=null&&mouse.rightButton.wasPressedThisFrame) Shoot(2);
   if(keys!=null&&keys.digit1Key.wasPressedThisFrame) Shoot(1);
   if(keys!=null&&keys.digit2Key.wasPressedThisFrame) Shoot(2);
  }
  void LateUpdate() {
   var target=new Vector3(Mathf.Clamp(player.transform.position.x+3,14,48),4.2f,-10);
   gameCamera.transform.position=Vector3.Lerp(gameCamera.transform.position,target,1-Mathf.Exp(-7*Time.deltaTime));
  }
  void Shoot(int slot) {
   Vector2 cursor=gameCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
   int mask=(1<<8)|(1<<9);
   var colliders=Physics2D.OverlapPointAll(cursor,mask);
   PortalSurface targetSurface=null; Collider2D targetCollider=null;
   foreach(var c in colliders) {
    var surface=c.GetComponent<PortalSurface>();
    if(surface!=null&&surface.enabledSurface) { targetSurface=surface; targetCollider=c; break; }
   }
   if(targetSurface==null) { Notify("Atire apenas nas paredes brancas de portal."); return; }
   player.AimAt(cursor);
   var origin=player.WeaponTip;
   Vector2 destination=cursor, normal=Vector2.left;
   if(targetSurface!=null) {
    var ray=(cursor-origin).normalized;
    var hits=Physics2D.RaycastAll(origin,ray,Vector2.Distance(origin,cursor),mask);
    bool resolved=false;
    foreach(var hit in hits) {
     var surfaceHit=hit.collider.GetComponent<PortalSurface>();
     if(surfaceHit==targetSurface) { targetSurface.Resolve(hit.point,origin,hit.normal,out destination,out normal); resolved=true; break; }
    }
    if(!resolved) targetSurface.Resolve(cursor,origin,out destination,out normal);
   }
   float distance=Vector2.Distance(origin,destination);
   if(distance>48) { Notify("Essa superficie esta longe demais."); return; }
   var blockers=Physics2D.LinecastAll(origin,destination,1<<8);
   foreach(var hit in blockers) {
    if(hit.collider!=targetCollider&&hit.collider.GetComponent<PortalSurface>()!=targetSurface && Vector2.Distance(origin,hit.point)<distance-.18f) {
     Notify("A caixa bloqueou o disparo."); return;
    }
   }
   player.AimAt(destination);
   player.GetComponent<PrisonerSpriteAnimator>()?.Fire();
   var bolt=new GameObject("Portal projectile").AddComponent<PortalBolt>();
   var renderer=bolt.gameObject.AddComponent<SpriteRenderer>();
   renderer.sprite=boltSprite; renderer.color=slot==1?new Color(.78f,.55f,1):new Color(.35f,1,.88f);
   renderer.sortingOrder=40; bolt.transform.localScale=Vector3.one*.28f;
   bolt.Launch(this,targetSurface,null,destination,normal,slot,origin);
  }
  public void PlacePortal(PortalPad pad,int slot) {
   if(slot==1) {
    if(FirstPortal!=null&&FirstPortal!=pad) { var oldSurface=FirstPortal.surfaceOwner; FirstPortal.SetSlot(0); if(oldSurface!=null) oldSurface.RefreshPhysics(); }
    if(SecondPortal==pad) SecondPortal=null;
    FirstPortal=pad;
   } else {
    if(SecondPortal!=null&&SecondPortal!=pad) { var oldSurface=SecondPortal.surfaceOwner; SecondPortal.SetSlot(0); if(oldSurface!=null) oldSurface.RefreshPhysics(); }
    if(FirstPortal==pad) FirstPortal=null;
    SecondPortal=pad;
   }
   pad.level=this; pad.gameObject.SetActive(true); pad.SetSlot(slot);
   var owner=pad.surfaceOwner; if(owner!=null) owner.RefreshPhysics();
   var firstSurface=FirstPortal!=null?FirstPortal.surfaceOwner:null;
   var secondSurface=SecondPortal!=null?SecondPortal.surfaceOwner:null;
   if(firstSurface!=null) firstSurface.RefreshPhysics();
   if(secondSurface!=null&&secondSurface!=firstSurface) secondSurface.RefreshPhysics();
   Notify(FirstPortal!=null&&SecondPortal!=null?"Portais conectados. Atravesse uma das aberturas.":"Atire a outra cor em qualquer parede marcada.");
  }
  public void PlacePortal(PortalSurface surface,int slot,Vector2 point,Vector2 normal) {
   var pad=surface.GetPortal(slot);
   if(pad==null) {
    var obj=new GameObject("Portal opening");
    obj.layer=9;
    var trigger=obj.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(.92f,2.05f);
    pad=obj.AddComponent<PortalPad>(); pad.runtimeSpawned=true; pad.pixelArt=true; pad.level=this;
    var glowObject=new GameObject("Animated portal"); glowObject.transform.SetParent(obj.transform,false);
    glowObject.transform.localScale=new Vector3(1.55f,1.55f,1);
    pad.glow=glowObject.AddComponent<SpriteRenderer>(); pad.glow.sortingOrder=20;
    pad.purpleFrames=surface.purpleFrames!=null?surface.purpleFrames:purplePortalFrames; pad.greenFrames=surface.greenFrames!=null?surface.greenFrames:greenPortalFrames; pad.glow.sprite=slot==1?pad.purpleFrames[0]:pad.greenFrames[0];
    surface.SetPortal(slot,pad);
   }
   if(pad.surfaceOwner!=null&&pad.surfaceOwner!=surface) pad.surfaceOwner.ForgetPortal(pad);
   pad.surfaceOwner=surface;
   if(surface.GetComponent<Rigidbody2D>()!=null) pad.transform.SetParent(surface.transform,true);
   else pad.transform.SetParent(null,true);
   surface.SetPortal(slot,pad);
   pad.SetPose(point,normal); PlacePortal(pad,slot); surface.RefreshPhysics();
  }
  public void EnterPortal(PortalPad source) {
   if(completed||portalTraversalActive||Time.time<teleportReady||FirstPortal==null||SecondPortal==null) return;
   var dest=source==FirstPortal?SecondPortal:source==SecondPortal?FirstPortal:null;
   if(dest==null) return;
   portalTraversalActive=true; teleportReady=Time.time+5f;
   Vector2 sourceNormal=source.normal.sqrMagnitude>.01f?source.normal.normalized:Vector2.left;
   Vector2 destinationNormal=dest.normal.sqrMagnitude>.01f?dest.normal.normalized:Vector2.right;
   Vector2 centerOffset=player.BodyCenterOffset;
   Vector2 centerRelative=player.BodyCenter-(Vector2)source.transform.position;
   Vector2 sourceTangent=new Vector2(-sourceNormal.y,sourceNormal.x);
   bool verticalPair=Mathf.Abs(sourceNormal.x)>.7f&&Mathf.Abs(destinationNormal.x)>.7f;
   Vector2 exitNormal=verticalPair?-sourceNormal:destinationNormal;
   Vector2 tangent=new Vector2(-exitNormal.y,exitNormal.x);
   var playerBounds=player.bodyCollider.bounds;
   float playerHalfTangent=Mathf.Abs(tangent.x)*playerBounds.extents.x+Mathf.Abs(tangent.y)*playerBounds.extents.y;
   float playerHalfNormal=Mathf.Abs(destinationNormal.x)*playerBounds.extents.x+Mathf.Abs(destinationNormal.y)*playerBounds.extents.y;
   var destinationTrigger=dest.GetComponent<BoxCollider2D>();
   float portalHalfTangent=destinationTrigger!=null
    ?Mathf.Abs(tangent.x)*destinationTrigger.bounds.extents.x+Mathf.Abs(tangent.y)*destinationTrigger.bounds.extents.y
    :1.02f;
   float tangentRoom=Mathf.Max(0,portalHalfTangent-playerHalfTangent-.04f);
   float tangentOffset=Mathf.Clamp(-Vector2.Dot(centerRelative,sourceTangent),-tangentRoom,tangentRoom);
   Vector2 exitCenter=(Vector2)dest.transform.position+tangent*tangentOffset+exitNormal*(playerHalfNormal+.1f);
   Vector2 exitPoint=exitCenter-centerOffset;
   Vector2 entryVelocity=player.body.linearVelocity;
   float outwardSpeed=Mathf.Max(2f,-Vector2.Dot(entryVelocity,sourceNormal));
   Vector2 exitVelocity=exitNormal*outwardSpeed-tangent*Vector2.Dot(entryVelocity,sourceTangent);
   StartCoroutine(TraversePortal(sourceNormal,exitNormal,exitPoint,exitVelocity,playerHalfNormal));
   Notify("Portal ativo.");
  }
  IEnumerator TraversePortal(Vector2 entryNormal,Vector2 exitNormal,Vector2 exitPoint,Vector2 exitVelocity,float halfDepth) {
   var body=player.body; var collider=player.bodyCollider; var visual=player.visual;
   Vector2 start=body.position;
   Vector2 sink=start-entryNormal.normalized*(halfDepth*2f+.15f);
   Vector2 emerge=exitPoint-exitNormal.normalized*(halfDepth+.1f);
   body.simulated=false; if(collider!=null) collider.enabled=false;
   float elapsed=0;
   float enterDuration=Mathf.Clamp((halfDepth*2f+.15f)/Mathf.Max(3f,exitVelocity.magnitude),.14f,.42f);
   while(elapsed<enterDuration) {
    elapsed+=Time.deltaTime; float t=Mathf.Clamp01(elapsed/enterDuration);
    Vector2 p=Vector2.Lerp(start,sink,t); player.transform.position=new Vector3(p.x,p.y,player.transform.position.z);
    yield return null;
   }
   body.position=emerge; player.transform.position=new Vector3(emerge.x,emerge.y,player.transform.position.z);
   elapsed=0;
   float emergeDuration=Mathf.Clamp((halfDepth+.1f)/Mathf.Max(3f,exitVelocity.magnitude),.14f,.34f);
   while(elapsed<emergeDuration) {
    elapsed+=Time.deltaTime; float t=Mathf.Clamp01(elapsed/emergeDuration);
    Vector2 p=Vector2.Lerp(emerge,exitPoint,t); player.transform.position=new Vector3(p.x,p.y,player.transform.position.z);
    yield return null;
   }
   body.position=exitPoint; player.transform.position=new Vector3(exitPoint.x,exitPoint.y,player.transform.position.z);
   body.linearVelocity=exitVelocity; body.simulated=true; body.WakeUp(); if(collider!=null) collider.enabled=true;
   if(Mathf.Abs(exitVelocity.x)>.08f) player.AimAt(player.BodyCenter+Vector2.right*Mathf.Sign(exitVelocity.x));
   portalTraversalActive=false;
  }
  void SetCellCarried(bool carried) {
   Carrying=carried;
   energyCell.simulated=!carried;
   var collider=energyCell.GetComponent<Collider2D>(); if(collider!=null) collider.enabled=!carried;
   var renderer=energyCell.GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.enabled=!carried&&!powerOn;
   if(handCell!=null) handCell.gameObject.SetActive(carried);
  }
  public void Interact() {
   if(Carrying&&Vector2.Distance(player.transform.position,socket.position)<2.3f) {
    SetCellCarried(false); powerOn=true; energyCell.gameObject.SetActive(false); securityDoor.SetActive(false);
    socketIndicator.color=Color.green; Notify("Energia restaurada. Porta destrancada.");
   } else if(!powerOn&&!Carrying&&Vector2.Distance(player.transform.position,energyCell.position)<2) {
    SetCellCarried(true); Notify("Celula na mao. Leve ao terminal e aperte [E].");
   } else if(powerOn&&(Vector2.Distance(player.transform.position,exit.position)<2.6f||
     securityDoor!=null&&Vector2.Distance(player.transform.position,securityDoor.transform.position)<3.2f)) {
    completed=true; player.Stop();
    if(Application.CanStreamedLevelBeLoaded("Fuga_02")) SceneManager.LoadScene("Fuga_02");
    else Notify("A proxima fase ainda nao foi incluida na build.");
   } else if(Carrying) {
    SetCellCarried(false); energyCell.gameObject.SetActive(true);
    energyCell.position=(Vector2)player.transform.position+Vector2.up*1.35f+Vector2.right*player.Facing*.45f;
    energyCell.linearVelocity=Vector2.zero;
   }
  }
  void ResetCell() {
   energyCell.gameObject.SetActive(true); energyCell.position=cellHome;
   energyCell.simulated=true; energyCell.linearVelocity=Vector2.zero;
   var collider=energyCell.GetComponent<Collider2D>(); if(collider!=null) collider.enabled=true;
   var renderer=energyCell.GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.enabled=true;
   if(handCell!=null) handCell.gameObject.SetActive(false); Carrying=false;
  }
  public void Respawn() {
   falls++; SetCellCarried(false); if(!powerOn) ResetCell();
   player.Teleport(checkpoint,Vector2.zero); teleportReady=Time.time+.38f; Notify("Voce voltou ao ultimo ponto seguro.");
  }
  void Notify(string message) { toast=message; toastUntil=Time.time+4; }
  void OnGUI() {
   var old=GUI.matrix; GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
   var title=new GUIStyle(GUI.skin.label){fontSize=24,fontStyle=FontStyle.Bold};
   var body=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};
   GUI.Box(new Rect(18,16,760,94),"");
   GUI.Label(new Rect(34,23,720,34),"NYX  /  MANUTENCAO",title);
   string objective=powerOn?"Atravesse a porta e continue pela prisao.":Carrying?"Instale a celula no terminal [E].":checkpointReached?"Pegue a celula auxiliar [E].":"Mire nas paredes brancas; clique esquerdo: roxo, direito: verde.";
   GUI.Label(new Rect(34,59,720,42),objective,body);
   GUI.Box(new Rect(970,18,292,74),"");
   GUI.Label(new Rect(986,29,265,53),"PORTAL ROXO: "+(FirstPortal==null?"—":"READY")+"\nPORTAL VERDE: "+(SecondPortal==null?"—":"READY"),body);
   GUI.Box(new Rect(18,659,1244,45),"");
   GUI.Label(new Rect(32,667,1220,31),"A/D ou setas: andar   -   Espaco: pulo variavel   -   Mouse: disparar portais   -   E: pegar / instalar / soltar   -   R: reiniciar",body);
   if(Time.time<toastUntil) GUI.Label(new Rect(30,602,1100,45),toast,body);
   if(completed) {
    GUI.Box(new Rect(340,250,600,180),"");
    GUI.Label(new Rect(370,275,540,42),"FASE DE MANUTENCAO CONCLUIDA",title);
    GUI.Label(new Rect(370,328,540,84),"A prisao continua escura.\nA fuga continua...\n\nR para jogar novamente.",body);
   }
   GUI.matrix=old;
  }
 }
}
