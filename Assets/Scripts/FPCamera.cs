using System;
using UnityEngine;
using VRM;

namespace App
{
    public class FPCamera
    {
        private Camera _camera;
        private CapsuleCollider _rigCollider;

        private readonly Transform _firstPersonBone;
        private readonly Vector3 _headOffset;
        private readonly Transform _characterTransform;

        private Vector3 _v3Cache = Vector3.zero;

        private float _prevLocalY = 0.0f;

        public FPCamera(VRMFirstPerson firstPerson)
        {
            _firstPersonBone = firstPerson.FirstPersonBone;
            _headOffset = firstPerson.FirstPersonOffset;
            _headOffset += new Vector3(0, 0.1f, 0);
            _characterTransform = firstPerson.transform;
            firstPerson.Setup();
        }

        public void ChangeCameraLayer()
        {
            if ((_camera.transform.position - _firstPersonBone.position).magnitude < 0.3)
            {
                SetFirstPersonOnly();
            }
            else
            {
                SetThirdPersonOnly();
            }
        }

        void SetFirstPersonOnly()
        {
            _camera.cullingMask |= 1 << VRMFirstPerson.FIRSTPERSON_ONLY_LAYER;
            _camera.cullingMask &= ~(1 << VRMFirstPerson.THIRDPERSON_ONLY_LAYER);
        }

        void SetThirdPersonOnly()
        {
            _camera.cullingMask |= 1 << VRMFirstPerson.THIRDPERSON_ONLY_LAYER;
            _camera.cullingMask &= ~ (1 << VRMFirstPerson.FIRSTPERSON_ONLY_LAYER);
        }

        public void InitializeTrackingSpace(OVRCameraRig rig)
        {
            var cameraTransform = rig.centerEyeAnchor;
            _camera = cameraTransform.GetComponent<Camera>();
            _rigCollider = rig.GetComponent<CapsuleCollider>();

            // リアルで座っていたり立っていたりとアバターの身長と合わないためトラッキングスペースの高さを調節する
            var currentCameraY = cameraTransform.position.y;
            var diff = currentCameraY - GetHeadPosition().y;
            rig.trackingSpace.localPosition = new Vector3(0, -1 * diff, 0);
            
            _prevLocalY = cameraTransform.localPosition.y;
        }

        public void SyncCameraAndRig()
        {
            // カメラRigのColliderの位置をカメラのローカル位置に持ってくる
            // これをやることで6dof移動時にカメラのColliderが移動するため、足場がないところに移動したらRigidbodyと組み合わせてカメラを落とせる
            var cameraLocal = _camera.transform.localPosition;
            _rigCollider.center = new Vector3(cameraLocal.x, 0, cameraLocal.z);
        }

        void AdjustTrackingSpace()
        {
            var cameraTransform = _camera.transform;
            var trackingSpace = cameraTransform.parent;
            var cameraLocalPosition = cameraTransform.localPosition.y;

            float diff = cameraLocalPosition - _prevLocalY;
            _prevLocalY = cameraLocalPosition;

            //トラッキングスペース調整
            trackingSpace.localPosition -= new Vector3(0, diff, 0);
        }

        public void Warp()
        {
            // リアルで座っていたり立っていたりとアバターの身長と合わないためトラッキングスペースの高さを調節する。
            // VRChatではこのようなことはやっていない。ワープ毎に変更させたくなければこの処理を外す。
            AdjustTrackingSpace();

            //リアル移動時に変化するのはcameraのlocalPositionである。
            //そのため、ワープした時にはcameraが顔位置になるようにCameraRigを動かす必要がある。
            var cameraTransform = _camera.transform;
            var trackingSpaceTransform = cameraTransform.parent;
            var cameraRigTransform = trackingSpaceTransform.parent.transform;
            var rotation = _characterTransform.rotation;

            // 目線位置の取得
            var targetCameraPosition = GetHeadPosition();
            // VRChatではワープ時回転同期はしていない。不要なら外す。
            var targetCameraRotation = GetTargetAxis(rotation);

            // 親子差分を計算してカメラが目線位置にくるようにOVRCameraRigの位置を決める
            var worldDiffPosition = cameraTransform.position - cameraRigTransform.position;
            var worldDiffRotation = Quaternion.Inverse(GetTargetAxis(cameraRigTransform.rotation)) *
                                    GetTargetAxis(cameraTransform.rotation);
            var parentTargetPosition = targetCameraPosition - worldDiffPosition;
            
            cameraRigTransform.position = parentTargetPosition;
            cameraRigTransform.rotation = Quaternion.Inverse(worldDiffRotation) * targetCameraRotation;
        }

        Quaternion GetTargetAxis(Quaternion rotation)
        {
            // Y軸のみ対象とする
            _v3Cache.y = rotation.eulerAngles.y;
            return Quaternion.Euler(_v3Cache);
        }


        // この処理はFinalIK未使用時にリアル動作の回転を合わせるためのもの。
        public void SyncRealWorldTransform(Animator animator)
        {
            var head = _firstPersonBone;
            var spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            var cameraAngle = _camera.transform.rotation;
            var rootRotation = _characterTransform.rotation;

            var eulerCamera = cameraAngle.eulerAngles;
            
            //y軸回転は全体を動かすので個別パースは動かさない
            var headTarget = Quaternion.Euler(eulerCamera.x, 0, eulerCamera.z);
            
            //体は頭の20%程度回転
            var spineTarget = Quaternion.Slerp(Quaternion.identity, headTarget, 0.2f);

            head.localRotation = headTarget;
            spine.localRotation = spineTarget;

            //体全体を滑らかに回転
            var targetAngle = Quaternion.Euler(rootRotation.eulerAngles.x, eulerCamera.y, rootRotation.eulerAngles.z);
            
            _characterTransform.rotation = targetAngle;
        }

        Vector3 GetHeadPosition()
        {
            return _firstPersonBone.localToWorldMatrix.MultiplyPoint(_headOffset);
        }
    }
}