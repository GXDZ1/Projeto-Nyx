using UnityEngine; namespace Nyx { public class SpriteLoop : MonoBehaviour {
  public SpriteRenderer target;
  public Sprite[] frames;
  public float fps=6;
  void Update() { if(frames!=null && frames.Length>0) target.sprite=frames[(int)(Time.time*fps)%frames.Length]; }
 }
}