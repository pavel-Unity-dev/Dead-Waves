using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    #region singleton
    public static PlayerManager instance;
    

    private void Awake()
    {
        instance = this;
    }
    #endregion  
    public GameObject player;

    private void Start()
    {
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount = 0;
    }

}
