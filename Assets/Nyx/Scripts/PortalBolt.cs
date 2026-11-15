using UnityEngine;

namespace Nyx {
 public class PortalBolt : MonoBehaviour {
  MaintenanceLevel level;
  PortalSurface surface;
  PortalPad fixedPad;
  Vector2 destination, normal, localPoint, localNormal;
  int slot;
  const float TravelSpeed=12f;
  bool placed;

  public void Launch(MaintenanceLevel owner,PortalSurface target,PortalPad pad,Vector2 point,Vector2 face,int portalSlot,Vector2 start) {
   level=owner; surface=target; fixedPad=pad; destination=point; normal=face; slot=portalSlot;
   transform.position=start;
   Vector2 direction=destination-start;
   if(direction.sqrMagnitude>.001f) transform.right=direction.normalized;
   if(surface!=null) {
    localPoint=surface.transform.InverseTransformPoint(destination);
    localNormal=surface.transform.InverseTransformDirection(normal);
   }
  }

  void Update() {
   if(placed) return;
   if(surface!=null) {
    destination=surface.transform.TransformPoint(localPoint);
    normal=surface.transform.TransformDirection(localNormal).normalized;
   }
   Vector2 next=Vector2.MoveTowards(transform.position,destination,TravelSpeed*Time.deltaTime);
   if(next!= (Vector2)transform.position) transform.right=(next-(Vector2)transform.position).normalized;
   transform.position=next;
   if(Vector2.SqrMagnitude(destination-next)>.0004f) return;
   placed=true;
   if(surface!=null) level.PlacePortal(surface,slot,destination,normal);
   else if(fixedPad!=null) level.PlacePortal(fixedPad,slot);
   Destroy(gameObject);
  }
 }
}
