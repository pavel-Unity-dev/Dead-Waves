using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public sealed class MobileLookInputMB : MonoBehaviour
{
    public static MobileLookInputMB Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private RectTransform _rightZone;

    public bool IsLooking { get; private set; }
    public Vector2 Delta { get; private set; }

    private int _lookFingerId = -1;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        IsLooking = false;
        Delta = Vector2.zero;

        // 1) ≈сли палец уже выбран Ч читаем только его.
        if (_lookFingerId != -1)
        {
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.finger.index != _lookFingerId)
                {
                    continue;
                }

                // ѕалец отпустили Ч сбрасываем.
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _lookFingerId = -1;
                    return;
                }

                //  рутим камеру только когда палец реально двигаетс€.
                if (touch.phase == TouchPhase.Moved)
                {
                    Delta = touch.delta;
                    IsLooking = true;
                }

                return;
            }

            // ≈сли выбранного пальца нет в activeTouches Ч сбрасываем.
            _lookFingerId = -1;
            return;
        }

        // 2) »наче ищем новый палец (только Began в правой зоне).
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase != TouchPhase.Began)
            {
                continue;
            }

            if (!IsInRightZone(touch.screenPosition))
            {
                continue;
            }

            _lookFingerId = touch.finger.index;
            return;
        }
    }

    private bool IsInRightZone(Vector2 screenPos)
    {
        if (_rightZone == null)
        {
            return screenPos.x >= Screen.width * 0.5f;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(_rightZone, screenPos, null);
    }
}