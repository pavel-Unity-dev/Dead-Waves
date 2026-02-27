//using UnityEngine;

//public class EnemyVisibleCulling : MonoBehaviour
//{
//    private Animator anim;
//    private Renderer[] renderers;

//    private void Awake()
//    {
//        anim = GetComponent<Animator>();
//        renderers = GetComponentsInChildren<Renderer>(true);
//    }

//    private void Update()
//    {
//        if (anim == null || renderers == null) return;

//        bool anyVisible = false;
//        for (int i = 0; i < renderers.Length; i++)
//        {
//            if (renderers[i] != null && renderers[i].isVisible)
//            {
//                anyVisible = true;
//                break;
//            }
//        }

//        if (anim.enabled != anyVisible)
//            anim.enabled = anyVisible;
//    }
//}
using UnityEngine;

public class EnemyVisibleCulling : MonoBehaviour
{
    [SerializeField] private float checkInterval = 0.2f; // 5 раз/сек
    [SerializeField] private Renderer[] renderers;

    private Animator anim;
    private float nextCheckTime;
    private bool lastVisible;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        // чтобы все враги не проверялись в один кадр
        nextCheckTime = Time.time + Random.Range(0f, checkInterval);
    }

    private void Update()
    {
        if (anim == null || renderers == null) return;

        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + checkInterval;

        bool anyVisible = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r != null && r.isVisible)
            {
                anyVisible = true;
                break;
            }
        }

        if (anyVisible == lastVisible) return;
        lastVisible = anyVisible;

        anim.enabled = anyVisible;
    }
}