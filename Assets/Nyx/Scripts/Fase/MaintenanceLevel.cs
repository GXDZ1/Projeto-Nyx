using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace Nyx {
 // Reúne as regras da fase: portais, objetivos, câmera, perigos e tutorial.
 public class MaintenanceLevel : MonoBehaviour {
  const float PortalShotRange = 48f;
  const float PortalSurfaceSeamTolerance = .02f;
  const int PortalShotMask = (1 << 8) | (1 << 9);
  public Prisoner player;
  public Camera gameCamera;
  public PortalPad[] pads;
  public Rigidbody2D energyCell;
  public Transform handCell, socket, exit;
  public GameObject securityDoor;
  public SpriteRenderer doorRenderer;
  public Sprite closedDoorSprite, openDoorSprite, weaponBulletSprite, energyCellSprite;
  public GameObject weaponPickup;
  public SpriteRenderer socketIndicator;
  public Sprite boltSprite;
  public Sprite[] purplePortalFrames, greenPortalFrames;
  public bool powerOn, completed, checkpointReached;
  public bool phaseTwo, weaponUnlocked, firearmMode;
  public float cameraMinX=14f, cameraMaxX=132f;
  public Rect cameraRoom;
  public int falls;
  public bool Carrying {get;private set;}
  public PortalPad FirstPortal {get;private set;}
  public PortalPad SecondPortal {get;private set;}
  Vector2 checkpoint=new Vector2(3,.15f), cellHome=new Vector2(36,.5f);
  float teleportReady;
  bool portalTraversalActive;
  bool portalReentryLocked;
  float portalCameraX;
  float cameraDesignSize;
  readonly Collider2D[] portalExitHits=new Collider2D[16];
  TutorialPercurso tutorial;

  void Start() {
   tutorial=GetComponent<TutorialPercurso>();
   if(!phaseTwo&&energyCell==null) CreateFallbackEnergyCell();
   if(pads!=null) {
    foreach(var pad in pads) if(pad!=null) pad.level=this;
    if(pads.Length>1&&pads[0]!=null&&pads[1]!=null) {
     pads[0].normal=Vector2.left;
     pads[1].normal=Vector2.right;
    }
   }
   if(securityDoor!=null) securityDoor.SetActive(true);
   if(doorRenderer==null&&securityDoor!=null) doorRenderer=securityDoor.GetComponent<SpriteRenderer>();
   SetDoorOpen(powerOn);
   if(energyCell!=null) {
    cellHome=energyCell.position;
    if(!powerOn) energyCell.gameObject.SetActive(true);
   }
   if(player!=null) player.SetWeaponMode(false);
   if(player!=null) checkpoint=player.transform.position;
   if(gameCamera!=null) {
    cameraDesignSize=gameCamera.orthographicSize;
    FollowCamera(1f);
   }
  }

  void CreateFallbackEnergyCell() {
   var cell=new GameObject("Célula auxiliar");
   cell.transform.position=cellHome;
   cell.layer=8;
   var renderer=cell.AddComponent<SpriteRenderer>(); renderer.sprite=energyCellSprite; renderer.color=Color.white; renderer.sortingOrder=8;
   var collider=cell.AddComponent<BoxCollider2D>(); collider.size=new Vector2(.8f,.8f);
   var rigidbody=cell.AddComponent<Rigidbody2D>(); rigidbody.gravityScale=.8f; rigidbody.constraints=RigidbodyConstraints2D.FreezeRotation;
   energyCell=rigidbody;
  }
  void Update() {
   var keys=Keyboard.current; var mouse=Mouse.current;
   if(portalReentryLocked&&Time.time>=teleportReady&&!PlayerOverlapsPortal()) portalReentryLocked=false;
   if(keys!=null&&keys.rKey.wasPressedThisFrame) { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return; }
   if(completed||player==null||portalTraversalActive) return;
   if(keys!=null&&keys.qKey.wasPressedThisFrame&&weaponUnlocked) {
    firearmMode=!firearmMode;
    player.SetWeaponMode(firearmMode);
    Notify(firearmMode?"Pistola equipada. Q alterna para o lançador de portais.":"Lançador de portais equipado. Q alterna para a pistola.");
   }
   if(player.transform.position.y < -3) Respawn();
   if(energyCell!=null&&!powerOn&&!Carrying&&energyCell.position.y < -3) {
    ResetCell();
    Notify("A célula voltou ao ponto onde você a encontrou.");
   }
   if(Carrying&&handCell!=null) {
    handCell.position=(Vector2)player.transform.position+new Vector2(player.Facing*.48f,1.08f);
    handCell.localScale=new Vector3(player.Facing*.52f,.62f,1);
   }
   if(keys!=null&&keys.eKey.wasPressedThisFrame) Interact();
   if(mouse!=null&&mouse.leftButton.wasPressedThisFrame) {
    if(firearmMode) FireWeapon(); else Shoot(1);
   }
   if(mouse!=null&&mouse.rightButton.wasPressedThisFrame&&!firearmMode) Shoot(2);
   if(keys!=null&&keys.digit1Key.wasPressedThisFrame&&!firearmMode) Shoot(1);
   if(keys!=null&&keys.digit2Key.wasPressedThisFrame&&!firearmMode) Shoot(2);
  }
  void LateUpdate() {
   FollowCamera(1-Mathf.Exp(-(portalTraversalActive?11f:7f)*Time.deltaTime));
  }

  void FollowCamera(float amount) {
   if(player==null||gameCamera==null) return;
   float followX=(portalTraversalActive?portalCameraX:player.transform.position.x)+3f;
   if(cameraRoom.width<=0f||cameraRoom.height<=0f) {
    var target=new Vector3(Mathf.Clamp(followX,cameraMinX,cameraMaxX),4.2f,-10);
    gameCamera.transform.position=Vector3.Lerp(gameCamera.transform.position,target,amount);
    return;
   }

   // Limita as bordas da imagem, incluindo quando a janela muda de proporção.
   float aspect=Mathf.Max(.01f,gameCamera.aspect);
   float height=Mathf.Min(cameraDesignSize,cameraRoom.height*.5f,cameraRoom.width/(2f*aspect));
   gameCamera.orthographicSize=height;
   float halfWidth=height*aspect;
   float x=Mathf.Lerp(gameCamera.transform.position.x,followX,amount);
   float minX=cameraRoom.xMin+halfWidth, maxX=cameraRoom.xMax-halfWidth;
   float minY=cameraRoom.yMin+height, maxY=cameraRoom.yMax-height;
   gameCamera.transform.position=new Vector3(
    minX<maxX?Mathf.Clamp(x,minX,maxX):cameraRoom.center.x,
    minY<maxY?Mathf.Clamp(4.2f,minY,maxY):cameraRoom.center.y,-10);
  }
  void Shoot(int slot) {
   var mouse=Mouse.current;
   if(mouse==null) return;
   Vector2 cursor=gameCamera.ScreenToWorldPoint(mouse.position.ReadValue());
   player.AimAt(cursor);
   Vector2 origin=player.WeaponTip;
   Vector2 toCursor=cursor-origin;
   Vector2 direction=Vector2.Dot(toCursor,player.AimDirection)>.001f
    ?toCursor.normalized:player.AimDirection;

   RaycastHit2D blocker=default;
   foreach(var hit in Physics2D.RaycastAll(origin,direction,PortalShotRange,PortalShotMask)) {
    // The solid floor and its portal plate share an edge, so allow coincident hits.
    if(blocker.collider!=null&&hit.distance>blocker.distance+PortalSurfaceSeamTolerance) break;
    var surface=hit.collider.GetComponent<PortalSurface>();
    if(surface!=null&&surface.enabledSurface) {
     // A ray starting inside the target must still point into its exposed face.
     if(!surface.snapToEdge&&Vector2.Dot(direction,surface.PlacementNormal)>=-.005f) {
      if(!hit.collider.isTrigger&&blocker.collider==null) blocker=hit;
      continue;
     }
     surface.Resolve(hit.point,hit.normal,out var point,out var normal);
     FirePortalBolt(slot,surface,origin,hit.point,point,normal);
     return;
    }
    if(!hit.collider.isTrigger&&blocker.collider==null) blocker=hit;
   }
   if(blocker.collider!=null&&blocker.collider.GetComponentInParent<NyxCrate>()!=null) {
    Notify("Uma caixa está no caminho do disparo.");
    return;
   }
   Notify("Mire na direção de uma placa branca para abrir um portal.");
  }

  void FireWeapon() {
   if(!weaponUnlocked||player==null||gameCamera==null) return;
   var mouse=Mouse.current;
   if(mouse==null) return;
   Vector2 cursor=gameCamera.ScreenToWorldPoint(mouse.position.ReadValue());
   player.AimAt(cursor);
   Vector2 origin=player.WeaponTip;
   Vector2 direction=(cursor-origin).sqrMagnitude>.001f?(cursor-origin).normalized:player.AimDirection;
   float range=22f;
   Vector2 end=origin+direction*range;
   foreach(var hit in Physics2D.RaycastAll(origin,direction,range)) {
    if(hit.collider==null||hit.collider.transform.IsChildOf(player.transform)) continue;
    var guard=hit.collider.GetComponentInParent<GuardaSeguranca>();
    if(guard!=null) { guard.TakeDamage(25,direction); end=hit.point; break; }
    if(!hit.collider.isTrigger) { end=hit.point; break; }
   }
   if(weaponBulletSprite!=null) {
    var shot=new GameObject("Disparo da pistola");
    var renderer=shot.AddComponent<SpriteRenderer>(); renderer.sprite=weaponBulletSprite; renderer.sortingOrder=30;
    shot.transform.position=origin; shot.transform.localScale=Vector3.one*.22f;
    StartCoroutine(AnimateWeaponShot(shot.transform,origin,end));
   }
   player.GetComponent<PrisonerSpriteAnimator>()?.Fire();
  }

  IEnumerator AnimateWeaponShot(Transform shot,Vector2 start,Vector2 end) {
   const float duration=.12f; float elapsed=0;
   while(elapsed<duration&&shot!=null) {
    elapsed+=Time.deltaTime;
    shot.position=Vector2.Lerp(start,end,Mathf.Clamp01(elapsed/duration));
    yield return null;
   }
   if(shot!=null) Destroy(shot.gameObject);
  }

  void FirePortalBolt(int slot,PortalSurface surface,Vector2 origin,Vector2 impact,Vector2 point,Vector2 normal) {
   player.GetComponent<PrisonerSpriteAnimator>()?.Fire();
   var bolt=new GameObject("Portal projectile").AddComponent<PortalBolt>();
   var renderer=bolt.gameObject.AddComponent<SpriteRenderer>();
   renderer.sprite=boltSprite; renderer.color=slot==1?new Color(.78f,.55f,1):new Color(.35f,1,.88f);
   renderer.sortingOrder=40; bolt.transform.localScale=Vector3.one*.28f;
   bolt.Launch(this,surface,origin,impact,point,normal,slot);
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
  }
  public void PlacePortal(PortalSurface surface,int slot,Vector2 point,Vector2 normal) {
   // A segunda cor substitui a primeira quando se mira na mesma placa.
   var other=slot==1?SecondPortal:FirstPortal;
   if(other!=null&&other.surfaceOwner==surface) {
    other.SetSlot(0);
    if(slot==1) SecondPortal=null; else FirstPortal=null;
   }
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
   if(completed||portalTraversalActive||portalReentryLocked||Time.time<teleportReady||FirstPortal==null||SecondPortal==null) return;
   var dest=source==FirstPortal?SecondPortal:source==SecondPortal?FirstPortal:null;
   if(dest==null||player==null||player.bodyCollider==null||!player.body.simulated) return;
   Vector2 sourceNormal=source.normal.normalized;
   Vector2 centerRelative=player.BodyCenter-(Vector2)source.transform.position;
   if(source.surfaceOwner!=null&&(Vector2.Dot(centerRelative,sourceNormal)<0||
     !player.IsMovingIntoPortal(sourceNormal))) return;

   Vector2 exitNormal=dest.normal.normalized;
   if(dest.surfaceOwner==null&&Mathf.Abs(sourceNormal.x)>.7f&&Mathf.Abs(exitNormal.x)>.7f)
    exitNormal=-sourceNormal;
   var bounds=player.bodyCollider.bounds;
   var exitBody=new Bounds((Vector2)bounds.center-player.body.position,bounds.size);
   if(!TryGetPortalExit(source,dest,exitNormal,player.body,bounds,out var exitPoint,out var exitVelocity)) {
    Notify("A saída do portal está bloqueada.");
    teleportReady=Time.time+.35f;
    return;
   }

   portalTraversalActive=true; portalReentryLocked=true;
   StartCoroutine(TraversePortal(source,dest,sourceNormal,exitNormal,exitPoint,exitVelocity,exitBody));
  }

  public void EnterPortal(PortalPad source,NyxCrate crate) {
   if(completed||!crate.CanEnterPortal(source)) return;
   var destination=source==FirstPortal?SecondPortal:source==SecondPortal?FirstPortal:null;
   if(destination==null||destination==source) return;
   if(TryGetPortalExit(source,destination,destination.normal,crate.Body,crate.Collider.bounds,
     out var position,out var velocity))
    crate.ExitPortal(destination,position,velocity);
  }

  bool TryGetPortalExit(PortalPad source,PortalPad destination,Vector2 exitNormal,
   Rigidbody2D traveler,Bounds bounds,out Vector2 point,out Vector2 velocity) {
   Vector2 sourceNormal=source.normal;
   Vector2 sourceTangent=new Vector2(-sourceNormal.y,sourceNormal.x);
   Vector2 exitTangent=new Vector2(-exitNormal.y,exitNormal.x);
   Vector2 centerOffset=(Vector2)bounds.center-traveler.position;
   Vector2 centerRelative=(Vector2)bounds.center-(Vector2)source.transform.position;
   var destinationTrigger=destination.GetComponent<Collider2D>();
   float opening=destinationTrigger!=null?ExtentAlong(destinationTrigger.bounds.size,exitTangent):1.02f;
   float room=Mathf.Max(0f,opening-ExtentAlong(bounds.size,exitTangent)-.04f);
   float offset=Mathf.Clamp(-Vector2.Dot(centerRelative,sourceTangent),-room,room);
   float depth=ExtentAlong(bounds.size,exitNormal);
   Vector2 exitBase=(Vector2)destination.transform.position+exitNormal*(depth+.13f)-centerOffset;
   var exitBody=new Bounds(centerOffset,bounds.size);

   Vector2 entryVelocity=traveler.linearVelocity;
   float outwardSpeed=Mathf.Clamp(-Vector2.Dot(entryVelocity,sourceNormal),2.8f,10f);
   float tangentSpeed=Mathf.Clamp(-Vector2.Dot(entryVelocity,sourceTangent),-4.5f,4.5f);
   velocity=exitNormal*outwardSpeed+exitTangent*tangentSpeed;
   return TryFindPortalExit(exitBase,exitTangent,offset,room,exitBody,traveler,out point);
  }
  static float ExtentAlong(Vector2 size,Vector2 direction) =>
   (Mathf.Abs(direction.x)*size.x+Mathf.Abs(direction.y)*size.y)*.5f;

  bool TryFindPortalExit(Vector2 exitBase,Vector2 tangent,float preferred,float room,Bounds body,Rigidbody2D traveler,out Vector2 point) {
   // Todos os candidatos ficam na face livre da placa e dentro da abertura.
   point=exitBase+tangent*preferred;
   if(PortalExitIsClear(point,body,traveler)) return true;
   point=exitBase;
   if(PortalExitIsClear(point,body,traveler)) return true;
   point=exitBase+tangent*room;
   if(room>.01f&&PortalExitIsClear(point,body,traveler)) return true;
   point=exitBase-tangent*room;
   return room>.01f&&PortalExitIsClear(point,body,traveler);
  }
  bool PortalExitIsClear(Vector2 position,Bounds body,Rigidbody2D traveler) {
   var filter=new ContactFilter2D { useTriggers=true };
   filter.SetLayerMask(player.groundMask);
   int count=Physics2D.OverlapBox(position+(Vector2)body.center,body.size,0,filter,portalExitHits);
   for(int i=0;i<count;i++) {
    var hit=portalExitHits[i];
    if(hit.attachedRigidbody==traveler) continue;
    if(!hit.isTrigger||hit.TryGetComponent<NyxDamageZone>(out _)) return false;
   }
   return true;
  }
  bool PlayerOverlapsPortal() {
   if(player==null||player.bodyCollider==null) return false;
   var bounds=player.bodyCollider.bounds;
   foreach(var portal in new[]{FirstPortal,SecondPortal}) {
    if(portal==null) continue;
    var portalCollider=portal.GetComponent<Collider2D>();
    if(portalCollider!=null&&portalCollider.enabled&&bounds.Intersects(portalCollider.bounds)) return true;
   }
   return false;
  }
  IEnumerator TraversePortal(PortalPad source,PortalPad dest,Vector2 entryNormal,Vector2 exitNormal,Vector2 exitPoint,Vector2 exitVelocity,Bounds exitBody) {
   var body=player.body; var collider=player.bodyCollider;
   Vector2 start=body.position;
   float artDepth=GetVisualHalfDepth(entryNormal);
   Vector2 entryCenter=player.BodyCenter;
   float entryDistance=Vector2.Dot(entryCenter-(Vector2)source.transform.position,entryNormal);
   Vector2 sink=start-entryNormal*(entryDistance+artDepth+.22f);
   float exitArtDepth=GetVisualHalfDepth(exitNormal);
   Vector2 emerge=exitPoint-exitNormal*(ExtentAlong(exitBody.size,exitNormal)+exitArtDepth+.22f);
   portalCameraX=player.transform.position.x;
   // A coroutine anima o corpo entre dois pontos sem a física interferir no trajeto.
   // O recorte esconde a parte do sprite que já passou pela superfície do portal.
   bool traversalFinished=false;
   try {
    using(var traversalMask=new PortalTraversalMask(player.transform)) {
    traversalMask.SetPlane(source.transform.position,entryNormal);
    body.simulated=false; if(collider!=null) collider.enabled=false;
    player.transform.position=new Vector3(start.x,start.y,player.transform.position.z);
    float elapsed=0;
    const float enterDuration=.24f;
    while(elapsed<enterDuration) {
     elapsed+=Time.deltaTime; float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/enterDuration));
     Vector2 p=Vector2.Lerp(start,sink,t); player.transform.position=new Vector3(p.x,p.y,player.transform.position.z);
     portalCameraX=Mathf.Lerp(start.x, sink.x, t*.35f);
     yield return null;
    }
    // Um clarão branco bem curto marca a troca de lado sem quebrar a travessia suave.
    var flashRenderers=player.GetComponentsInChildren<SpriteRenderer>(true);
    var flashColors=new Color[flashRenderers.Length];
    var flashModes=new SpriteMaskInteraction[flashRenderers.Length];
    for(int i=0;i<flashRenderers.Length;i++) {
     flashColors[i]=flashRenderers[i].color; flashModes[i]=flashRenderers[i].maskInteraction;
     flashRenderers[i].color=Color.white; flashRenderers[i].maskInteraction=SpriteMaskInteraction.None;
    }
    player.transform.position=new Vector3(start.x,start.y,player.transform.position.z);
    yield return new WaitForSeconds(.055f);
    // The character is fully behind the first opening before the position changes.
    traversalMask.SetPlane(dest.transform.position,exitNormal);
    body.position=emerge; player.transform.position=new Vector3(emerge.x,emerge.y,player.transform.position.z);
    for(int i=0;i<flashRenderers.Length;i++) if(flashRenderers[i]!=null) {
     flashRenderers[i].color=flashColors[i]; flashRenderers[i].maskInteraction=flashModes[i];
    }
    portalCameraX=Mathf.Lerp(start.x,exitPoint.x,.72f);
    elapsed=0;
    const float emergeDuration=.28f;
    while(elapsed<emergeDuration) {
     elapsed+=Time.deltaTime; float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/emergeDuration));
     Vector2 p=Vector2.Lerp(emerge,exitPoint,t); player.transform.position=new Vector3(p.x,p.y,player.transform.position.z);
     portalCameraX=Mathf.Lerp(Mathf.Lerp(start.x,exitPoint.x,.72f),exitPoint.x,t);
     yield return null;
    }
    }
    traversalFinished=true;
   }
   finally {
    if(body!=null) {
     bool clear=traversalFinished&&PortalExitIsClear(exitPoint,exitBody,body);
     Vector2 position=clear?exitPoint:PortalExitIsClear(start,exitBody,body)?start:checkpoint;
     body.position=position;
     player.transform.position=new Vector3(position.x,position.y,player.transform.position.z);
     body.simulated=true;
     if(collider!=null) collider.enabled=true;
     player.Teleport(position,clear?exitVelocity:Vector2.zero);
     if(clear&&Mathf.Abs(exitVelocity.x)>.08f)
      player.AimAt(player.BodyCenter+Vector2.right*Mathf.Sign(exitVelocity.x));
     teleportReady=Time.time+(clear?5f:.35f);
     portalReentryLocked=clear;
     if(!clear&&traversalFinished) Notify("A saída do portal foi bloqueada.");
    }
    portalTraversalActive=false;
   }
  }

  float GetVisualHalfDepth(Vector2 axis) {
   var renderers=player.GetComponentsInChildren<SpriteRenderer>(true);
   float extent=.9f;
   foreach(var renderer in renderers) {
    if(renderer==null||!renderer.enabled) continue;
    Bounds b=renderer.bounds;
    float center=Mathf.Abs(Vector2.Dot((Vector2)b.center-(Vector2)player.transform.position,axis));
    float projected=Mathf.Abs(axis.x)*b.extents.x+Mathf.Abs(axis.y)*b.extents.y;
    extent=Mathf.Max(extent,center+projected);
   }
   return extent;
  }

  sealed class PortalTraversalMask : System.IDisposable {
   readonly SpriteMask mask;
   readonly Sprite sprite;
   readonly Texture2D texture;
   readonly SpriteRenderer[] renderers;
   readonly SpriteMaskInteraction[] originalModes;

   public PortalTraversalMask(Transform root) {
    renderers=root.GetComponentsInChildren<SpriteRenderer>(true);
    originalModes=new SpriteMaskInteraction[renderers.Length];
    int backLayer=int.MaxValue,frontLayer=int.MinValue;
    int backLayerID=0,frontLayerID=0;
    for(int i=0;i<renderers.Length;i++) {
     var renderer=renderers[i];
     originalModes[i]=renderer.maskInteraction;
     renderer.maskInteraction=SpriteMaskInteraction.VisibleOutsideMask;
     int order=SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
     if(order<backLayer) { backLayer=order; backLayerID=renderer.sortingLayerID; }
     if(order>frontLayer) { frontLayer=order; frontLayerID=renderer.sortingLayerID; }
    }
    texture=new Texture2D(64,64,TextureFormat.RGBA32,false) { name="Portal traversal mask",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };
    var pixels=new Color[64*64]; for(int i=0;i<pixels.Length;i++) pixels[i]=Color.white;
    texture.SetPixels(pixels); texture.Apply(false,true);
    sprite=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(0,.5f),100);
    var go=new GameObject("Portal traversal clip"); go.hideFlags=HideFlags.HideAndDontSave;
    mask=go.AddComponent<SpriteMask>(); mask.sprite=sprite; mask.isCustomRangeActive=true;
    mask.backSortingLayerID=backLayerID; mask.frontSortingLayerID=frontLayerID;
    mask.backSortingOrder=-32768; mask.frontSortingOrder=32767;
    go.transform.localScale=new Vector3(100,100,1);
   }

   public void SetPlane(Vector2 point,Vector2 outwardNormal) {
    // A máscara cobre o lado de dentro do portal; o resto do personagem continua visível.
    Vector2 inward=-outwardNormal.normalized;
    mask.transform.SetPositionAndRotation(point,Quaternion.Euler(0,0,Mathf.Atan2(inward.y,inward.x)*Mathf.Rad2Deg));
   }

   public void Dispose() {
    for(int i=0;i<renderers.Length;i++) if(renderers[i]!=null) renderers[i].maskInteraction=originalModes[i];
    if(mask!=null) Object.Destroy(mask.gameObject);
    if(sprite!=null) Object.Destroy(sprite);
    if(texture!=null) Object.Destroy(texture);
   }
  }
  void SetCellCarried(bool carried) {
   if(energyCell==null) return;
   Carrying=carried;
   energyCell.simulated=!carried;
   var collider=energyCell.GetComponent<Collider2D>(); if(collider!=null) collider.enabled=!carried;
   var renderer=energyCell.GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.enabled=!carried&&!powerOn;
   if(handCell!=null) handCell.gameObject.SetActive(carried);
  }
  public void Interact() {
   if(weaponPickup!=null&&!weaponUnlocked&&Vector2.Distance(player.transform.position,weaponPickup.transform.position)<2.2f) {
    weaponUnlocked=true; firearmMode=true; player.SetWeaponMode(true);
    weaponPickup.SetActive(false); Notify("Pistola coletada. Aperte Q para alternar entre ela e os portais.");
    return;
   }
   if(phaseTwo) {
    if(exit!=null&&Vector2.Distance(player.transform.position,exit.position)<2.6f) {
     completed=true; player.Stop(); Notify("Fim desta parte da fuga. A saída foi alcançada.");
     var renderer=exit.GetComponent<SpriteRenderer>();
     if(renderer!=null&&openDoorSprite!=null) renderer.sprite=openDoorSprite;
    } else if(weaponPickup!=null&&!weaponUnlocked&&Vector2.Distance(player.transform.position,weaponPickup.transform.position)<3.2f) {
     Notify("Aperte E para pegar a pistola.");
    }
    return;
   }
   if(energyCell==null||socket==null||securityDoor==null) return;
   if(Carrying&&Vector2.Distance(player.transform.position,socket.position)<2.3f) {
    SetCellCarried(false); powerOn=true; energyCell.gameObject.SetActive(false); SetDoorOpen(true);
    if(socketIndicator!=null) socketIndicator.color=Color.green;
   } else if(!powerOn&&!Carrying&&Vector2.Distance(player.transform.position,energyCell.position)<2) {
    SetCellCarried(true);
   } else if(powerOn&&((exit!=null&&Vector2.Distance(player.transform.position,exit.position)<2.6f)||
     Vector2.Distance(player.transform.position,securityDoor.transform.position)<3.2f)) {
    if(Application.CanStreamedLevelBeLoaded("Fuga_02")) {
     completed=true; player.Stop(); SceneManager.LoadScene("Fuga_02");
    } else {
     Notify("A segunda fase ainda não está habilitada nas cenas do jogo.");
    }
   } else if(!powerOn&&Vector2.Distance(player.transform.position,securityDoor.transform.position)<3.2f) {
    Notify("A porta está sem energia. Leve a célula até o terminal.");
   } else if(Carrying) {
    SetCellCarried(false); energyCell.gameObject.SetActive(true);
    energyCell.position=(Vector2)player.transform.position+Vector2.up*1.35f+Vector2.right*player.Facing*.45f;
    energyCell.linearVelocity=Vector2.zero;
   }
  }
  void ResetCell() {
   if(energyCell==null) return;
   energyCell.gameObject.SetActive(true); energyCell.position=cellHome;
   energyCell.simulated=true; energyCell.linearVelocity=Vector2.zero;
   var collider=energyCell.GetComponent<Collider2D>(); if(collider!=null) collider.enabled=true;
   var renderer=energyCell.GetComponent<SpriteRenderer>(); if(renderer!=null) renderer.enabled=true;
   if(handCell!=null) handCell.gameObject.SetActive(false); Carrying=false;
  }
  public void Respawn(string message="Você voltou ao último ponto seguro.") {
   if(portalTraversalActive||completed) return;
   falls++; SetCellCarried(false); if(energyCell!=null&&!powerOn) ResetCell();
   player.Teleport(checkpoint,Vector2.zero);
   if(!portalReentryLocked) teleportReady=Time.time+.38f;
   Notify(message);
  }

  void SetDoorOpen(bool open) {
   if(securityDoor!=null) securityDoor.SetActive(true);
   if(doorRenderer==null&&securityDoor!=null) doorRenderer=securityDoor.GetComponent<SpriteRenderer>();
   if(doorRenderer!=null) {
    Sprite sprite=open?openDoorSprite:closedDoorSprite;
    if(sprite!=null) doorRenderer.sprite=sprite;
    doorRenderer.enabled=true;
   }
   if(securityDoor!=null) {
    var collider=securityDoor.GetComponent<Collider2D>();
    if(collider!=null) collider.enabled=false;
   }
  }

  public void SetCheckpoint(Vector2 position) {
   if(Vector2.Distance(checkpoint,position)<.5f) return;
   checkpoint=position;
   checkpointReached=true;
   Notify("Ponto de retorno salvo.");
  }

  void Notify(string message) => tutorial?.Avisar(message);
 }
}
