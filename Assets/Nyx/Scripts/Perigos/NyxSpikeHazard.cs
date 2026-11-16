using UnityEngine;

namespace Nyx {
 public class NyxSpikeHazard : MonoBehaviour {
  public MaintenanceLevel level;
  void OnTriggerEnter2D(Collider2D other) {
   if(level!=null&&other.GetComponent<Prisoner>()!=null) level.Respawn();
  }
 }
}
