using UnityEngine;

namespace SaberhagenIndustries.Demo
{
    public class MB_LightController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject lightYaw;
        [SerializeField] private GameObject lightPitch;

        [Header("Sensitivity")]
        [SerializeField] private float YawSensitivity = 300f;
        [SerializeField] private float PitchSensitivity = 200f;

        private float yawRotation;
        private float pitchRotation;


        private void Start()
        {
            yawRotation = lightYaw.transform.localEulerAngles.y;
            pitchRotation = lightPitch.transform.localEulerAngles.x;
        }


        private void Update()
        {
            if (Input.GetMouseButtonDown(2)) Cursor.visible = false;
            if (Input.GetMouseButtonUp(2)) Cursor.visible = true;

            if (Input.GetMouseButton(2))
            {
                yawRotation += Input.GetAxisRaw("Mouse X") * YawSensitivity * Time.deltaTime;
                pitchRotation -= Input.GetAxisRaw("Mouse Y") * PitchSensitivity * Time.deltaTime;

                pitchRotation = Mathf.Clamp(pitchRotation, 10f, 80f);

                lightYaw.transform.localEulerAngles = new Vector3(0, yawRotation, 0);
                lightPitch.transform.localEulerAngles = new Vector3(pitchRotation, 0, 0);
            }
        }
    }
}

