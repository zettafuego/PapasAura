using UnityEngine;

namespace PapasAura
{
    public class WorldPlatform : MonoBehaviour
    {
        public float Left { get; private set; }
        public float Right { get; private set; }
        public float Top { get; private set; }
        public bool Solid { get; private set; } = true;
        public bool Crumbles { get; private set; }

        float crumbleTimer = -1f;
        float fallVel;
        SpriteRenderer sr;

        public void Setup(float x, float y, float width, bool crumbles)
        {
            sr = GetComponent<SpriteRenderer>();
            Crumbles = crumbles;
            Solid = true;
            transform.position = new Vector3(x, y, 0f);
            float scaleX = Mathf.Max(0.4f, width / 4.8f);
            transform.localScale = new Vector3(scaleX, 0.85f, 1f);
            Left = x - width * 0.5f;
            Right = x + width * 0.5f;
            Top = y;
            if (sr != null && crumbles)
                sr.color = new Color(0.72f, 0.62f, 0.5f, 1f);
        }

        public void OnLanded()
        {
            if (Crumbles && crumbleTimer < 0f)
                crumbleTimer = 0.7f;
        }

        void Update()
        {
            if (crumbleTimer >= 0f)
            {
                crumbleTimer -= Time.deltaTime;
                if (sr != null)
                    sr.color = Color.Lerp(new Color(0.45f, 0.25f, 0.15f), sr.color, 0.4f);
                transform.position += Vector3.down * 0.04f * Mathf.Sin(Time.time * 40f);
                if (crumbleTimer <= 0f)
                {
                    Solid = false;
                    crumbleTimer = -2f;
                    fallVel = 0f;
                }
            }

            if (!Solid)
            {
                fallVel += 28f * Time.deltaTime;
                transform.position += Vector3.down * fallVel * Time.deltaTime;
                if (sr != null)
                {
                    var c = sr.color;
                    c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 0.8f);
                    sr.color = c;
                }
                if (transform.position.y < -40f)
                    Destroy(gameObject);
            }
        }
    }
}
