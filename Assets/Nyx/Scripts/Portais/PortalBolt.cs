using UnityEngine;

namespace Nyx {
 public class PortalBolt : MonoBehaviour {
  MaintenanceLevel level;
  PortalSurface surface;
  Vector2 localImpact, localPortalPoint, localNormal;
  int slot;
  const float TravelSpeed=12f;

  public void Launch(MaintenanceLevel owner,PortalSurface target,Vector2 start,Vector2 impact,Vector2 portalPoint,Vector2 face,int portalSlot) {
   level=owner; surface=target; slot=portalSlot;
   transform.position=start;
   Vector2 direction=impact-start;
   if(direction.sqrMagnitude>.001f) transform.right=direction.normalized;
   localImpact=surface.transform.InverseTransformPoint(impact);
   localPortalPoint=surface.transform.InverseTransformPoint(portalPoint);
   localNormal=surface.transform.InverseTransformDirection(face);
  }

  void Update() {
   if(surface==null||level==null) {
    Destroy(gameObject);
    return;
   }
   Vector2 impact=surface.transform.TransformPoint(localImpact);
   Vector2 next=Vector2.MoveTowards(transform.position,impact,TravelSpeed*Time.deltaTime);
   if(next!= (Vector2)transform.position) transform.right=(next-(Vector2)transform.position).normalized;
   transform.position=next;
   if(Vector2.SqrMagnitude(impact-next)>.0004f) return;
   Vector2 portalPoint=surface.transform.TransformPoint(localPortalPoint);
   Vector2 normal=surface.transform.TransformDirection(localNormal).normalized;
   level.PlacePortal(surface,slot,portalPoint,normal);
   Destroy(gameObject);
  }
 }
}
