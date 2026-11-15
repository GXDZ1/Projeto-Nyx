using UnityEngine;
namespace Nyx {
 public class PortalPad : MonoBehaviour {
  public MaintenanceLevel level;
  public SpriteRenderer glow, core;
  public TextMesh caption;
  public Sprite[] purpleFrames, greenFrames;
  public bool pixelArt, runtimeSpawned;
  public PortalSurface surfaceOwner;
  public Vector2 normal=Vector2.left;
  public int Slot {get; private set;}
  public void SetPose(Vector2 center,Vector2 face) {
   normal=face.sqrMagnitude>.1f?face.normalized:Vector2.left;
   transform.position=center+normal*.035f;
   transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(normal.y,normal.x)*Mathf.Rad2Deg);
  }
  public void SetSlot(int slot) {
   Slot=slot;
   if(glow!=null) { glow.enabled=slot!=0; glow.color=Color.white; }
   if(core!=null) { core.enabled=!pixelArt && slot!=0; if(!pixelArt) core.color=slot==0?new Color(.21f,.27f,.36f):new Color(.1f,.02f,.22f); }
   if(caption!=null) caption.text=slot==0?"SURFACE":slot==1?"PORTAL VIOLET":"PORTAL CYAN";
   var trigger=GetComponent<Collider2D>(); if(trigger!=null) trigger.enabled=slot!=0;
  }
  void Update() {
   if(Slot==0 || glow==null) return;
   if(pixelArt) { var frames=Slot==1?purpleFrames:greenFrames; if(frames!=null&&frames.Length>0) glow.sprite=frames[(int)(Time.time*6)%frames.Length]; return; }
   var c=glow.color; c.a=.8f+Mathf.Sin(Time.time*4)*.2f; glow.color=c;
  }
  void OnTriggerStay2D(Collider2D other) { if(level!=null&&other.GetComponent<Prisoner>()!=null) level.EnterPortal(this); }
 }
}
