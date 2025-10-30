using UnityEngine;

namespace SaberhagenIndustries.Demo
{
    public class MB_FramerateLimiter : MonoBehaviour
    {
        [SerializeField] private int _targetFPS = 60;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFPS;
        }
    }

}
