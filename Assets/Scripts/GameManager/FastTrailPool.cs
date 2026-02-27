using System.Collections.Generic;
using UnityEngine;

public class FastTrailPool : MonoBehaviour
{
    public static FastTrailPool I;

    [SerializeField] private TrailRenderer prefab;
    [SerializeField] private int count = 80;

    private readonly Queue<TrailRenderer> pool = new Queue<TrailRenderer>();

    private class ActiveTrail
    {
        public TrailRenderer tr;
        public Vector3 start;
        public Vector3 end;
        public float t;
        public float speed;
        public float returnAt;
    }

    private readonly List<ActiveTrail> active = new List<ActiveTrail>(256);

    private void Awake()
    {
        I = this;

        for (int i = 0; i < count; i++)
        {
            var tr = Instantiate(prefab, transform);
            tr.gameObject.SetActive(false);
            pool.Enqueue(tr);
        }
    }

    public void Spawn(Vector3 startPos, Vector3 endPos, float trailSpeed)
    {
        if (prefab == null) return;

        if (pool.Count == 0)
        {
            var extra = Instantiate(prefab, transform);
            extra.gameObject.SetActive(false);
            pool.Enqueue(extra);
        }

        var tr = pool.Dequeue();
        tr.gameObject.SetActive(true);
        tr.transform.position = startPos;
        tr.Clear();

        float dist = Vector3.Distance(startPos, endPos);
        dist = Mathf.Max(0.01f, dist);

        float travelTime = dist / Mathf.Max(0.01f, trailSpeed); // сколько времени летит до end
        float returnAt = Time.time + travelTime + tr.time;      // долет + время хвоста

        float spd = 1f / travelTime; // чтобы t дошёл до 1 за travelTime

        active.Add(new ActiveTrail
        {
            tr = tr,
            start = startPos,
            end = endPos,
            t = 0f,
            speed = spd,
            returnAt = returnAt
        });

    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var a = active[i];

            // двигаем трейл к точке
            a.t += Time.deltaTime * a.speed;
            float tt = Mathf.Clamp01(a.t);
            a.tr.transform.position = Vector3.Lerp(a.start, a.end, tt);

            // возвращаем в пул после времени хвоста
            if (Time.time >= a.returnAt)
            {
                a.tr.gameObject.SetActive(false);
                pool.Enqueue(a.tr);
                active.RemoveAt(i);
            }
        }
    }
}
