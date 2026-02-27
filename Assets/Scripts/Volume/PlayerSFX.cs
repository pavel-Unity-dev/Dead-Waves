using UnityEngine;

public class PlayerSFX : MonoBehaviour
{
    [SerializeField] private AudioSource source;

    [Header("Clips")]
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip reloadClip;
    [SerializeField] private AudioClip emptyClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float shotVolume = 0.4f;
    [Range(0f, 1f)] public float reloadVolume = 0.3f;
    [Range(0f, 1f)] public float emptyVolume = 0.25f;

    private void Awake()
    {
        if (source == null)
            source = GetComponent<AudioSource>();
    }

    public void PlayShot() => source.PlayOneShot(shotClip, shotVolume);
    public void PlayReload() => source.PlayOneShot(reloadClip, reloadVolume);
    public void PlayEmpty() => source.PlayOneShot(emptyClip, emptyVolume);
}
