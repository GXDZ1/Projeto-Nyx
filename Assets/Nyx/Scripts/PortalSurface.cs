using UnityEngine;
namespace Nyx {
 public class PortalSurface : MonoBehaviour {
  public BoxCollider2D collider2D;
  public BoxCollider2D solidCollider;
  public BoxCollider2D[] portalFrameColliders;
  public Vector2 fixedNormal = Vector2.left;
  public bool snapToEdge;
  public PortalPad placedPortal;
  public PortalPad violetPortal, greenPortal;
  public Sprite[] purpleFrames, greenFrames;
  public bool enabledSurface = true;
  public PortalPad GetPortal(int slot) => slot==1?violetPortal:greenPortal;
  public void SetPortal(int slot,PortalPad pad) { if(slot==1) violetPortal=pad; else greenPortal=pad; placedPortal=pad; }
  public void ForgetPortal(PortalPad pad) { if(violetPortal==pad) violetPortal=null; if(greenPortal==pad) greenPortal=null; if(placedPortal==pad) placedPortal=violetPortal!=null?violetPortal:greenPortal; if(pad!=null&&pad.surfaceOwner==this) pad.surfaceOwner=null; RefreshPhysics(); }
  public void RefreshPhysics() {
   var activePortal=violetPortal!=null&&violetPortal.Slot!=0?violetPortal:greenPortal!=null&&greenPortal.Slot!=0?greenPortal:null;
   var level=activePortal!=null?activePortal.level:null;
   bool open=activePortal!=null&&level!=null&&level.FirstPortal!=null&&level.SecondPortal!=null;
   if(solidCollider!=null) solidCollider.enabled=!open;
   if(portalFrameColliders!=null) foreach(var frame in portalFrameColliders) if(frame!=null) frame.enabled=open;
  }
  public void Resolve(Vector2 click,Vector2 shooter,Vector2 hitNormal,out Vector2 point,out Vector2 normal) {
   Resolve(click,shooter,out point,out normal);
   if(hitNormal.sqrMagnitude>.1f) normal=hitNormal.normalized;
   if(collider2D!=null) {
    var b=collider2D.bounds;
    if(Mathf.Abs(normal.x)>.5f) point.x=normal.x<0?b.min.x:b.max.x;
    else point.y=normal.y<0?b.min.y:b.max.y;
   }
  }
  public void Resolve(Vector2 click, Vector2 shooter, out Vector2 point, out Vector2 normal) {
   normal=fixedNormal.sqrMagnitude>.1f?fixedNormal.normalized:Vector2.left;
   point=click;
   if(collider2D==null) return;
   var b=collider2D.bounds;
   if(!snapToEdge) {
    if(Mathf.Abs(normal.x)>.5f) normal=shooter.x<b.center.x?Vector2.left:Vector2.right;
    else if(Mathf.Abs(normal.y)>.5f) normal=shooter.y<b.center.y?Vector2.down:Vector2.up;
    if(Mathf.Abs(normal.x)>.5f) { point.x=normal.x<0?b.min.x:b.max.x; point.y=Mathf.Clamp(click.y,b.min.y+1.03f,b.max.y-1.03f); }
    else { point.y=normal.y<0?b.min.y:b.max.y; point.x=Mathf.Clamp(click.x,b.min.x+1.03f,b.max.x-1.03f); }
    return;
   }
   float left=Mathf.Abs(click.x-b.min.x), right=Mathf.Abs(b.max.x-click.x);
   float bottom=Mathf.Abs(click.y-b.min.y), top=Mathf.Abs(b.max.y-click.y);
   float d=Mathf.Min(Mathf.Min(left,right),Mathf.Min(bottom,top));
   const float tie=.02f;
   if(left<=d+tie&&shooter.x<b.center.x) { normal=Vector2.left; point.x=b.min.x; }
   else if(right<=d+tie&&shooter.x>=b.center.x) { normal=Vector2.right; point.x=b.max.x; }
   else if(bottom<=d+tie&&shooter.y<b.center.y) { normal=Vector2.down; point.y=b.min.y; }
   else if(top<=d+tie&&shooter.y>=b.center.y) { normal=Vector2.up; point.y=b.max.y; }
   else if(d==left) { normal=Vector2.left; point.x=b.min.x; }
   else if(d==right) { normal=Vector2.right; point.x=b.max.x; }
   else if(d==bottom) { normal=Vector2.down; point.y=b.min.y; }
   else { normal=Vector2.up; point.y=b.max.y; }
  }
 }
}
