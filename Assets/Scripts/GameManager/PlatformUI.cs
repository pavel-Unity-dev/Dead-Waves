using UnityEngine;

public class PlatformUI : MonoBehaviour
{
    [SerializeField] private GameObject mobileControls; // сюда перетащи MobileControls

    private void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS
        mobileControls.SetActive(true);
#else
        mobileControls.SetActive(false);
#endif
    }
}