using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MouseLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float _sensitivity = 100f;
    [SerializeField] private float _mobileSensitivity = 0.2f;

    [Header("Recoil")]
    [SerializeField] private float _recoilReturnSpeed = 25f; // как быстро возвращается
    private float _recoilCurrent; // текущая отдача (в градусах)

    private PlayerInputController _controls;
    private Vector2 _lookInput;

    private float _rotationX;

    private const float MinPitch = -80f;
    private const float MaxPitch = 80f;

    private void Awake()
    {
        _controls = new PlayerInputController();
    }

    private void Start()
    {
        if (Application.isMobilePlatform)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        _controls.Player.Enable();

        // ВАЖНО: подписываемся на методы, а не на лямбды (так легче отписаться и нет мусора).
        _controls.Player.Look.performed += OnLookPerformed;
        _controls.Player.Look.canceled += OnLookCanceled;
    }

    private void OnDisable()
    {
        // Отписка обязательна, иначе могут быть странные баги/дубли ввода при Enable/Disable.
        _controls.Player.Look.performed -= OnLookPerformed;
        _controls.Player.Look.canceled -= OnLookCanceled;

        _controls.Player.Disable();
    }

    private void Update()
    {
        Vector2 finalLook = GetLookDelta();

        float yaw = finalLook.x;
        float pitch = finalLook.y;

        // Базовый угол от мыши/свайпа
        _rotationX -= pitch;
        _rotationX = Mathf.Clamp(_rotationX, MinPitch, MaxPitch);

        // Плавно возвращаем recoil к нулю
        _recoilCurrent = Mathf.MoveTowards(_recoilCurrent, 0f, _recoilReturnSpeed * Time.deltaTime);

        // Итоговый угол = базовый + отдача вверх
        float finalPitch = _rotationX - _recoilCurrent; // минус = камера уходит вверх
        finalPitch = Mathf.Clamp(finalPitch, MinPitch, MaxPitch);

        transform.localRotation = Quaternion.Euler(finalPitch, 0f, 0f);
        transform.parent.Rotate(Vector3.up * yaw);
    }

    public void AddRecoil(float kick)
    {
        _recoilCurrent += kick; // kick в градусах (например 1.5f)
    }

    private Vector2 GetLookDelta()
    {
        if (Application.isMobilePlatform)
        {
            var mobile = MobileLookInputMB.Instance;

            if (mobile != null && mobile.IsLooking)
            {
                return mobile.Delta * _mobileSensitivity;
            }

            return Vector2.zero;
        }

        // На ПК deltaTime нужен, на мобиле — нет (там уже delta от тача)
        return _lookInput * (_sensitivity * Time.deltaTime);
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        _lookInput = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        _lookInput = Vector2.zero;
    }
}