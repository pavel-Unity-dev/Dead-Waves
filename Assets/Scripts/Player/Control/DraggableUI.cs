using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableUI : MonoBehaviour, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    [Header("Settings")]
    public bool editMode = false;

    [Header("Save Key (unique name!)")]
    public string saveKey = "Button";

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        LoadPosition();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!editMode) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void SavePosition()
    {
        PlayerPrefs.SetFloat(saveKey + "_X", rectTransform.anchoredPosition.x);
        PlayerPrefs.SetFloat(saveKey + "_Y", rectTransform.anchoredPosition.y);
    }

    public void LoadPosition()
    {
        if (PlayerPrefs.HasKey(saveKey + "_X"))
        {
            rectTransform.anchoredPosition = new Vector2(
                PlayerPrefs.GetFloat(saveKey + "_X"),
                PlayerPrefs.GetFloat(saveKey + "_Y")
            );
        }
    }
}