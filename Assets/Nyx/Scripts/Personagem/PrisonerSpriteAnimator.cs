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
   int weaponTopRow=12, weaponBottomRow=18;
   float torsoWidth=16f;
   bool trimWeapon=false;
   bool firingPistol=player.firearm!=null&&player.weapon==player.firearm;
   bool shotFrame=Time.time<shotUntil&&firingPistol&&shoot!=null&&shoot.Length>0;
   if(shotFrame) {
    frame=shoot[Mathf.Clamp((int)((.24f-(shotUntil-Time.time))*12),0,shoot.Length-1)];
    trimWeapon=true;
    torsoWidth=15f;
   }
   else if(!player.Grounded) {
    frame=player.body.linearVelocity.y>0 ? jump : fall;
    trimWeapon=true;
   }
   else {
    bool moving=Mathf.Abs(player.body.linearVelocity.x)>.1f;
    var armedFrames=moving?walkGun:idleGun;
    trimWeapon=player.weapon!=null&&player.weapon.gameObject.activeInHierarchy&&armedFrames!=null&&armedFrames.Length>0;
    var frames=trimWeapon?armedFrames:(moving?walk:idle);
    bool walkingBack=moving&&player.WalkingBackward;
    if(!trimWeapon&&walkingBack&&walkBack!=null&&walkBack.Length>0) frames=walkBack;
    if(frames==null||frames.Length==0) return;
    int index=(int)(Time.time*(moving?10:3))%frames.Length;
    if(trimWeapon&&walkingBack) index=frames.Length-1-index;
    frame=frames[index];
    if(trimWeapon) {
     // A arma acompanha o balanço de cada quadro. As linhas são contadas de cima.
     weaponTopRow=moving?(index==0||index==3?12:13):14+index;
     weaponBottomRow=weaponTopRow+6;
     torsoWidth=moving?15f:16f;
    }
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
    if(trimWeapon&&rect.width==24&&rect.height==24) {
     float lower=rect.height-weaponBottomRow;
     float upper=rect.height-weaponTopRow;
     TrimPaintedWeapon(bodySprite,0f,lower,upper,torsoWidth);
     TrimPaintedWeapon(headSprite,cut,lower,upper,torsoWidth);
    }
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

  static void TrimPaintedWeapon(Sprite sprite,float rowOffset,float firstWeaponRow,float lastWeaponRow,float torsoWidth) {
   // Recorta só a faixa da arma e mantém o pivô, a cabeça e as pernas no lugar.
   float height=sprite.rect.height;
   float lower=Mathf.Clamp(firstWeaponRow-rowOffset,0f,height);
   float upper=Mathf.Clamp(lastWeaponRow-rowOffset,0f,height);
   var vertices=new List<Vector2>(12);
   var triangles=new List<ushort>(18);
   AddStrip(0f,lower,sprite.rect.width);
   AddStrip(lower,upper,torsoWidth);
   AddStrip(upper,height,sprite.rect.width);
   sprite.OverrideGeometry(vertices.ToArray(),triangles.ToArray());

   void AddStrip(float bottom,float top,float width) {
    if(top<=bottom) return;
    ushort start=(ushort)vertices.Count;
    vertices.Add(new Vector2(0f,bottom));
    vertices.Add(new Vector2(0f,top));
    vertices.Add(new Vector2(width,top));
    vertices.Add(new Vector2(width,bottom));
    triangles.AddRange(new[]{start,(ushort)(start+1),(ushort)(start+2),start,(ushort)(start+2),(ushort)(start+3)});
   }
  }
 }
}
