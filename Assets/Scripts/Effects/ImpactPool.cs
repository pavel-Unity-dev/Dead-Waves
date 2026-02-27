using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImpactPool : MonoBehaviour
{
    public static ImpactPool I;

    [SerializeField] private GameObject prefab;
    [SerializeField] private int prewarm = 20;
    [SerializeField] private float lifeTime = 1.5f;

    private readonly Queue<GameObject> pool = new();

    private void Awake()
    {
        I = this;

        for (int i = 0; i < prewarm; i++)
        {
            var go = Instantiate(prefab);
            go.SetActive(false);
            pool.Enqueue(go);
        }
    }

    public void Spawn(Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return;

        GameObject go = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);

        StartCoroutine(ReturnLater(go));
    }

    private IEnumerator ReturnLater(GameObject go)
    {
        yield return new WaitForSeconds(lifeTime);
        if (go == null) yield break;
        go.SetActive(false);
        pool.Enqueue(go);
    }
}