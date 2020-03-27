using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;

namespace App
{

    class DestinationResolver
    {
        private NavMeshAgent _agent;
        private Vector2 _ref1 = Vector2.zero;
        private Vector2 _ref2 = Vector2.zero;
        
        public DestinationResolver(NavMeshAgent agent)
        {
            _agent = agent;
        }

        public Vector3 GetFinalDestination()
        {
            var corners = _agent.path.corners;
            if (corners.Length == 0)
            {
                return _agent.destination;
            }
            else
            {
                return corners[corners.Length - 1];
            }
        }
        public Vector3 GetNextDestination(Vector3 currentPosition)
        {
            //近い順に計算する
            var corners = _agent.path.corners;
            foreach (var corner in corners) {
                if (Distance2D(corner, currentPosition) >= 2.5f * Time.deltaTime)
                {
                    return corner;
                }
            }

            return GetFinalDestination();
        }

        public bool IsReady()
        {
            return !_agent.pathPending;
        }

        public bool IsReached(Vector3 currentPosition)
        {
            var distance = Distance2D(GetFinalDestination(), currentPosition);
            return distance <= 0.01f;
        }
        
        public float Distance2D(Vector3 target, Vector3 origin)
        {
            _ref1.x = target.x;
            _ref1.y = target.z;
            _ref2.x = origin .x;
            _ref2.y = origin.z;
            return (_ref1 - _ref2).magnitude;
        }

    }
    public class Mover
    {
        private readonly Transform _transform;
        private readonly CharacterController _characterController;
        private readonly DestinationResolver _destinationResolver;
        private Vector2 _ref1 = Vector2.zero;
        private Vector2 _ref2 = Vector2.zero;
        
        private Vector3 _prevPosition;

        private const float RotationThreshold = 45;
        private const float BaseSpeed = 2.5f;
        private const float BaseRotationSpeed = 1000f;

        public Mover(NavMeshAgent navMeshAgent, CharacterController characterController)
        {
            _transform = characterController.transform;
            _characterController = characterController;
            _prevPosition = _transform.position;
            _destinationResolver = new DestinationResolver(navMeshAgent);
        }

        public float GetSpeed()
        {
            var currentPosition = _transform.position;
            var speed = _destinationResolver.Distance2D(currentPosition, _prevPosition) / Time.deltaTime;
            _prevPosition = currentPosition;
            return speed;
        }

        public void Move(bool shouldMove)
        {
            var move = Vector3.zero;
            if (shouldMove && _destinationResolver.IsReady())
            {
                var currentPosition = _transform.position;

                //最終目的地に到達していない場合
                if (!_destinationResolver.IsReached(currentPosition))
                {
                    var directionToNext = _destinationResolver.GetNextDestination(currentPosition) - currentPosition;
                    var distanceToFinal =
                        _destinationResolver.Distance2D(_destinationResolver.GetFinalDestination(), currentPosition);
                    var forward = _transform.forward;

                    //SignedAngleのAngleは回転がなくても45~-45になったりするため、符号の取得のみに利用する
                    //https://forum.unity.com/threads/is-vector3-signedangle-working-as-intended.694105/
                    var sign = Mathf.Sign(Vector3.SignedAngle(forward, directionToNext, Vector3.up));
                    var angle = Angle2D(forward, directionToNext);

                    // 急旋回
                    if (Mathf.Abs(angle) > RotationThreshold)
                    {
                        var rotation = Mathf.Min(BaseRotationSpeed * Time.deltaTime, angle) * sign;
                        _transform.Rotate(0f, rotation, 0f);
                        move = Vector3.zero;
                    }
                    else
                    {
                        // 移動しながら回転する。目的地に近くほど減衰
                        var rotation = Mathf.Min(angle, distanceToFinal) * sign;
                        _transform.Rotate(0f, rotation, 0f);

                        //移動、目的地に近くほど減衰
                        var speed = BaseSpeed * Time.deltaTime * Mathf.Min(1, distanceToFinal);
                        move = _transform.forward * speed;
                    }
                }
            }

            //Y方向は重力のみで落とす
            move.y = Physics.gravity.y * Time.deltaTime;
            _characterController.Move(move);
            
        }
        

        private float Angle2D(Vector3 forward, Vector3 target)
        {
            _ref1.x = forward.x;
            _ref1.y = forward.z;
            _ref2.x = target.x;
            _ref2.y = target.z;
            return Vector2.Angle(_ref1, _ref2);
        }

      
    }
}