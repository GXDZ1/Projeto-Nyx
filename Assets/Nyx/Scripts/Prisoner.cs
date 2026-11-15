using UnityEngine;
using UnityEngine.InputSystem;

namespace Nyx {
 public class Prisoner : MonoBehaviour {
  public MaintenanceLevel level;
  public Transform visual, weapon, arm;
  public SpriteRenderer sprite;
  public Rigidbody2D body;
  public Collider2D bodyCollider;
  public LayerMask groundMask = (1 << 0) | (1 << 8);
  public float moveSpeed=5.5f;
  public float jumpSpeed=10.8f;
  public float jumpBufferDuration=.16f;
  public float coyoteDuration=.14f;
  public bool Grounded {get; private set;}
  public bool WalkingBackward {get; private set;}
  public int Facing {get; private set;}=1;
  public Vector2 AimDirection {get; private set;}=Vector2.right;
  public int AimFacing {get; private set;}=1;
  public Vector2 BodyCenter => bodyCollider!=null?bodyCollider.bounds.center:body.position+Vector2.up*.9f;
  public Vector2 BodyCenterOffset => BodyCenter-body.position;
  float input, jumpBuffer, coyoteTime;
  bool jumpHeld;

  void Awake() {
   if(body==null) body=GetComponent<Rigidbody2D>();
   if(bodyCollider==null) bodyCollider=GetComponent<Collider2D>();
   if(body!=null) {
    body.bodyType=RigidbodyType2D.Dynamic;
    body.freezeRotation=true;
    body.interpolation=RigidbodyInterpolation2D.Interpolate;
    body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
    body.WakeUp();
   }
   if(groundMask.value==0) groundMask=(1<<0)|(1<<8);
  }

  void Update() {
   if(level!=null&&level.completed) return;
   var keys=Keyboard.current;
   input=keys==null?0:((keys.dKey.isPressed||keys.rightArrowKey.isPressed?1:0)-(keys.aKey.isPressed||keys.leftArrowKey.isPressed?1:0));
   if(keys!=null) {
    if(keys.spaceKey.wasPressedThisFrame) jumpBuffer=jumpBufferDuration;
    jumpHeld=keys.spaceKey.isPressed;
   } else jumpHeld=false;

   var mouse=Mouse.current;
   if(mouse!=null&&level!=null&&level.gameCamera!=null) {
    Vector2 pointer=level.gameCamera.ScreenToWorldPoint(mouse.position.ReadValue());
    AimAt(pointer);
   } else if(Mathf.Abs(input)>.01f) AimFacing=input>0?1:-1;
   Facing=AimFacing;
   WalkingBackward=Mathf.Abs(input)>.01f&&input*Facing<-.01f;
   if(sprite!=null) sprite.flipX=Facing<0;
   UpdateWeapon();
  }

  void FixedUpdate() {
   if(body==null||(level!=null&&level.completed)) return;
   Grounded=CheckGrounded();
   if(Grounded) coyoteTime=coyoteDuration;
   else coyoteTime=Mathf.Max(0,coyoteTime-Time.fixedDeltaTime);

   Vector2 velocity=body.linearVelocity;
   if(jumpBuffer>0&&coyoteTime>0) {
    velocity.y=jumpSpeed;
    Grounded=false;
    coyoteTime=0;
    jumpBuffer=0;
   } else jumpBuffer=Mathf.Max(0,jumpBuffer-Time.fixedDeltaTime);

   float acceleration=Grounded?(Mathf.Abs(input)>.01f?38f:46f):24f;
   velocity.x=Mathf.MoveTowards(velocity.x,input*moveSpeed,acceleration*Time.fixedDeltaTime);
   if(Grounded&&velocity.y<0) velocity.y=-1.1f;
   body.linearVelocity=new Vector2(velocity.x,Mathf.Max(velocity.y,-22f));
   body.gravityScale=Grounded?2f:velocity.y>.15f?(jumpHeld?1.6f:3.7f):3.1f;
   if(Grounded&&Mathf.Abs(input)>.01f) PushCrateAhead();
  }

  void PushCrateAhead() {
   if(bodyCollider==null) return;
   Bounds bounds=bodyCollider.bounds;
   Vector2 center=new Vector2(bounds.center.x+Mathf.Sign(input)*(bounds.extents.x+.08f),bounds.center.y);
   var hits=Physics2D.OverlapBoxAll(center,new Vector2(.22f,bounds.size.y*.62f),0,1<<8);
   foreach(var hit in hits) {
    if(hit==null) continue;
    var crate=hit.GetComponentInParent<NyxCrate>();
    if(crate!=null) { crate.Push(input); return; }
   }
  }

  bool CheckGrounded() {
   if(bodyCollider==null) return false;
   if(bodyCollider.IsTouchingLayers(groundMask)) return true;
   Bounds bounds=bodyCollider.bounds;
   Vector2 size=new Vector2(Mathf.Max(.12f,bounds.size.x*.72f),.16f);
   Vector2 center=new Vector2(bounds.center.x,bounds.min.y-.025f);
   foreach(var hit in Physics2D.OverlapBoxAll(center,size,0,groundMask)) {
    if(hit==null||hit==bodyCollider||hit.isTrigger||hit.attachedRigidbody==body) continue;
    return true;
   }
   return false;
  }

  void UpdateWeapon() {
   float angle=Mathf.Atan2(AimDirection.y,AimDirection.x)*Mathf.Rad2Deg;
   if(arm!=null) {
    Vector2 shoulder=new Vector2(AimFacing*.2f,.72f);
    arm.localPosition=shoulder+AimDirection.normalized*.15f;
    arm.localRotation=Quaternion.Euler(0,0,angle);
   }
   if(weapon==null) return;
   weapon.localPosition=new Vector3(AimFacing*.42f,.75f+AimDirection.y*.08f,0);
   weapon.localScale=new Vector3(.5f,.22f,1);
   weapon.localRotation=Quaternion.Euler(0,0,angle);
  }

  public Vector2 WeaponTip => weapon!=null?(Vector2)weapon.position+(Vector2)weapon.right*.34f:(Vector2)transform.position+new Vector2(Facing*.42f,.95f);

  public void AimAt(Vector2 point) {
   Vector2 shoulder=(Vector2)transform.position+new Vector2(AimFacing*.42f,.75f);
   Vector2 direction=point-shoulder;
   if(direction.sqrMagnitude>.001f) {
    AimDirection=direction.normalized;
    if(Mathf.Abs(direction.x)>.3f) AimFacing=direction.x>0?1:-1;
   }
  }

  public void Teleport(Vector2 point,Vector2 velocity) {
   if(body==null) return;
   body.position=point;
   transform.position=new Vector3(point.x,point.y,transform.position.z);
   body.linearVelocity=velocity;
   body.WakeUp();
   jumpBuffer=0; coyoteTime=0; Grounded=false;
   if(Mathf.Abs(velocity.x)>.08f) Facing=velocity.x>0?1:-1;
  }

  public void Teleport(Vector2 point) { Teleport(point,Vector2.zero); }

  public void Stop() { input=0; if(body!=null) body.linearVelocity=Vector2.zero; }
 }
}
