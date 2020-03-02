using System;
using UnityEngine;

namespace App
{
    public class StartController : MonoBehaviour
    {
        
        public Vector3 debugDestination = Vector3.zero;

        private PlayerController _player;
        
        void Start()
        {
            _player = FindObjectOfType<PlayerController>();
            FindObjectOfType<OVRCameraRig>().UpdatedAnchors += _player.UpdatedAnchors;
#if !UNITY_EDITOR
            var ray = FindObjectOfType<RayController>();
            ray.OnDestinationChanged += _player.OnDestinationChanged;
            ray.OnStop += _player.OnStop;
#endif
        }

#if UNITY_EDITOR
        void FixedUpdate()
        {
            _player.OnDestinationChanged(debugDestination);
        }
#endif
    }
}