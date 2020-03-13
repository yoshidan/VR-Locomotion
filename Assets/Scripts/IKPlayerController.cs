/*
using RootMotion.FinalIK;
using UnityEngine;

namespace App
{
    public class IKPlayerController : PlayerController
    {
        private VRIK _vrik;

        public GameObject saverPrefab;

        public override void Start()
        {
            base.Start();
            
            _vrik = gameObject.AddComponent<VRIK>();
            _vrik.solver.plantFeet = false;
            _vrik.solver.spine.headTarget = Camera.main.transform;
            _vrik.solver.leftArm.target = GameObject.Find("IKLeftHand").transform;
            _vrik.solver.rightArm.target = GameObject.Find("IKRightHand").transform;
            _vrik.solver.locomotion.footDistance = 0.1f;
            _vrik.solver.locomotion.maxVelocity = 0.3f;
            _vrik.solver.leftLeg.swivelOffset = 15f;
            _vrik.solver.rightLeg.swivelOffset = -15f;    
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

        protected override void AfterCorrectWarp()
        {
            _vrik.solver.Reset();
            _vrik.enabled = true;
        }
    }
}
*/