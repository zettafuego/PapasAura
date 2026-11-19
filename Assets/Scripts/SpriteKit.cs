using UnityEngine;

namespace PapasAura
{
    public static class SpriteKit
    {
        public static Sprite Potato(string name)
        {
            return Load(name, new Vector2(0.5f, 0.06f), 380f);
        }

        public static Sprite Orb()
        {
            return Load("aura_orb", new Vector2(0.5f, 0.5f), 520f);
        }

        public static Sprite Platform()
        {
            return Load("platform", new Vector2(0.5f, 0.84f), 210f);
        }

        public static Sprite Pan()
        {
            return Load("frying_pan", new Vector2(0.5f, 0.62f), 90f);
        }

        public static Sprite Background()
        {
            return Load("bg_farm", new Vector2(0.5f, 0.5f), 40f);
        }

        static Sprite Load(string name, Vector2 pivot, float ppu)
        {
            var tex = Resources.Load<Texture2D>("Sprites/" + name);
            if (tex == null)
            {
                Debug.LogWarning("Papas Aura: falta Sprites/" + name);
                return Fallback(pivot);
            }

            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                pivot,
                ppu,
                0,
                SpriteMeshType.FullRect);
        }

        static Sprite Fallback(Vector2 pivot)
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var c = new Color(0.85f, 0.7f, 0.25f, 1f);
            var px = new Color[64];
            for (int i = 0; i < 64; i++) px[i] = c;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), pivot, 8f);
        }
    }
}
