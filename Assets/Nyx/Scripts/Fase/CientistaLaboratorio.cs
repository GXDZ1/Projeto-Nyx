using UnityEngine;

namespace Nyx
{
    public class CientistaLaboratorio : MonoBehaviour
    {
        public MaintenanceLevel level;
        public SpriteRenderer spriteRenderer;
        public Sprite[] idleFrames, startledFrames, walkFrames;
        public bool followPlayer;
        public float moveRange = 2f;
        public float reactionRange = 3.7f;

        Vector3 home;
        float startledUntil;
        bool watching;

        void Awake()
        {
            home = transform.position;
        }

        void Update()
        {
            var player = level != null ? level.player : null;
            if (player == null || spriteRenderer == null)
                return;

            Vector3 position = transform.position;
            Vector3 difference = player.transform.position - position;
            bool nearby = player.body.simulated && Mathf.Abs(difference.y) < 1.8f;
            if (!watching && nearby && Mathf.Abs(difference.x) <= reactionRange)
            {
                watching = true;
                startledUntil = Time.time + .65f;
            }
            else if (watching && (!nearby || Mathf.Abs(difference.x) > reactionRange + 1.2f))
            {
                watching = false;
            }

            if (watching && Mathf.Abs(difference.x) > .15f)
                spriteRenderer.flipX = difference.x < 0f;

            if (Time.time < startledUntil)
            {
                SetFrame(startledFrames, 8f);
                return;
            }

            float destination = home.x;
            if (followPlayer && watching)
            {
                float gap = Mathf.Abs(difference.x);
                destination = gap > 1.3f ? player.transform.position.x - Mathf.Sign(difference.x) * 1.3f : position.x;
                destination = Mathf.Clamp(destination, home.x - moveRange, home.x + moveRange);
            }

            if (Mathf.Abs(destination - position.x) > .04f)
            {
                if (!watching)
                    spriteRenderer.flipX = destination < position.x;
                position.x = Mathf.MoveTowards(position.x, destination, 2.1f * Time.deltaTime);
                transform.position = position;
                SetFrame(walkFrames, 8f);
            }
            else
            {
                SetFrame(idleFrames, 2f);
            }
        }

        void SetFrame(Sprite[] frames, float framesPerSecond)
        {
            if (frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[(int)(Time.time * framesPerSecond) % frames.Length];
        }
    }
}
