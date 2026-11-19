using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PapasAura
{
    public class PapasGame : MonoBehaviour
    {
        public const int AuraToWin = 300;
        public static PapasGame I { get; private set; }

        public bool Playing { get; private set; }
        public bool Ended { get; private set; }
        public int TeamAura { get; private set; }

        readonly List<PotatoPlayer> potatoes = new List<PotatoPlayer>();
        readonly List<WorldPlatform> platforms = new List<WorldPlatform>();
        readonly List<AuraOrb> orbs = new List<AuraOrb>();

        FryingPan pan;
        Camera cam;
        Transform worldRoot;
        SpriteRenderer bg;
        AudioSource sfx;

        GameObject startPanel;
        GameObject endPanel;
        GameObject hudRoot;
        Text auraText;
        Text potatoText;
        Text endTitle;
        Text endBody;

        AudioClip jumpClip;
        AudioClip orbClip;
        AudioClip clutchClip;
        AudioClip cookClip;
        AudioClip winClip;
        AudioClip loseClip;

        float nextPlatformY;
        float lastSpawnX;
        Font uiFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<PapasGame>() != null) return;
            var go = new GameObject("PapasAura");
            go.AddComponent<PapasGame>();
        }

        void Awake()
        {
            I = this;
            EnsureCamera();
            EnsureEventSystem();
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            BuildClips();
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            worldRoot = new GameObject("World").transform;
            BuildBackground();
            BuildUi();
            BuildMatch();
            ShowStart(true);
        }

        void EnsureCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.04f, 0.14f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 80f;
            cam.transform.position = new Vector3(0f, 3.5f, -10f);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        void BuildBackground()
        {
            var go = new GameObject("FarmSky");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 20f);
            bg = go.AddComponent<SpriteRenderer>();
            bg.sprite = SpriteKit.Background();
            bg.sortingOrder = -40;
            go.transform.localScale = Vector3.one * 1.35f;
        }

        void BuildMatch()
        {
            ClearWorld();
            TeamAura = 0;
            Playing = false;
            Ended = false;
            lastSpawnX = 0f;
            nextPlatformY = 0f;

            SpawnPlatform(0f, 0f, 10.5f, false);
            SpawnPlatform(-5.2f, 2.5f, 4.2f, false);
            SpawnPlatform(4.6f, 2.7f, 3.8f, false);
            SpawnPlatform(-1.2f, 5.1f, 3.6f, false);
            nextPlatformY = 7.4f;
            for (int i = 0; i < 10; i++)
                SpawnNextTowerPlatform();

            SpawnPotato("RUSSET", "papa blanca / russet   A D   salto W",
                new Color(0.72f, 0.48f, 0.28f),
                SpriteKit.Potato("russet_idle"), SpriteKit.Potato("russet_jump"),
                new Vector3(-2.6f, 0.05f, 0f),
                KeyCode.A, KeyCode.D, KeyCode.W, 6.4f, 13.1f, 1f, 1f, 0f);

            SpawnPotato("AMARILLA", "papa amarilla   flechas   salto ARRIBA",
                new Color(0.95f, 0.78f, 0.22f),
                SpriteKit.Potato("amarilla_idle"), SpriteKit.Potato("amarilla_jump"),
                new Vector3(0f, 0.05f, 0f),
                KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.UpArrow, 7.6f, 12.2f, 1.6f, 1f, 2.1f);

            SpawnPotato("MORADA", "papa morada   J L   salto I",
                new Color(0.62f, 0.28f, 0.78f),
                SpriteKit.Potato("morada_idle"), SpriteKit.Potato("morada_jump"),
                new Vector3(2.6f, 0.05f, 0f),
                KeyCode.J, KeyCode.L, KeyCode.I, 7.1f, 13.4f, 1f, 2.2f, 0.4f);

            var panGo = new GameObject("Sarten");
            panGo.transform.SetParent(worldRoot);
            var psr = panGo.AddComponent<SpriteRenderer>();
            psr.sprite = SpriteKit.Pan();
            psr.sortingOrder = 20;
            pan = panGo.AddComponent<FryingPan>();
            pan.Setup(-7.2f);

            SpawnOrb(new Vector3(-5.2f, 3.6f, 0f), 12);
            SpawnOrb(new Vector3(4.6f, 3.8f, 0f), 12);
            SpawnOrb(new Vector3(-1.2f, 6.3f, 0f), 16);
        }

        void SpawnPotato(string name, string hint, Color accent, Sprite idle, Sprite jump,
            Vector3 pos, KeyCode l, KeyCode r, KeyCode j,
            float speed, float jumpF, float orbM, float airM, float magnet)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldRoot);
            var p = go.AddComponent<PotatoPlayer>();
            p.MoveSpeed = speed;
            p.JumpForce = jumpF;
            p.OrbMult = orbM;
            p.AirMult = airM;
            p.Magnet = magnet;
            p.Setup(name, hint, accent, idle, jump, pos, l, r, j);
            potatoes.Add(p);
        }

        void SpawnPlatform(float x, float y, float width, bool crumbles)
        {
            var go = new GameObject("Plat");
            go.transform.SetParent(worldRoot);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteKit.Platform();
            sr.sortingOrder = 2;
            var plat = go.AddComponent<WorldPlatform>();
            plat.Setup(x, y, width, crumbles);
            platforms.Add(plat);
            lastSpawnX = x;
        }

        void SpawnNextTowerPlatform()
        {
            float width = Random.Range(2.6f, 4.6f);
            float dir = Random.value < 0.5f ? -1f : 1f;
            float x = Mathf.Clamp(lastSpawnX + dir * Random.Range(2.2f, 4.8f), -7.5f, 7.5f);
            bool crumble = Random.value < 0.28f;
            SpawnPlatform(x, nextPlatformY, width, crumble);
            if (Random.value < 0.72f)
                SpawnOrb(new Vector3(x + Random.Range(-0.6f, 0.6f), nextPlatformY + 1.15f, 0f),
                    Random.value < 0.2f ? 24 : 12);
            nextPlatformY += Random.Range(2.25f, 2.75f);
        }

        void SpawnOrb(Vector3 pos, int value)
        {
            var go = new GameObject("Aura");
            go.transform.SetParent(worldRoot);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteKit.Orb();
            sr.sortingOrder = 8;
            var orb = go.AddComponent<AuraOrb>();
            orb.Setup(pos, value);
            orbs.Add(orb);
        }

        void ClearWorld()
        {
            for (int i = worldRoot.childCount - 1; i >= 0; i--)
                Destroy(worldRoot.GetChild(i).gameObject);
            potatoes.Clear();
            platforms.Clear();
            orbs.Clear();
            pan = null;
        }

        void Update()
        {
            if (!Playing && !Ended)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    StartRun();
                return;
            }

            if (Ended)
            {
                if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
                    Restart();
                return;
            }

            float dt = Time.deltaTime;
            platforms.RemoveAll(p => p == null);
            orbs.RemoveAll(o => o == null || !o.Alive);

            float highest = 0f;
            for (int i = 0; i < potatoes.Count; i++)
            {
                var p = potatoes[i];
                if (p == null || !p.Alive) continue;
                p.Tick(dt, platforms, potatoes, pan.Y);
                if (p.transform.position.y > highest)
                    highest = p.transform.position.y;
                TryCollect(p);
                AttractOrbs(p);
            }

            while (nextPlatformY < highest + 18f)
                SpawnNextTowerPlatform();

            float camY = Mathf.Max(3.2f, highest - 1.2f, pan.Y + 6.5f);
            var cp = cam.transform.position;
            cp.y = Mathf.Lerp(cp.y, camY, 1f - Mathf.Exp(-4.5f * dt));
            cam.transform.position = cp;

            RefreshHud();

            if (TeamAura >= AuraToWin)
                EndGame(true);
            else if (AllCooked())
                EndGame(false);
        }

        void TryCollect(PotatoPlayer p)
        {
            Vector3 pos = p.transform.position + Vector3.up * 0.8f;
            for (int i = 0; i < orbs.Count; i++)
            {
                var o = orbs[i];
                if (o == null || !o.Alive) continue;
                if ((o.transform.position - pos).sqrMagnitude > 1.7f) continue;
                int gain = Mathf.RoundToInt(o.Value * p.OrbMult);
                o.Collect();
                AddAura(p, gain, "orb", true);
                PlaySfx(orbClip);
            }
        }

        void AttractOrbs(PotatoPlayer p)
        {
            if (p.Magnet <= 0.01f) return;
            Vector3 pos = p.transform.position + Vector3.up * 0.8f;
            for (int i = 0; i < orbs.Count; i++)
            {
                var o = orbs[i];
                if (o == null || !o.Alive) continue;
                Vector3 d = pos - o.transform.position;
                if (d.sqrMagnitude < p.Magnet * p.Magnet)
                    o.Nudge(d.normalized * Time.deltaTime * 3.2f);
            }
        }

        public Vector3? NearestOrb(Vector3 from)
        {
            AuraOrb best = null;
            float bestD = 999f;
            for (int i = 0; i < orbs.Count; i++)
            {
                var o = orbs[i];
                if (o == null || !o.Alive) continue;
                float d = (o.transform.position - from).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = o;
                }
            }
            return best == null ? (Vector3?)null : best.transform.position;
        }

        public void AddAura(PotatoPlayer p, int amount, string why, bool popup)
        {
            if (!Playing || Ended || p == null || !p.Alive || amount <= 0) return;
            p.Aura += amount;
            TeamAura += amount;
            p.Flash();
            if (popup)
            {
                string label = why == "CLUTCH" ? "CLUTCH +" + amount : "+" + amount + " AURA";
                SpawnPopup(p.transform.position + Vector3.up * 1.8f, label, p.Accent);
            }
            if (why == "CLUTCH")
                PlaySfx(clutchClip);
        }

        public void OnPotatoCooked(PotatoPlayer p)
        {
            PlaySfx(cookClip);
            SpawnPopup(p.transform.position + Vector3.up, p.Variety + " FRITA", new Color(1f, 0.4f, 0.15f));
        }

        public void PlayJump()
        {
            PlaySfx(jumpClip, 0.45f);
        }

        void StartRun()
        {
            Playing = true;
            ShowStart(false);
            hudRoot.SetActive(true);
            if (pan != null) pan.StartRising();
        }

        void Restart()
        {
            BuildMatch();
            ShowStart(true);
            endPanel.SetActive(false);
            hudRoot.SetActive(false);
            cam.transform.position = new Vector3(0f, 3.5f, -10f);
        }

        bool AllCooked()
        {
            for (int i = 0; i < potatoes.Count; i++)
                if (potatoes[i] != null && potatoes[i].Alive)
                    return false;
            return potatoes.Count > 0;
        }

        void EndGame(bool won)
        {
            Ended = true;
            Playing = false;
            hudRoot.SetActive(true);
            endPanel.SetActive(true);
            if (won)
            {
                endTitle.text = "AURA FARMADA";
                string breakdown = "";
                for (int i = 0; i < potatoes.Count; i++)
                    if (potatoes[i] != null)
                        breakdown += potatoes[i].Variety + " " + potatoes[i].Aura + "   ";
                endBody.text = "Las papas viven.\nSe farmearon " + TeamAura + " aura.\n\n" + breakdown +
                               "\n\n[R]  otra ronda";
                PlaySfx(winClip);
            }
            else
            {
                endTitle.text = "FRITAS";
                endBody.text = "La sarten se las comio.\nAura total: " + TeamAura + " / " + AuraToWin +
                               "\n\n[R]  reintentar farmear";
                PlaySfx(loseClip);
            }
        }

        void RefreshHud()
        {
            if (auraText != null)
                auraText.text = "AURA  " + TeamAura + "  /  " + AuraToWin;
            if (potatoText == null) return;
            var sb = "";
            for (int i = 0; i < potatoes.Count; i++)
            {
                var p = potatoes[i];
                if (p == null) continue;
                sb += p.Variety + "  " + (p.Alive ? p.Aura + " aura" : "FRITA") + "     ";
            }
            potatoText.text = sb;
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            startPanel = MakePanel(canvasGo.transform, "Start", new Color(0.05f, 0.03f, 0.08f, 0.88f));
            var startCol = MakeCol(startPanel.transform);
            MakeText(startCol, "PAPAS AURA", 78, new Color(1f, 0.86f, 0.28f), FontStyle.Bold);
            MakeText(startCol, "3 variedades de papa  ·  saltan por sus vidas", 28, Color.white, FontStyle.Normal);
            MakeText(startCol, "Ganan si farmean 300 AURA. Si caen a la sarten, se frien.", 26,
                new Color(1f, 0.78f, 0.45f), FontStyle.Italic);
            MakeText(startCol, "RUSSET     papa blanca / russet          A D     salto W\nAMARILLA   papa amarilla                     ← →     salto ↑\nMORADA     papa morada                       J L     salto I",
                24, new Color(0.9f, 0.9f, 0.85f), FontStyle.Normal);
            MakeText(startCol, "Si nadie mueve una papa, ella salta sola.\nCabecear a otra papa da aura. El aire da aura. Los orbes dorados dan aura.",
                22, new Color(0.75f, 0.72f, 0.8f), FontStyle.Normal);
            MakeText(startCol, "[ ENTER ]   a farmear aura", 36, new Color(0.55f, 1f, 0.45f), FontStyle.Bold);

            hudRoot = new GameObject("HUD");
            hudRoot.transform.SetParent(canvasGo.transform, false);
            var hudRt = hudRoot.AddComponent<RectTransform>();
            Stretch(hudRt);
            auraText = MakeHudText(hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0, -36), 42,
                new Color(1f, 0.85f, 0.25f), FontStyle.Bold);
            potatoText = MakeHudText(hudRoot.transform, new Vector2(0.5f, 1f), new Vector2(0, -78), 22,
                Color.white, FontStyle.Normal);
            hudRoot.SetActive(false);

            endPanel = MakePanel(canvasGo.transform, "End", new Color(0.04f, 0.02f, 0.06f, 0.9f));
            var endCol = MakeCol(endPanel.transform);
            endTitle = MakeText(endCol, "", 72, new Color(1f, 0.85f, 0.3f), FontStyle.Bold);
            endBody = MakeText(endCol, "", 30, Color.white, FontStyle.Normal);
            endPanel.SetActive(false);
        }

        void ShowStart(bool on)
        {
            startPanel.SetActive(on);
        }

        GameObject MakePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        Transform MakeCol(Transform parent)
        {
            var go = new GameObject("Col");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, 0.12f);
            rt.anchorMax = new Vector2(0.88f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.MiddleCenter;
            v.spacing = 18f;
            v.childControlHeight = false;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            return go.transform;
        }

        Text MakeText(Transform parent, string msg, int size, Color color, FontStyle style)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = size + 12;
            le.preferredHeight = size * (msg.Contains("\n") ? 3.2f : 1.4f);
            var t = go.AddComponent<Text>();
            t.font = uiFont;
            t.text = msg;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        Text MakeHudText(Transform parent, Vector2 anchor, Vector2 pos, int size, Color color, FontStyle style)
        {
            var go = new GameObject("HudTxt");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(1400, 60);
            rt.anchoredPosition = pos;
            var t = go.AddComponent<Text>();
            t.font = uiFont;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = TextAnchor.UpperCenter;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void SpawnPopup(Vector3 pos, string msg, Color color)
        {
            var go = new GameObject("Popup");
            go.transform.position = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = msg;
            tm.fontSize = 42;
            tm.characterSize = 0.09f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 30;
            go.AddComponent<FloatPopup>();
        }

        void BuildClips()
        {
            jumpClip = Tone(520f, 0.09f);
            orbClip = Tone(880f, 0.12f);
            clutchClip = Tone(1240f, 0.16f);
            cookClip = Tone(140f, 0.28f);
            winClip = Tone(660f, 0.4f);
            loseClip = Tone(90f, 0.5f);
        }

        static AudioClip Tone(float freq, float dur)
        {
            int n = Mathf.Max(64, (int)(44100 * dur));
            var clip = AudioClip.Create("t", n, 1, 44100, false);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / 44100f;
                d[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - t / dur) * 0.22f;
            }
            clip.SetData(d, 0);
            return clip;
        }

        void PlaySfx(AudioClip clip, float vol = 0.7f)
        {
            if (clip != null && sfx != null)
                sfx.PlayOneShot(clip, vol);
        }
    }

    public class FloatPopup : MonoBehaviour
    {
        float life = 0.85f;
        void Update()
        {
            life -= Time.deltaTime;
            transform.position += Vector3.up * Time.deltaTime * 1.4f;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
