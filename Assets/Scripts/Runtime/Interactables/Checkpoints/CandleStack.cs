using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class CandleStack : MonoBehaviour
{
    private class Candle
    {
        public Animator animator;
        public Transform transform;
        public Light2D light2D;

        public Candle(Transform transform)
        {
            this.transform = transform;
            this.animator = transform.GetComponent<Animator>();

            this.light2D = transform.GetComponentInChildren<Light2D>();
            light2D.enabled = false;
        }

        public void Light()
        {
            light2D.enabled = true;
            this.animator.SetBool("lit", true);
        }
    }

    [SerializeField] private int candlesAmount = 10;
    [SerializeField] private GameObject[] candlePrefabs; //Different variations
    [SerializeField] private float candleOffsets = 0.1f;
    [SerializeField] private SoundManager.SoundVariations candleLights;
    [SerializeField] private Vector2 offset;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Vector2 lightDelayRange = new Vector2(0.05f, 0.2f);

    private List<Candle> candles = new();

    void Start()
    {
        transform.Find("GameObject").GetComponent<SpriteRenderer>().enabled = false;

        for (int i = 0; i < candlesAmount; i++)
        {
            GameObject go = Instantiate(candlePrefabs[Mathf.FloorToInt(Random.Range(0, candlePrefabs.Length))], transform);
            go.transform.position = (Vector2)transform.position + offset + new Vector2(candleOffsets * i, 0);
            candles.Add(new Candle(go.transform));
        }
    }

    public void Light(int amount)
    {
        StopAllCoroutines();
        StartCoroutine(LightRoutine(Mathf.Clamp(amount, 0, candles.Count)));
    }

    private System.Collections.IEnumerator LightRoutine(int amount)
    {
        float minDelay = Mathf.Min(lightDelayRange.x, lightDelayRange.y);
        float maxDelay = Mathf.Max(lightDelayRange.x, lightDelayRange.y);

        for (int i = 0; i < amount; i++)
        {
            candles[i].Light();
            if (audioSource)
            {
                candleLights.PlaySound(audioSource);
            }
            float delay = Random.Range(minDelay, maxDelay);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            else
                yield return null;
        }
    }
}
