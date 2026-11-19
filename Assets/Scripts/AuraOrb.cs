using UnityEngine;

namespace PapasAura
{
    public class AuraOrb : MonoBehaviour
    {
        public int Value = 12;
        public bool Alive = true;

        float bob;
        Vector3 origin;

        public void Setup(Vector3 pos, int value)
        {
            origin = pos;
            transform.position = pos;
            Value = value;
            Alive = true;
            bob = Random.Range(0f, Mathf.PI * 2f);
            transform.localScale = Vector3.one * 0.85f;
        }

        void Update()
        {
            if (!Alive) return;
            bob += Time.deltaTime * 3f;
            transform.position = origin + Vector3.up * (Mathf.Sin(bob) * 0.22f);
            float pulse = 0.85f + Mathf.Sin(bob * 1.6f) * 0.08f;
            transform.localScale = Vector3.one * pulse;
        }

        public void Nudge(Vector3 delta)
        {
            origin += delta;
            transform.position += delta;
        }

        public void Collect()
        {
            Alive = false;
            Destroy(gameObject);
        }
    }
}
