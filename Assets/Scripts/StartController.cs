using System;
using System.Collections;
using UnityEngine;

namespace App
{
    public class StartController : MonoBehaviour
    {

        public Vector3 debugDestination = Vector3.zero;

        PlayerController _playerController;

        OVRCameraRig _cameraRig;
        
        void Start()
        {
            _playerController = FindObjectOfType<PlayerController>();
            _cameraRig = FindObjectOfType<OVRCameraRig>();
            
#if UNITY_EDITOR
            _cameraRig.enabled = false;
#endif
            
            OVRManager.TrackingAcquired += () => StartCoroutine(Prepare());

        }
        
        IEnumerator Prepare() {

            //トラッキングの準備が整うまでに時間がかかる。
            Debug.Log("Don't move. Adjusting camera position.....");
            yield return new WaitForSeconds(0.5f);
            
            _cameraRig.UpdatedAnchors += _playerController.UpdatedAnchors;
            
            var ray = FindObjectOfType<RayController>();
            ray.OnDestinationChanged += _playerController.OnDestinationChanged;
            ray.OnStop += _playerController.OnStop;

            Debug.Log("OK");
        }

        void FixedUpdate()
        {
          
#if UNITY_EDITOR
            _playerController.UpdatedAnchors(_cameraRig);

            if (Input.GetKeyDown(KeyCode.A))
            {
                _playerController.OnDestinationChanged(debugDestination);
            }
            else if (Input.GetKeyDown(KeyCode.B))
            {
                _playerController.OnStop();
            }
#endif
        }
    }
}