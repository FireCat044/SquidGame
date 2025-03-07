using NUnit.Framework;
using Player;
using System.Reflection;
using UnityEngine;

namespace Player
{
    public partial class PlayerController : MonoBehaviour
    {
        private bool _hasAnimator;
        private Animator _animator;
        private int _animIDJump;
        private float _jumpTimeoutDelta;
        private float JumpTimeout; // Предполагается, что это поле определено где-то в классе
        private float _fallTimeoutDelta;
        private int _animIDFreeFall;
        private bool Grounded;
        private float _verticalVelocity;
        private float _terminalVelocity;
        private float Gravity; // Предполагается, что это поле определено где-то в классе
        private PlayerInput _input; // Предполагаемый класс для ввода
        private bool _isDead;
        private int _animIDDeath;
        private CharacterController _controller;
        private DeathScreen DeathScript; // Предполагаемый класс для экрана смерти
        private float GroundedOffset;
        private float GroundedRadius;
        private AudioClip[] FootstepAudioClips;
        private float FootstepAudioVolume;
        private AudioClip LandingAudioClip;

        // Пример инициализации, чтобы код был рабочим
        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _hasAnimator = _animator != null;
            _controller = GetComponent<CharacterController>();
            _input = new PlayerInput(); // Предполагается, что это ваш класс ввода
            JumpTimeout = 0.5f; // Пример значения
            Gravity = -9.81f; // Пример значения
            _terminalVelocity = 53.0f; // Пример значения
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDDeath = Animator.StringToHash("Death");
        }

        private void Update()
        {
            if (Grounded)
            {
                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, true);
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void DeathPlayer()
        {
            if (Input.GetKeyDown(KeyCode.K) && !_isDead) // Запускаємо смерть тільки якщо живий
            {
                _isDead = true; // Позначаємо, що персонаж мертвий
                _animator.SetBool(_animIDDeath, true);

                // Вимикаємо введення користувача
                _input.move = Vector2.zero;
                _input.jump = false;
                _input.sprint = false;

                // Блокування контролера руху
                _controller.enabled = false;

                // Активуємо панель смерті
                if (DeathScript != null)
                {
                    DeathScript.GameOver(); // Увімкнення Death_Screen
                }
                else
                {
                    Debug.LogWarning("DeathScreen is not assigned, cannot enable it!");
                }
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            var (color, position, radius) = GetGizmoDrawParameters();
            Gizmos.color = color;
            Gizmos.DrawSphere(position, radius);
        }

        public (Color color, Vector3 position, float radius) GetGizmoDrawParameters()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Color color = Grounded ? transparentGreen : transparentRed;
            Vector3 position = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            float radius = GroundedRadius;

            return (color, position, radius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }

    // Заглушка для PlayerInput, чтобы код компилировался
    public class PlayerInput
    {
        public Vector2 move;
        public bool jump;
        public bool sprint;
    }

    // Заглушка для DeathScreen, чтобы код компилировался
    public class DeathScreen
    {
        public void GameOver() { }
    }
}

[TestFixture]
public class PlayerControllerTests
{
    private PlayerController _playerController;

    [SetUp]
    public void SetUp()
    {
        _playerController = new GameObject().AddComponent<PlayerController>();

        // Устанавливаем значения по умолчанию через рефлексию
        typeof(PlayerController).GetField("Grounded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_playerController, true);
        typeof(PlayerController).GetField("GroundedOffset", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_playerController, 0.1f);
        typeof(PlayerController).GetField("GroundedRadius", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_playerController, 0.5f);
    }

    [Test]
    public void GetGizmoDrawParameters_WhenGrounded_ReturnsGreenColor()
    {
        // Arrange
        // Намеренно задаем неверный ожидаемый цвет, чтобы тест провалился
        Color expectedColor = new Color(1.0f, 0.0f, 0.0f, 0.35f); // Ожидаем красный вместо зеленого
        typeof(PlayerController).GetField("Grounded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_playerController, true);

        // Act
        var (color, _, _) = _playerController.GetGizmoDrawParameters();

        // Assert
        Assert.AreEqual(expectedColor, color, "Color should be transparentGreen when Grounded is true."); // Этот тест провалится
    }

    [Test]
    public void GetGizmoDrawParameters_WhenNotGrounded_ReturnsRedColor()
    {
        // Arrange
        Color expectedColor = new Color(1.0f, 0.0f, 0.0f, 0.35f); // transparentRed
        typeof(PlayerController).GetField("Grounded", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_playerController, false);

        // Act
        var (color, _, _) = _playerController.GetGizmoDrawParameters();

        // Assert
        Assert.AreEqual(expectedColor, color, "Color should be transparentRed when Grounded is false.");
    }

    [Test]
    public void GetGizmoDrawParameters_ReturnsCorrectPositionAndRadius()
    {
        // Arrange
        Vector3 initialPosition = new Vector3(1f, 2f, 3f);
        _playerController.transform.position = initialPosition;
        float groundedOffset = 0.1f;
        float groundedRadius = 0.5f;
        Vector3 expectedPosition = new Vector3(initialPosition.x, initialPosition.y - groundedOffset, initialPosition.z);

        // Устанавливаем значения через рефлексию
        typeof(PlayerController).GetField("GroundedOffset", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_playerController, groundedOffset);
        typeof(PlayerController).GetField("GroundedRadius", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(_playerController, groundedRadius);

        // Act
        var (_, position, radius) = _playerController.GetGizmoDrawParameters();

        // Assert
        Assert.AreEqual(expectedPosition, position, "Position should be offset by GroundedOffset.");
        Assert.AreEqual(groundedRadius, radius, "Radius should match GroundedRadius.");
    }
}