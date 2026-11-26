using UnityEngine;

namespace Survival.Player
{
    /// <summary>Plays the walk cycle that matches the movement direction and speed.</summary>
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAnimator : MonoBehaviour
    {
        public enum Direction { Down, Up, Side }

        [SerializeField] SpriteRenderer visual;
        [SerializeField] Sprite[] walkDown = new Sprite[0];
        [SerializeField] Sprite[] walkUp = new Sprite[0];
        [SerializeField] Sprite[] walkSide = new Sprite[0];
        [Tooltip("Frames per second at full speed.")]
        [SerializeField] float framesPerSecond = 8f;
        [SerializeField] float moveThreshold = 0.2f;

        PlayerMovement _movement;
        Rigidbody2D _body;
        float _time;

        public Direction CurrentDirection { get; private set; } = Direction.Down;
        public int CurrentFrame { get; private set; }
        public bool IsWalking { get; private set; }

        void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _body = GetComponent<Rigidbody2D>();
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        }

        void LateUpdate()
        {
            Vector2 facing = _movement.Facing;
            CurrentDirection = DirectionFor(facing);
            if (CurrentDirection == Direction.Side) visual.flipX = facing.x < 0f;
            else visual.flipX = false;

            float speed = _body != null ? _body.linearVelocity.magnitude : 0f;
            IsWalking = speed > moveThreshold;
            Sprite[] frames = FramesFor(CurrentDirection);
            if (frames.Length == 0) return;

            if (IsWalking)
            {
                // Animation speed follows walking speed.
                float speedFactor = Mathf.Clamp01(speed / Mathf.Max(0.01f, _movement.MoveSpeed));
                _time += Time.deltaTime * framesPerSecond * Mathf.Lerp(0.5f, 1f, speedFactor);
                CurrentFrame = Mathf.FloorToInt(_time) % frames.Length;
            }
            else
            {
                _time = 0f;
                CurrentFrame = 0;
            }

            visual.sprite = frames[CurrentFrame];
        }

        public static Direction DirectionFor(Vector2 facing)
        {
            if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y)) return Direction.Side;
            return facing.y > 0f ? Direction.Up : Direction.Down;
        }

        Sprite[] FramesFor(Direction direction) => direction switch
        {
            Direction.Up => walkUp,
            Direction.Side => walkSide,
            _ => walkDown,
        };
    }
}
