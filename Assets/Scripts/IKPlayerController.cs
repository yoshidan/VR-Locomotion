using RootMotion.FinalIK;
using UnityEngine;
using VRM;

namespace App
{
    public class IKPlayerController : PlayerController
    {
        private VRIK _vrik;

        public GameObject saverPrefab;

        public override void Start()
        {
            base.Start();

            var cameraRig = FindObjectOfType<OVRCameraRig>();
            
            //コントローラ位置を両手のIKに設定すると微妙にずれるのでトラッキングObjectを追加して調整する。
            var leftArmTarget = new GameObject("IKLeftHand");
            leftArmTarget.transform.SetParent(cameraRig.leftControllerAnchor);
            leftArmTarget.transform.localPosition = new Vector3(-0.03f, 0, -0.06f);
            leftArmTarget.transform.localRotation = Quaternion.Euler(-75, 0, 90);
            var rightArmTarget = new GameObject("IKRightHand");
            rightArmTarget.transform.SetParent(cameraRig.rightControllerAnchor);
            rightArmTarget.transform.localPosition = new Vector3(0.03f, 0, -0.06f);
            rightArmTarget.transform.localRotation = Quaternion.Euler(-75, 0, -90);

            // ワープ先がアバターの目線位置なので、カメラ位置をIKのheadTargetにしてしまうとカメラが目線からずれてしまう。
            // そのため、カメラがアバターの目線位置にくるようにトラッキング用Objectを追加する。
            var firstPerson = GetComponent<VRMFirstPerson>();
            var eyeCenter = firstPerson.FirstPersonOffset;
            var headTarget = new GameObject("IKHead");
            headTarget.transform.SetParent(cameraRig.centerEyeAnchor);
            headTarget.transform.localPosition = -eyeCenter;

            _vrik = gameObject.AddComponent<VRIK>();
            _vrik.solver.plantFeet = false;
            _vrik.solver.spine.headTarget = headTarget.transform;
            _vrik.solver.leftArm.target = leftArmTarget.transform;
            _vrik.solver.rightArm.target = rightArmTarget.transform;
            _vrik.solver.locomotion.footDistance = 0.1f;
            _vrik.solver.locomotion.maxVelocity = 0.3f;
            _vrik.solver.leftLeg.swivelOffset = -40f;
            _vrik.solver.rightLeg.swivelOffset = 40f;    
            _vrik.AutoDetectReferences();
            _vrik.enabled = false;
            
            var saber = Instantiate(saverPrefab);
            saber.transform.SetParent(_vrik.references.rightHand);
            saber.transform.localPosition = new Vector3(0.05f, -0.02f, 0);
            saber.transform.localRotation = Quaternion.Euler(90, 25, 0);
        }

        protected override void SyncRealWorldTransform()
        {
            //ignore 
        }

        public override bool OnDestinationChanged(Vector3 newDestination)
        {
            var result = base.OnDestinationChanged(newDestination);
            if (result)
            {
                _vrik.enabled = false;
            }
            return result;
        }

        protected override void AfterWarp()
        {
            _vrik.solver.Reset();
            _vrik.enabled = true;
        }
    }
}
