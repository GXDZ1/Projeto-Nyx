using UnityEngine;

namespace Nyx
{
    /// <summary>
    /// Troca os desenhos de um SpriteRenderer em sequência.
    /// </summary>
    public class SpriteLoop : MonoBehaviour
    {
        public SpriteRenderer target;
        public Sprite[] frames;
        public float fps = 6;

        void Update()
        {
            if (target == null || frames == null || frames.Length == 0)
                return;

            int frameIndex = (int)(Time.time * fps) % frames.Length;
            target.sprite = frames[frameIndex];
        }
    }
}
