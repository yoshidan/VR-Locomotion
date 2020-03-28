using UnityEngine;

namespace App
{
    public class RayController : MonoBehaviour
    {
        public LineRenderer lineRendererPrefab;
        
        public delegate bool DestinationChangeHandler(Vector3 destination);
        
        public delegate void StopHandler();

        public event DestinationChangeHandler OnDestinationChanged;
        
        public event StopHandler OnStop;
        
        private readonly LineRenderer[] _lineRenderers = new LineRenderer[70];
        
        private bool _pressing = false;
        
        private Vector3 _velocityRef = Vector3.zero;

        void Awake()
        {
            for (int i = 0; i < _lineRenderers.Length; i++)
            {
                _lineRenderers[i] = Instantiate(lineRendererPrefab);
            }
        }
        
        void FixedUpdate()
        {
            if (OVRInput.Get(OVRInput.Button.PrimaryThumbstickUp))
            {
                var startPosition = transform.position;
                var currentStartPosition = startPosition;

                var velocity = GetVelocity(transform.forward);
            
                var hitIndex = -1;
                var reachable = false;
               
                //各線分のstartとend位置をつなぐ
                for (int i = 0; i < _lineRenderers.Length; i++)
                {
                    var currentEndPosition = startPosition + GetMovingPosition(velocity, i);

                    RaycastHit hitInfo;
                    if(Physics.Linecast(currentStartPosition, currentEndPosition, out hitInfo))
                    {
                        _lineRenderers[i].SetPosition(0, currentStartPosition);
                        _lineRenderers[i].SetPosition(1, hitInfo.point);

                        hitIndex = i;
                        reachable = OnDestinationChanged != null && OnDestinationChanged.Invoke(hitInfo.point) ;
                        break;
                    }
                    _lineRenderers[i].SetPosition(0, currentStartPosition);
                    _lineRenderers[i].SetPosition(1, currentEndPosition);
                    currentStartPosition = currentEndPosition;
                }
               
                DisplayTracing(_lineRenderers, hitIndex, reachable);

                _pressing = true;
            }
            else if (_pressing)
            {
                _pressing = false; // OVRInput.GetUpだと取れないケースもあるため変数で制御
                OnStop?.Invoke();
                
                foreach (var render in _lineRenderers)
                {
                    render.gameObject.SetActive(false); 
                }

            }
        }
        
        Vector3 GetVelocity(Vector3 direction)
        {
            var v = 5;
            var axisV = Vector3.zero;
            axisV.x = direction.x;
            axisV.z = direction.z;
            
            // オブジェクトの向いている方向とXZ平面とのなす角
            var theta = Mathf.Acos(Vector3.Dot(direction, axisV) / (direction.magnitude * axisV.magnitude));
           
            // XZ平面においてオブジェクトの向いている方向とZ軸のなす角
            var directionH = new Vector2(direction.x, direction.z);
            var axisH = new Vector2(0, direction.z);
            var fi = Mathf.Acos(Vector2.Dot(directionH, axisH) / (directionH.magnitude * axisH.magnitude));

            _velocityRef.y = v * Mathf.Sin(theta) * Mathf.Sign(direction.y);
            _velocityRef.z = v * Mathf.Cos(theta) * Mathf.Cos(fi) * Mathf.Sign(direction.z);
            _velocityRef.x = v * Mathf.Cos(theta) * Mathf.Sin(fi) * Mathf.Sign(direction.x);
                
            // XZ方向は加速する。
            _velocityRef.z *= 2;
            _velocityRef.x *= 2;

            return _velocityRef;
        }

        Vector3 GetMovingPosition(Vector3 velocity, int lineIndex)
        {
            //移動距離を凝縮する
            var t = lineIndex / 30.0f;
                
            //Y方向は放物運動により落とす、XZは摩擦なしの等速運動
            return new Vector3(velocity.x * t,velocity.y * t - (0.5f * 9.8f * t * t) ,velocity.z * t);
        }

        void DisplayTracing(LineRenderer[] lineRenderers, int hitIndex, bool reachable)
        {
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                if (hitIndex >= 0)
                {
                    //到達可能の場合、到達点までの光線を表示
                    if (reachable)
                    {
                        if (i <= hitIndex)
                        {
                            lineRenderers[i].gameObject.SetActive(true);
                            lineRenderers[i].sharedMaterial.color = Color.blue;
                        }
                        else
                        {
                            lineRenderers[i].gameObject.SetActive(false);

                        }
                    }
                    else
                    {
                        lineRenderers[i].gameObject.SetActive(i <= hitIndex);
                        lineRenderers[i].sharedMaterial.color = Color.red;
                    }
                }
                else
                {
                    lineRenderers[i].gameObject.SetActive(true);
                    lineRenderers[i].sharedMaterial.color = Color.red;
                }
            }
        }
        
    }
}