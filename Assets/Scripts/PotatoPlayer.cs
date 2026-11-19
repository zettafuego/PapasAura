using System.Collections.Generic;
using UnityEngine;

namespace PapasAura
{
    public class PotatoPlayer : MonoBehaviour
    {
        public string Variety;
        public string ControlsHint;
        public Color Accent;
        public int Aura;
        public bool Alive = true;
        public bool Grounded { get; private set; }
        public float HalfW = 0.55f;
        public float Height = 1.9f;
        public float HeadY => transform.position.y + Height * 0.92f;
        public float X => transform.position.x;

        public KeyCode LeftKey;
        public KeyCode RightKey;
        public KeyCode JumpKey;
        public float MoveSpeed = 7f;
        public float JumpForce = 12.4f;
        public float Gravity = 27f;
        public float OrbMult = 1f;
        public float AirMult = 1f;
        public float Magnet = 0f;

        Sprite idle;
        Sprite jump;
        SpriteRenderer sr;
        PapasGame game;
        Vector2 vel;
        int jumpsLeft = 2;
        float coyote;
        float airClock;
        float lastHuman = -99f;
        float squash = 1f;
        bool facingRight = true;
        WorldPlatform lastPlat;
        float clutchArmed;
        float aiJumpCd;

        public void Setup(string variety, string hint, Color accent,
            Sprite idleSpr, Sprite jumpSpr, Vector3 pos,
            KeyCode left, KeyCode right, KeyCode jumpKey)
        {
            Variety = variety;
            ControlsHint = hint;
            Accent = accent;
            idle = idleSpr;
            jump = jumpSpr;
            LeftKey = left;
            RightKey = right;
            JumpKey = jumpKey;
            transform.position = pos;
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = idle;
            sr.sortingOrder = 10;
            game = PapasGame.I;
            Alive = true;
            Aura = 0;
            vel = Vector2.zero;
        }

        public void Tick(float dt, List<WorldPlatform> platforms, List<PotatoPlayer> others, float panY)
        {
            if (!Alive) return;

            float h;
            bool jumpPressed;
            ReadInput(out h, out jumpPressed, platforms, panY);

            if (Mathf.Abs(h) > 0.15f)
                facingRight = h > 0f;

            vel.x = h * MoveSpeed;

            if (Grounded)
            {
                jumpsLeft = 2;
                coyote = 0.12f;
                airClock = 0f;
            }
            else
            {
                coyote -= dt;
                vel.y -= Gravity * dt;
                airClock += dt;
                if (airClock >= 0.5f)
                {
                    airClock = 0f;
                    int gain = Mathf.Max(1, Mathf.RoundToInt(AirMult));
                    game.AddAura(this, gain, "aire", false);
                }
            }

            if (jumpPressed && (Grounded || coyote > 0f || jumpsLeft > 0))
            {
                if (!(Grounded || coyote > 0f))
                    jumpsLeft--;
                else
                    jumpsLeft = 1;

                vel.y = JumpForce;
                Grounded = false;
                coyote = 0f;
                squash = 1.18f;
                game.PlayJump();

                if (clutchArmed > 0f)
                {
                    game.AddAura(this, 22, "CLUTCH", true);
                    clutchArmed = 0f;
                }
            }

            Vector3 p = transform.position;
            float prevY = p.y;
            p.x = Mathf.Clamp(p.x + vel.x * dt, -9.3f, 9.3f);
            p.y += vel.y * dt;

            Grounded = false;
            lastPlat = null;
            if (vel.y <= 0.05f)
            {
                for (int i = 0; i < platforms.Count; i++)
                {
                    var plat = platforms[i];
                    if (plat == null || !plat.Solid) continue;
                    if (p.x + HalfW < plat.Left || p.x - HalfW > plat.Right) continue;
                    if (prevY >= plat.Top - 0.18f && p.y <= plat.Top)
                    {
                        p.y = plat.Top;
                        vel.y = 0f;
                        Grounded = true;
                        lastPlat = plat;
                        plat.OnLanded();
                        squash = 0.78f;
                        break;
                    }
                }

                if (!Grounded)
                {
                    for (int i = 0; i < others.Count; i++)
                    {
                        var o = others[i];
                        if (o == this || o == null || !o.Alive) continue;
                        if (Mathf.Abs(p.x - o.X) > HalfW + o.HalfW) continue;
                        float head = o.HeadY;
                        if (prevY >= head - 0.2f && p.y <= head)
                        {
                            p.y = head;
                            vel.y = JumpForce * 0.88f;
                            squash = 1.12f;
                            game.AddAura(this, 15, "headbounce", true);
                            break;
                        }
                    }
                }
            }

            transform.position = p;
            squash = Mathf.MoveTowards(squash, 1f, dt * 4f);
            float sx = facingRight ? 1f : -1f;
            transform.localScale = new Vector3(sx * Mathf.Lerp(1.2f, 1f, squash), squash, 1f);
            if (sr != null)
                sr.sprite = Grounded ? idle : jump;

            if (p.y < panY + 2.4f)
                clutchArmed = 0.35f;
            else
                clutchArmed -= dt;

            if (p.y < panY + 0.55f)
                Die();
        }

        void ReadInput(out float h, out bool jumpPressed, List<WorldPlatform> platforms, float panY)
        {
            float humanH = 0f;
            if (Input.GetKey(LeftKey)) humanH -= 1f;
            if (Input.GetKey(RightKey)) humanH += 1f;
            bool humanJump = Input.GetKeyDown(JumpKey);

            if (Mathf.Abs(humanH) > 0.01f || humanJump)
                lastHuman = Time.time;

            bool useAi = Time.time - lastHuman > 1.6f;
            if (!useAi)
            {
                h = humanH;
                jumpPressed = humanJump;
                return;
            }

            Vector3 p = transform.position;
            Vector3? orb = game.NearestOrb(p);
            float targetX = orb.HasValue ? orb.Value.x : 0f;

            if (Grounded && lastPlat != null)
            {
                if (p.x < lastPlat.Left + 0.55f) targetX = lastPlat.Left + 1.2f;
                else if (p.x > lastPlat.Right - 0.55f) targetX = lastPlat.Right - 1.2f;
            }

            h = 0f;
            if (Mathf.Abs(targetX - p.x) > 0.25f)
                h = Mathf.Sign(targetX - p.x);

            bool nearEdge = Grounded && lastPlat != null &&
                            (p.x < lastPlat.Left + 0.7f || p.x > lastPlat.Right - 0.7f);
            bool panClose = panY + 4.2f > p.y;
            bool orbAbove = orb.HasValue && orb.Value.y > p.y + 0.7f;
            aiJumpCd -= Time.deltaTime;
            jumpPressed = aiJumpCd <= 0f && (Grounded || jumpsLeft > 0) && (nearEdge || panClose || orbAbove);
            if (jumpPressed) aiJumpCd = 0.38f;
        }

        public void Die()
        {
            if (!Alive) return;
            Alive = false;
            vel = Vector2.zero;
            if (sr != null)
                sr.color = new Color(0.35f, 0.22f, 0.12f, 0.7f);
            game.OnPotatoCooked(this);
        }

        public void Flash()
        {
            if (sr == null) return;
            sr.color = Color.white;
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.08f);
        }

        void RestoreColor()
        {
            if (sr != null && Alive)
                sr.color = Color.white;
        }
    }
}
