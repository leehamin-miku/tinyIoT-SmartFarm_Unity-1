using UnityEngine;


namespace SaberhagenIndustries.Demo
{
    public class MB_CameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _cameraYaw;
        [SerializeField] private GameObject _cameraPitch;
        [SerializeField] private GameObject _cameraArm;

        [Header("Sensitivity")]
        [SerializeField] private float _yawSensitivity = 300f;
        [SerializeField] private float _pitchSensitivity = 200f;
        [SerializeField] private float _zoomSensitivity = 6f;

        [Header("Clamps")]
        [SerializeField] private float _minZoom = -5f;
        [SerializeField] private float _maxZoom = -60f;


        private float _yawRotation;
        private float _pitchRotation;
        private float _zoomDistance;

        
        private void Start()
        {
            _yawRotation = _cameraYaw.transform.localEulerAngles.y;
            _pitchRotation = _cameraPitch.transform.localEulerAngles.x;
            _zoomDistance = _cameraArm.transform.localPosition.z;
        }


        private void Update()
        {
            if (Input.GetMouseButtonDown(1)) Cursor.visible = false;
            if (Input.GetMouseButtonUp(1)) Cursor.visible = true;

            if (Input.GetMouseButton(1))
            {


                _yawRotation += Input.GetAxisRaw("Mouse X") * _yawSensitivity * Time.deltaTime;
                _pitchRotation -= Input.GetAxisRaw("Mouse Y") * _pitchSensitivity * Time.deltaTime;

                _pitchRotation = Mathf.Clamp(_pitchRotation, 10f, 90f); 

                _cameraYaw.transform.localEulerAngles = new Vector3( 0, _yawRotation, 0 );
                _cameraPitch.transform.localEulerAngles = new Vector3( _pitchRotation, 0, 0 );
            }

            _zoomDistance += Input.GetAxisRaw("Mouse ScrollWheel") * _zoomSensitivity;
            _zoomDistance = Mathf.Clamp(_zoomDistance, _maxZoom, _minZoom);

            _cameraArm.transform.localPosition = new Vector3( 0, 0, _zoomDistance );
        }
    }
}

