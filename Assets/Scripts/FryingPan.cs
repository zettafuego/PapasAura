using UnityEngine;

namespace PapasAura
{
    public class FryingPan : MonoBehaviour
    {
        public float Y { get; private set; }

        float speed = 0.32f;
        bool rising;
        float shake;

        public void Setup(float y)
        {
            Y = y;
            rising = false;
            transform.position = new Vector3(0.8f, y, 0.5f);
            transform.localScale = new Vector3(1.55f, 1.1f, 1f);
        }

        public void StartRising()
        {
            rising = true;
        }

        void Update()
        {
            if (!rising) return;
            speed = Mathf.Min(1.15f, speed + Time.deltaTime * 0.018f);
            Y += speed * Time.deltaTime;
            shake += Time.deltaTime * 18f;
            transform.position = new Vector3(Mathf.Sin(shake) * 0.15f + 0.6f, Y, 0.5f);
        }
    }
}
