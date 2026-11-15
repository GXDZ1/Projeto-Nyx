using UnityEngine;

namespace Nyx {
 public class NyxFireHazard : MonoBehaviour {
  static Sprite[] flameFrames;
  public SpriteRenderer fireRenderer;
  public MaintenanceLevel level;
  public Sprite[] frames;
  int frameOffset;

  void Awake() {
   if(fireRenderer==null) fireRenderer=GetComponent<SpriteRenderer>();
   if(fireRenderer==null) return;
   if(frames==null||frames.Length==0) {
    if(flameFrames==null) flameFrames=CreateFrames();
    frames=flameFrames;
   }
   if(fireRenderer!=null&&frames.Length>0) fireRenderer.sprite=frames[0];
   frameOffset=Mathf.Abs(Mathf.RoundToInt(transform.position.x*7));
  }

  void Update() {
   if(fireRenderer!=null&&frames!=null&&frames.Length>0)
    fireRenderer.sprite=frames[(Mathf.FloorToInt(Time.time*10)+frameOffset)%frames.Length];
  }

  void OnTriggerEnter2D(Collider2D other) {
   if(level!=null&&other.GetComponent<Prisoner>()!=null) level.Respawn();
  }

  static Sprite[] CreateFrames() {
   const int width=32,height=40;
   var frames=new Sprite[6];
   for(int frame=0;frame<frames.Length;frame++) {
    var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
    texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;
    texture.SetPixels32(CreateFirePixels(frame,width,height)); texture.Apply(false,true);
    frames[frame]=Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,0),24,0,SpriteMeshType.FullRect);
    frames[frame].name="Nyx fire frame "+frame;
   }
   return frames;
  }

  public static Color32[] CreateFirePixels(int frame,int width,int height) {
   float[] tongues={3.5f,11.5f,19.5f,27.5f};
   var pixels=new Color32[width*height];
   for(int y=0;y<height;y++) for(int x=0;x<width;x++) {
    bool inTongue=false;
    float edge=1f, heightRatio=0f;
    for(int i=0;i<tongues.Length;i++) {
     float phase=frame*.92f+i*1.57f;
     float tipHeight=29f+3.4f*Mathf.Sin(phase);
     if(y>tipHeight) continue;
     float progress=y/tipHeight;
     float center=tongues[i]+Mathf.Sin(y*.31f+phase)*.85f;
     float radius=(5.1f*Mathf.Pow(1-progress,.72f)+.38f)*(.86f+.14f*Mathf.Sin(y*.72f+phase));
     float normalizedEdge=Mathf.Abs(x-center)/radius;
     if(normalizedEdge<=1f&&normalizedEdge<edge) {
      inTongue=true; edge=normalizedEdge; heightRatio=progress;
     }
    }
    int baseTop=8+Mathf.RoundToInt(1.3f*Mathf.Sin(x*.57f+frame*.82f));
    bool inBase=y<=2||(y<=baseTop&&x>=1&&x<width-1);
    if(!inTongue&&!inBase) continue;

    Color32 color;
    if(y<=2) color=(x<4||x>width-5)?new Color32(130,32,35,255):new Color32(221,59,27,255);
    else if(inTongue) {
     if(edge>.84f) color=new Color32(196,42,28,255);
     else if(edge>.62f) color=new Color32(246,75,22,255);
     else if(edge>.37f) color=new Color32(255,145,25,255);
     else if(heightRatio<.68f) color=new Color32(255,239,123,255);
     else color=new Color32(255,190,48,255);
    } else {
     int edgePixels=Mathf.Min(x-1,width-2-x);
     color=edgePixels<2?new Color32(235,65,24,255):y<6?new Color32(255,105,24,255):new Color32(255,163,30,255);
    }
    pixels[y*width+x]=color;
   }
   for(int ember=0;ember<4;ember++) {
    int x=(frame*7+ember*9+3)%width;
    int y=Mathf.Min(height-1,30+(frame*3+ember*5)%10);
    pixels[y*width+x]=ember%2==0?new Color32(255,231,111,255):new Color32(255,126,32,255);
   }
   return pixels;
  }
 }
}
