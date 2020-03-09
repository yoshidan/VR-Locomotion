using UnityEngine;
using UnityEngine.AI;
using VRM;

namespace App
{
    public class PlayerController : MonoBehaviour
    {
        private readonly int _animWait = Animator.StringToHash("Base Layer.WAIT");
        private bool _shouldMove = false;
        private bool _shouldWarp = false;
        private bool _warped = false;
        private bool _ready = false;

        private NavMeshAgent _navMeshAgent;
        private Animator _animator;
        private Mover _mover;
        private FPCamera _firstPersonCamera;

        public virtual void Start()
        {
            _animator = GetComponent<Animator>();
            
            // レイキャストが被って見えなくなってしまうのでignoreレイヤ設定
            gameObject.layer = 2;
            foreach (Transform trans in GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = gameObject.layer;
            }
            
            var firstPerson = GetComponent<VRMFirstPerson>();
            var characterController = GetComponent<CharacterController>();
            
            //自動移動の防止
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.radius = characterController.radius;
            _navMeshAgent.height = characterController.height;

            _mover = new Mover(_navMeshAgent, characterController);
            
            // 一人称カメラ設定
            _firstPersonCamera = new FPCamera(Camera.main, firstPerson);
            firstPerson.Setup();
            
            _firstPersonCamera.ChangeCameraLayer();
            
        }

        public void UpdatedAnchors(OVRCameraRig rig)
        {
#if UNITY_EDITOR
            _ready = true;
#else
            if (!_ready && rig.centerEyeAnchor.localPosition.y > 0)
            {
                var currentCameraY = rig.centerEyeAnchor.position.y;
                var diff = currentCameraY - _firstPersonCamera.GetHeadPosition().y;
                rig.trackingSpace.localPosition -= new Vector3(0, diff, 0);
                _ready = true;
            }
#endif
        }
        
        void FixedUpdate()
        {
            
            if (!_ready)
            {
                return;
            }
            _animator.enabled = true;
            
            if (_shouldMove)
            {
                _mover.Move();
             
            }else {
                
                var state = _animator.GetCurrentAnimatorStateInfo(0);
                if (state.fullPathHash == _animWait)
                {
                    // アニメーション停止後にワープ
                    _animator.enabled = false;
                    if (_shouldWarp)
                    {
                        _shouldWarp = false;
                        _firstPersonCamera.Warp();
                        _warped = true;
                    }
                    else
                    {
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

        public void LateUpdate()
        {
            
            if (_warped)
            {
                _warped = false;
                //ワープ後にもう一回ワープしないと正しい位置にならないため補正する。
                _firstPersonCamera.Warp();
                AfterCorrectWarp();
            }
        }

        protected virtual void AfterCorrectWarp()
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

        private void AfterMove()
        {
            _firstPersonCamera.ChangeCameraLayer();
            var speed = _mover.GetSpeed();
            _animator.SetFloat("speed", speed);
        }
        
        
    }
}