using UnityEngine;

public class EnemyDeathReporter : MonoBehaviour
{
    private WaveManager _wm;
    private bool _reported;

    public void Init(WaveManager wm)
    {
        _wm = wm;
        _reported = false;
    }

    public void NotifyDeath()
    {
        if (_reported) return;
        _reported = true;

        if (_wm != null)
            _wm.NotifyEnemyDied();
    }

    public void ResetReporter()
    {
        _reported = false;
    }
}