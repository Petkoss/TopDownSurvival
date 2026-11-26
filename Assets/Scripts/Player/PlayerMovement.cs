using UnityEngine;
using UnityEngine.InputSystem;

namespace Survival.Player
{
    /// <summary>Top-down 8-directional movement driven by a physics body.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 4.5f;
        [SerializeField] float acceleration = 40f;

        Rigidbody2D _body;
        InputAction _move;
        Vector2 _input;

        public float MoveSpeed => moveSpeed;

        /// <summary>Last non-zero movement direction (normalized).</summary>
        public Vector2 Facing { get; private set; } = Vector2.down;

        /// <summary>Lets scripts (and tests) drive movement. Null = use player input.</summary>
        public Vector2? OverrideInput { get; set; }

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;

            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");
        }

        void OnEnable() => _move.Enable();
        void OnDisable() => _move.Disable();
        void OnDestroy() => _move.Dispose();

        void Update()
        {
            _input = OverrideInput ?? _move.ReadValue<Vector2>();
            if (_input.sqrMagnitude > 1f) _input.Normalize();

            if (_input.sqrMagnitude > 0.01f) Facing = _input.normalized;
        }

        void FixedUpdate()
        {
            Vector2 target = _input * moveSpeed;
            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, target, acceleration * Time.fixedDeltaTime);
        }

        /// <summary>Instantly moves the player (spawning, loading).</summary>
        public void Teleport(Vector2 position)
        {
            if (_body == null) _body = GetComponent<Rigidbody2D>();
            _body.position = position;
            _body.linearVelocity = Vector2.zero;
            transform.position = position;
            Physics2D.SyncTransforms();
        }
    }
}
