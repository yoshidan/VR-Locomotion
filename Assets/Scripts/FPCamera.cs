using UnityEditor;
using UnityEngine;
using VRM;

namespace App
{
    public class FPCamera
    {
        public readonly Camera camera;
        
        private readonly Transform _firstPersonBone;
        private readonly Vector3 _headOffset;
        private readonly Transform _characterTransform;
        
        private Vector3 _v3Cache = Vector3.zero;

        public FPCamera(Camera camera, VRMFirstPerson firstPerson)
        {
            this.camera = camera;
            _firstPersonBone = firstPerson.FirstPersonBone;
            _headOffset = firstPerson.FirstPersonOffset;
            _headOffset += new Vector3(0, 0.1f, 0);
            _characterTransform = firstPerson.transform;
        }
        
        public void ChangeCameraLayer()
        {
            if ((camera.transform.position - _firstPersonBone.position).magnitude < 0.3)
            {
                SetFirstPersonOnly(); 
            }
            else
            {
                SetThirdPersonOnly();
            }
        }
        
        private void SetFirstPersonOnly()
        {
            camera.cullingMask |= 1 << VRMFirstPerson.FIRSTPERSON_ONLY_LAYER;
            camera.cullingMask &= ~(1 << VRMFirstPerson.THIRDPERSON_ONLY_LAYER);
        }

        private void SetThirdPersonOnly()
        {
            camera.cullingMask |= 1 << VRMFirstPerson.THIRDPERSON_ONLY_LAYER;
            camera.cullingMask &= ~ (1 << VRMFirstPerson.FIRSTPERSON_ONLY_LAYER);
        }
        
        public void Warp()
        {
            //リアル移動時に変化するのはcameraのlocalPositionである。そのため、ワープした時にはcameraが顔位置になるようにCameraRigを動かす必要がある。
            var cameraTransform = camera.transform;
            var cameraRigTransform = cameraTransform.parent.parent.transform;
            var rotation = _characterTransform.rotation;
            
            // オフセットを加算したfirstPersonBone位置
            var targetCameraPosition = GetHeadPosition();
            var targetCameraRotation = GetTargetAxis(rotation);

            //子から見たへの方向ベクトル
            var worldDiffPosition = cameraTransform.position - cameraRigTransform.position;
            //子から見た親への回転
            var worldDiffRotation = Quaternion.Inverse(GetTargetAxis(cameraRigTransform.rotation)) * GetTargetAxis(cameraTransform.rotation);

            //子と親のlocalPositionを変更することなく子の位置がtargetCameraPositionとなるように親の位置を変更する
            //親にワールド位置を設定してから親子差分を引けば(回転なら逆回転すれば）、子が目的位置になる
            cameraRigTransform.position = targetCameraPosition - worldDiffPosition;

            cameraRigTransform.rotation = Quaternion.Inverse(worldDiffRotation) * targetCameraRotation; 

        }

        private Quaternion GetTargetAxis(Quaternion rotation)
        {
            // Y軸のみ対象とする
            _v3Cache.y = rotation.eulerAngles.y;
            return Quaternion.Euler(_v3Cache);
        }
        
        
        public void SyncRealWorldTransform(Animator animator)
        {
            var head = _firstPersonBone;
            var spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            var cameraAngle = camera.transform.rotation;
            var rootRotation = _characterTransform.rotation;

            var eulerCamera = cameraAngle.eulerAngles;
            //y軸回転は全体を動かすので個別パースは動かさない
            //giveupを設けてローカル回転する場合、warp時に回転も同期させる必要があるがVR酔いがきつい。というかうまくいかない。
            var headTarget = Quaternion.Euler(eulerCamera.x, 0, eulerCamera.z);
            //体は頭の20%程度回転
            var spineTarget = Quaternion.Slerp(Quaternion.identity, headTarget, 0.2f);

            //回転角が10度未満だったらposition移動はしない
            // var angle = Quaternion.Angle(headTarget, Quaternion.identity);
            //var shouldPositionChange = angle <= 10 || angle >= 350;

            head.localRotation = headTarget;
            spine.localRotation = spineTarget;

            //高さ以外の位置を変更する ( VRChatのようにうまくできない問題 ）
            //var targetPosition = Camera.main.transform.localPosition;
            //targetPosition.y = CharacterController.transform.position.y;
            //CharacterController.center = new Vector3(targetPosition.x, 0 , targetPosition.z);

            //体全体を滑らかに回転
            var targetAngle = Quaternion.Euler(rootRotation.eulerAngles.x, eulerCamera.y, rootRotation.eulerAngles.z);
            _characterTransform.rotation = targetAngle;

        }

        public Vector3 GetHeadPosition()
        {
            return _firstPersonBone.localToWorldMatrix.MultiplyPoint(_headOffset); 
        }
    }
}