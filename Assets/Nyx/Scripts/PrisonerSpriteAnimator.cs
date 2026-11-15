using System.Collections.Generic;
using UnityEngine;
namespace Nyx {
 public class PrisonerSpriteAnimator : MonoBehaviour {
  public Prisoner player;
  public Sprite[] idle, walk, walkBack, idleGun, walkGun, shoot;
  public Sprite jump, fall;
  public Sprite neckPixel;
  SpriteRenderer headRenderer, neckRenderer;
  Transform headVisual, neckVisual;
  readonly Dictionary<Sprite,Sprite[]> splitFrames=new Dictionary<Sprite,Sprite[]>();
  readonly Dictionary<Sprite,float> splitHeights=new Dictionary<Sprite,float>();
  float shotUntil;
  void Awake() {
   if(player==null) player=GetComponent<Prisoner>();
   if(player==null||player.visual==null) return;
   var head=new GameObject("Aimable head"); headVisual=head.transform; headVisual.SetParent(player.visual,false);
   headRenderer=head.AddComponent<SpriteRenderer>(); headRenderer.sortingOrder=player.sprite!=null?player.sprite.sortingOrder+2:12;
   var neck=new GameObject("Neck"); neckVisual=neck.transform; neckVisual.SetParent(player.visual,false);
   neckRenderer=neck.AddComponent<SpriteRenderer>(); neckRenderer.sprite=neckPixel; neckRenderer.color=new Color(.84f,.65f,.53f); neckRenderer.sortingOrder=player.sprite!=null?player.sprite.sortingOrder-1:9;
  }
  public void Fire() { shotUntil=Time.time+.24f; }
  void LateUpdate() {
   Sprite frame;
   if(Time.time<shotUntil && (player.weapon==null||!player.weapon.gameObject.activeInHierarchy)) frame=shoot[Mathf.Clamp((int)((.24f-(shotUntil-Time.time))*12),0,shoot.Length-1)];
   else if(!player.Grounded) frame=player.body.linearVelocity.y>0 ? jump : fall;
   else {
    bool moving=Mathf.Abs(player.body.linearVelocity.x)>.1f;
    var frames=player.level.Carrying||player.weapon!=null&&player.weapon.gameObject.activeInHierarchy
      ? (moving?walk:idle)
      : (moving?walkGun:idleGun);
    if(moving&&player.WalkingBackward&&walkBack!=null&&walkBack.Length>0) frames=walkBack;
    frame=frames[(int)(Time.time*(moving?10:3))%frames.Length];
   }
   if(frame==null) return;
   if(!splitFrames.TryGetValue(frame,out var pair)) {
    var rect=frame.rect; float ppu=frame.pixelsPerUnit;
    float cut=Mathf.Clamp(Mathf.Round(rect.height*.46f),1,rect.height-1);
    float pivotX=frame.pivot.x/rect.width;
    var bodyRect=new Rect(rect.x,rect.y,rect.width,cut);
    var headRect=new Rect(rect.x,rect.y+cut,rect.width,rect.height-cut);
    var bodySprite=Sprite.Create(frame.texture,bodyRect,new Vector2(pivotX,0),ppu,0,SpriteMeshType.FullRect);
    var headSprite=Sprite.Create(frame.texture,headRect,new Vector2(pivotX,0),ppu,0,SpriteMeshType.FullRect);
    pair=new[]{bodySprite,headSprite}; splitFrames.Add(frame,pair); splitHeights.Add(frame,cut/ppu);
   }
   player.sprite.sprite=pair[0];
   player.sprite.flipX=player.Facing<0;
   if(headRenderer!=null) {
    headRenderer.sprite=pair[1]; headRenderer.flipX=player.AimFacing<0;
    headVisual.localPosition=Vector3.up*splitHeights[frame];
    float worldAngle=Mathf.Atan2(player.AimDirection.y,player.AimDirection.x)*Mathf.Rad2Deg;
    float relative=player.AimFacing<0?180f-worldAngle:worldAngle;
    headVisual.localRotation=Quaternion.Euler(0,0,Mathf.Clamp(Mathf.DeltaAngle(0,relative),-38f,38f));
   }
   if(neckRenderer!=null) {
    neckRenderer.sprite=neckPixel;
    neckVisual.localPosition=Vector3.up*(splitHeights[frame]-.08f);
    neckVisual.localScale=new Vector3(.12f,.18f,1);
   }
   player.visual.localPosition=Vector3.zero;
   player.visual.localScale=Vector3.one*2;
   player.visual.localRotation=Quaternion.identity;
  }
 }
}
