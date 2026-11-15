using UnityEngine;
namespace Nyx {
 public class NyxCrate : MonoBehaviour {
  static PhysicsMaterial2D crateMaterial;
  void Awake() {
   var body=GetComponent<Rigidbody2D>(); if(body==null) body=gameObject.AddComponent<Rigidbody2D>();
   body.bodyType=RigidbodyType2D.Dynamic; body.mass=1.4f; body.gravityScale=2f;
   body.constraints=RigidbodyConstraints2D.FreezeRotation;
   body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
   body.interpolation=RigidbodyInterpolation2D.Interpolate;
   body.linearDamping=2.6f; body.angularDamping=1f;
   if(crateMaterial==null) crateMaterial=new PhysicsMaterial2D("Nyx crate friction") { friction=.72f, bounciness=0f };
   foreach(var box in GetComponents<BoxCollider2D>()) { box.isTrigger=false; box.edgeRadius=.02f; box.sharedMaterial=crateMaterial; }
   gameObject.layer=8;
  }

  void FixedUpdate() {
   var body=GetComponent<Rigidbody2D>(); if(body==null) return;
   var velocity=body.linearVelocity;
   velocity.x=Mathf.Clamp(velocity.x,-3.5f,3.5f);
   if(Mathf.Abs(velocity.x)<.035f) velocity.x=0;
   body.linearVelocity=velocity;
  }

  public void Push(float direction) {
   var body=GetComponent<Rigidbody2D>(); if(body==null||Mathf.Abs(direction)<.01f) return;
   body.WakeUp();
   body.AddForce(Vector2.right*Mathf.Sign(direction)*24f,ForceMode2D.Force);
  }
 }
}
