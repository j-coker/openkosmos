using System;
using Unity.Entities;
using UnityEngine;

namespace Kosmos.Prototype.FuelSystem
{
    public class SimpleEngine : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float _fuelConsumptionKgSec = 140f;
        
        [Header("State")]
        [SerializeField] [Range(0f, 1f)] private float _throttle;
        
        [Header("Settings")]
        [SerializeField] private float _scaleSmoothing = 5f;
        
        [Header("Transforms")]
        [SerializeField] private Transform _plumeTransform;
        
        private EntityManager _entityManager;
        private Entity _entity;
        
        public Entity Entity => _entity;
        
        public float Throttle
        {
            get => _throttle;
            set => _throttle = Mathf.Clamp01(value);
        }
        
        public float FuelConsumptionKgSec => _fuelConsumptionKgSec;
        
        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entity = _entityManager.CreateEntity();
            _entityManager.AddComponentObject(_entity, this);
            
            _plumeTransform.localScale = new Vector3(1f, 0f, 1f);
        }
        
        private void Update()
        {
            var plumeScale = Mathf.Lerp(_plumeTransform.localScale.y, _throttle, Time.deltaTime * _scaleSmoothing);
            _plumeTransform.localScale = new Vector3(1f, plumeScale, 1f);
        }
    }
}