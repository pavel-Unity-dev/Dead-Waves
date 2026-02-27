using UnityEngine;

public class EnemySFX : MonoBehaviour
{
    [SerializeField] private AudioSource source;

    [Header("Clips")]
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip hitClip; // опционально

    [Header("Volumes")]
    [Range(0f, 1f)] public float attackVolume = 0.35f;
    [Range(0f, 1f)] public float shootVolume = 0.35f;
    [Range(0f, 1f)] public float deathVolume = 0.45f;
    [Range(0f, 1f)] public float hitVolume = 0.25f;

    private void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
    }

    public void PlayAttack() => Play(attackClip, attackVolume);

    public void PlayShoot() => Play(shootClip, attackVolume);
    public void PlayDeath() => Play(deathClip, deathVolume);
    public void PlayHit() => Play(hitClip, hitVolume);

    private void Play(AudioClip clip, float vol)
    {
        if (clip == null || source == null) return;
        source.PlayOneShot(clip, vol);
    }
}
