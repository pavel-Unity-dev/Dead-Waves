using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        transform.forward = cam.transform.forward;
    }
}
