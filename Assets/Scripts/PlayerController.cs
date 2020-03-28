using App;
using UnityEngine;
using UnityEngine.AI;
using VRM;

namespace App
{

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        private readonly int _animWait = Animator.StringToHash("Base Layer.WAIT");
        private bool _shouldMove = false;
        private bool _shouldWarp = false;
        private bool _warped = false;

        private NavMeshAgent _navMeshAgent;
        private Animator _animator;
        private Mover _mover;
        private FPCamera _firstPersonCamera;
        private bool _ready = false;
        private Rigidbody _rigid;
        private CharacterController _characterController;
        
        public virtual void Start()
        {
            _animator = GetComponent<Animator>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _characterController = GetComponent<CharacterController>();
            
            //自動移動の防止
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.updateRotation = false;

            _mover = new Mover(_navMeshAgent, _characterController);

            _firstPersonCamera = new FPCamera(GetComponent<VRMFirstPerson>());
        }

        public void UpdatedAnchors(OVRCameraRig rig)
        {
            if (!_ready && rig.centerEyeAnchor.localPosition.y > 0)
            {
                _firstPersonCamera.InitializeTrackingSpace(rig);
                _firstPersonCamera.ChangeCameraLayer();
                _ready = true;
                _warped = true;
            }
        }
        
        void FixedUpdate()
        {
            if (!_ready)
            {
                return;
            }

            _animator.enabled = true;

            _mover.Move(_shouldMove);
            if(!_shouldMove)
            {
                var state = _animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == _animWait)
                {
                    // 止まった時の目線位置にワープさせたいので、アニメーションがWAITに切り替わったタイミングでワープする。
                    _animator.enabled = false;
                    if (_shouldWarp)
                    {
                        _shouldWarp = false;
                        _firstPersonCamera.Warp();
                        _warped = true;
                    }
                    else
                    {
                        _firstPersonCamera.SyncCameraAndRig();
                        SyncRealWorldTransform();
                    }
                }
            }

            AfterMove();
        }

        protected virtual void SyncRealWorldTransform()
        {
            _firstPersonCamera.SyncRealWorldTransform(_animator);
        }

        void LateUpdate()
        {
            if (_warped)
            {
                _warped = false;
                //XXX カメラが常時ぶれてるようで補正が必要? ２連続ワープで回避する。
                _firstPersonCamera.Warp();
                AfterWarp();
            }
        }

        protected virtual void AfterWarp()
        {
        }

        public virtual bool OnDestinationChanged(Vector3 newDestination)
        {
            _shouldMove = _navMeshAgent.SetDestination(newDestination);
            return _shouldMove;
        }

        public void OnStop()
        {
            _shouldMove = false;
            _shouldWarp = true;
            _navMeshAgent.SetDestination(transform.position);
        }

        void AfterMove()
        {
            _firstPersonCamera.ChangeCameraLayer();
            var speed = _mover.GetSpeed();
            _animator.SetFloat("speed", speed);
        }
    }
}