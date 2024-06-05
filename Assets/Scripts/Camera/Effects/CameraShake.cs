using UnityEngine;
using UnityEngine.Serialization;

namespace Kosmos.Camera.Effects
{
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private Transform _camTransform;
        [SerializeField] private float _shakeAmount = 0.7f;
        [SerializeField] private float _decreaseFactor = 1.0f;
        [SerializeField] private float _newPositionTime = 0.1f;
        [SerializeField] private float _smoothing = 5f;
        [SerializeField] private bool _shakeEnabled = true;

        private Vector3 _originalPos;

        private float _newPositionTimer = 0f;
        private Vector3 _targetPosition;
        
        public void SetShakeEnabled(bool enabled)
        {
            _shakeEnabled = enabled;
        }
        
        private void OnEnable()
        {
            _originalPos = _camTransform.localPosition;
        }

        private void Update()
        {
            if (_shakeEnabled)
            {
                if (_newPositionTimer <= 0f)
                {
                    _newPositionTimer = _newPositionTime;
                    _targetPosition = _originalPos + Random.insideUnitSphere * _shakeAmount;
                }
                else
                {
                    _newPositionTimer -= Time.deltaTime;
                }
                
                _camTransform.position = Vector3.Lerp(_camTransform.position, _targetPosition, Time.deltaTime * _smoothing);
            }
            else
            {
                _camTransform.localPosition = _originalPos;
            }
        }
    }
}