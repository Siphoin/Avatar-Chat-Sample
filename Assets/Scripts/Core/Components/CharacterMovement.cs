using AvatarChat.Extensions;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AvatarChat.Core.Components
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterMovement : NetworkBehaviour
    {
        [SerializeField] private float _speed = 5f;
        [SerializeField, ReadOnly] private Rigidbody2D _rigidbody;
        [SerializeField] private float _reconciliationSpeed = 15f;
        [SerializeField] private float _errorThreshold = 0.5f;

        private Vector2 _clientPredictedTarget;
        private Vector2 _lastServerPosition;
        private bool _hasServerPosition;
        private EventSystem _eventSystem;

        private readonly NetworkVariable<Vector2> _serverTargetPosition = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _eventSystem = EventSystem.current;

            int characterLayer = LayerMask.NameToLayer("Character");
            if (characterLayer != -1)
            {
                Physics2D.IgnoreLayerCollision(characterLayer, characterLayer, true);
            }

            if (IsServer)
            {
                _rigidbody.bodyType = RigidbodyType2D.Dynamic;
                _rigidbody.gravityScale = 0;
                _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            else
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }

            _clientPredictedTarget = transform.position;
            _lastServerPosition = transform.position;

            _serverTargetPosition.OnValueChanged += OnServerTargetPositionChanged;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _serverTargetPosition.OnValueChanged -= OnServerTargetPositionChanged;
        }

        private void OnServerTargetPositionChanged(Vector2 previous, Vector2 current)
        {
            _lastServerPosition = current;
            _hasServerPosition = true;
        }

        private void Update()
        {
            if (IsClient && IsOwner)
            {
                HandleInput();
                PredictMovement();
                ReconcileWithServer();
            }
            else if (!IsServer)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    _serverTargetPosition.Value,
                    _speed * Time.deltaTime
                );
            }
        }

        private void ReconcileWithServer()
        {
            if (!_hasServerPosition) return;

            float distanceToTarget = Vector2.Distance(_clientPredictedTarget, _serverTargetPosition.Value);
            if (distanceToTarget > 0.1f) return;

            float error = Vector2.Distance(transform.position, _lastServerPosition);
            if (error > _errorThreshold)
            {
                transform.position = Vector2.Lerp(
                    transform.position,
                    _lastServerPosition,
                    _reconciliationSpeed * Time.deltaTime
                );
            }
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                MoveTowardsTarget();
            }
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_eventSystem != null && _eventSystem.IsPointerOverUIObject())
                {
                    return;
                }

                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                _clientPredictedTarget = mouseWorldPos;
                RequestMoveServerRpc(mouseWorldPos);
            }
        }

        private void PredictMovement()
        {
            if ((Vector2)transform.position != _clientPredictedTarget)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    _clientPredictedTarget,
                    _speed * Time.deltaTime
                );
            }
        }

        private void MoveTowardsTarget()
        {
            if (Vector2.Distance(_rigidbody.position, _serverTargetPosition.Value) > 0.01f)
            {
                Vector2 nextPos = Vector2.MoveTowards(
                    _rigidbody.position,
                    _serverTargetPosition.Value,
                    _speed * Time.fixedDeltaTime
                );
                _rigidbody.MovePosition(nextPos);
            }
        }

        [ServerRpc]
        private void RequestMoveServerRpc(Vector2 target)
        {
            _serverTargetPosition.Value = target;
        }

        private void OnValidate()
        {
            if (!_rigidbody) _rigidbody = GetComponent<Rigidbody2D>();
        }
    }
}