using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController _controller;
    [SerializeField] private Transform _groundCheck;

    [Header("Movement")]
    [SerializeField] private float _speed = 5f;

    [Header("Jump & Gravity")]
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _jumpHeight = 6f;

    [Header("Ground Check")]
    [SerializeField] private float _groundDistance = 0.4f;
    [SerializeField] private LayerMask _groundMask;

    private PlayerInputController _controls;
    private Vector2 _moveInput;

    private Vector3 _velocity;
    private bool _isGrounded;
    private bool _jumpQueued;

    private void Awake()
    {
        if (_controller == null)
        {
            _controller = GetComponent<CharacterController>();
        }

        _controls = new PlayerInputController();
    }

    private void OnEnable()
    {
        _controls.Player.Enable();

        _controls.Player.Move.performed += OnMove;
        _controls.Player.Move.canceled += OnMoveCanceled;

        _controls.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        _controls.Player.Jump.performed -= OnJump;

        _controls.Player.Move.performed -= OnMove;
        _controls.Player.Move.canceled -= OnMoveCanceled;

        _controls.Player.Disable();
    }

    private void Update()
    {
        UpdateGrounded();
        HandleJump();
        HandleMovement();
        ApplyGravity();
    }

    public void JumpFromUIButton()
    {
        _jumpQueued = true;
    }

    private void UpdateGrounded()
    {
        if (_groundCheck == null)
        {
            _isGrounded = _controller.isGrounded;
            return;
        }

        _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundMask);

        if (_isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -2f;
        }
    }

    private void HandleJump()
    {
        if (_jumpQueued && _isGrounded)
        {
            // v = sqrt(h * -2 * g)
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        _jumpQueued = false;
    }

    private void HandleMovement()
    {
        Vector3 move =
            transform.right * _moveInput.x +
            transform.forward * _moveInput.y;

        _controller.Move(move * (_speed * Time.deltaTime));
    }

    private void ApplyGravity()
    {
        _velocity.y += _gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _moveInput = Vector2.zero;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        _jumpQueued = true;
    }
}