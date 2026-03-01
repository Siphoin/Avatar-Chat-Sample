using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace AvatarChat.Core.Components
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(NetworkRigidbody2D))]
    public class CharacterMovement : NetworkBehaviour
    {
        [SerializeField] private float _speed = 5f;
        [SerializeField, ReadOnly] private Rigidbody2D _rigidbody;

        private Vector2 _clientPredictedTarget;
        private readonly NetworkVariable<Vector2> _serverTargetPosition = new(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                _rigidbody.bodyType = RigidbodyType2D.Dynamic;
                _rigidbody.gravityScale = 0;
                _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;
            }
            else
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }

            if (IsOwner)
            {
                _clientPredictedTarget = transform.position;
            }
        }

        private void Update()
        {
            if (IsClient && IsOwner)
            {
                HandleInput();
                PredictMovement();
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
                Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                _clientPredictedTarget = mouseWorldPos;
                RequestMoveServerRpc(mouseWorldPos);
            }
        }

        private void PredictMovement()
        {
            Vector2 currentPos = transform.position;
            if (currentPos != _clientPredictedTarget)
            {
                transform.position = Vector2.MoveTowards(
                    currentPos,
                    _clientPredictedTarget,
                    _speed * Time.deltaTime
                );
            }
        }

        private void MoveTowardsTarget()
        {
            Vector2 currentPos = _rigidbody.position;
            if (currentPos != _serverTargetPosition.Value)
            {
                _rigidbody.WakeUp();
                Vector2 nextPos = Vector2.MoveTowards(
                    currentPos,
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
            if (!_rigidbody)
            {
                _rigidbody = GetComponent<Rigidbody2D>();
            }
        }
    }
}