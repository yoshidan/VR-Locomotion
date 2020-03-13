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
        private const int IgnoreRayCastLayer = 2;

        private readonly int _animWait = Animator.StringToHash("Base Layer.WAIT");
        private bool _shouldMove = false;
        private bool _shouldWarp = false;
        private bool _warped = false;

        private NavMeshAgent _navMeshAgent;
        private Animator _animator;
        private Mover _mover;
        private FPCamera _firstPersonCamera;
        private CapsuleCollider _rigCollider;
        private Rigidbody _rigid;
        private CharacterController _characterController;
        
        public virtual void Start()
        {
            _animator = GetComponent<Animator>();
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _characterController = GetComponent<CharacterController>();

            IgnoreRayCast();

            //自動移動の防止
            _navMeshAgent.updatePosition = false;
            _navMeshAgent.updateRotation = false;
            //Colliderの余白をほぼ皆無にしないとNavMeshと組み合わせた時に浮く

            _mover = new Mover(_navMeshAgent, _characterController);

            _firstPersonCamera = new FPCamera(Camera.main, GetComponent<VRMFirstPerson>());
            _firstPersonCamera.ChangeCameraLayer();
        }

        private void IgnoreRayCast()
        {
            // レイキャストが被って見えなくなってしまうのでignoreレイヤ設定
            gameObject.layer = IgnoreRayCastLayer;
            foreach (Transform trans in GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = gameObject.layer;
            }
        }

        public void UpdatedAnchors(OVRCameraRig rig)
        {
            if (_rigCollider == null && rig.centerEyeAnchor.localPosition.y > 0)
            {
                var currentCameraY = rig.centerEyeAnchor.position.y;
                var diff = currentCameraY - _firstPersonCamera.GetHeadPosition().y;
                rig.trackingSpace.localPosition -= new Vector3(0, diff, 0);
                
                // モデルの顔位置とカメラが一致するようにトラッキングスペースの高さを調整する。(起動時に座っていても立っていても必ず顔位置にカメラを一致）
                _rigCollider = rig.GetComponent<CapsuleCollider>();
                _warped = true;
                
                Debug.Log(rig.transform.position.y.ToString("F3") + "," + currentCameraY.ToString("F3") + "," +
                          diff.ToString("F3"));
            }
        }
        
        void FixedUpdate()
        {
         
            if (_rigCollider == null)
            {
                return;
            }

            _animator.enabled = true;

            if (_shouldMove)
            {
                _mover.Move();
            }
            else
            {
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
                        // カメラRigのColliderの位置をカメラのローカル位置に持ってくる
                        // これをやることで6dof移動時にカメラのColliderが移動するため、足場がないところに移動したらRigidbodyと組み合わせて重力で落とすことが可能
                        var cameraLocal = _firstPersonCamera.camera.transform.localPosition;
                        _rigCollider.center = new Vector3(cameraLocal.x, 0, cameraLocal.z);

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