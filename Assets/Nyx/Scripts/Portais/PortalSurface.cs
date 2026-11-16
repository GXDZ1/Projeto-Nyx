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
  public Vector2 PlacementNormal => fixedNormal.sqrMagnitude>.1f?fixedNormal.normalized:Vector2.left;
  public PortalPad GetPortal(int slot) => slot==1?violetPortal:greenPortal;
  public void SetPortal(int slot,PortalPad pad) { if(slot==1) violetPortal=pad; else greenPortal=pad; placedPortal=pad; }
  public void ForgetPortal(PortalPad pad) { if(violetPortal==pad) violetPortal=null; if(greenPortal==pad) greenPortal=null; if(placedPortal==pad) placedPortal=violetPortal!=null?violetPortal:greenPortal; if(pad!=null&&pad.surfaceOwner==this) pad.surfaceOwner=null; RefreshPhysics(); }
  public void RefreshPhysics() {
   // A passagem acontece pelo gatilho do portal. O suporte continua sólido para o mundo todo.
   if(solidCollider!=null) solidCollider.enabled=true;
   if(portalFrameColliders!=null)
    foreach(var frame in portalFrameColliders)
     if(frame!=null) frame.enabled=false;
  }
  public void Resolve(Vector2 impact,Vector2 hitNormal,out Vector2 point,out Vector2 normal) {
   normal=PlacementNormal;
   point=impact;
   if(snapToEdge&&hitNormal.sqrMagnitude>.1f) {
    normal=hitNormal.normalized;
    return;
   }

   // Use the opening's dimensions even when its solid collider is disabled.
   var mount=solidCollider!=null?solidCollider:collider2D;
   if(mount==null) return;
   Vector2 center=mount.transform.TransformPoint(mount.offset);
   Vector2 halfX=mount.transform.TransformVector(Vector2.right*(mount.size.x*.5f));
   Vector2 halfY=mount.transform.TransformVector(Vector2.up*(mount.size.y*.5f));
   var tangent=new Vector2(-normal.y,normal.x);
   float depth=Mathf.Abs(Vector2.Dot(halfX,normal))+Mathf.Abs(Vector2.Dot(halfY,normal));
   float halfWidth=Mathf.Abs(Vector2.Dot(halfX,tangent))+Mathf.Abs(Vector2.Dot(halfY,tangent));
   const float PortalHalfWidth=1.03f;
   float room=Mathf.Max(0,halfWidth-PortalHalfWidth);
   float offset=Mathf.Clamp(Vector2.Dot(impact-center,tangent),-room,room);
   point=center+normal*depth+tangent*offset;
  }
 }
}
