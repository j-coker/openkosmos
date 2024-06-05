using System;
using Unity.Entities;
using UnityEngine;

namespace Kosmos.Prototype.FuelSystem
{
    public class SimpleTank : MonoBehaviour
    {
        [Header("Stats")] 
        [SerializeField] private float _maxKgFuel = 19635f;

        [Header("Settings")] 
        [SerializeField] private float _currentKgFuel = 19635f;
        
        [Header("Transforms")]
        [SerializeField] private Transform _emptyTankTransform;
        [SerializeField] private Transform _filledTankTransform;

        private EntityManager _entityManager;
        private Entity _entity;
        
        public Entity Entity => _entity;
        
        public float CurrentKgFuel
        {
            get => _currentKgFuel;
            set => _currentKgFuel = Mathf.Clamp(value, 0f, _maxKgFuel);
        }
        
        public float MaxKgFuel => _maxKgFuel;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _entity = _entityManager.CreateEntity();
            _entityManager.AddComponentObject(_entity, this);
        }

        private void Update()
        {
            var fillScale = _currentKgFuel / _maxKgFuel;
            _filledTankTransform.localScale = new Vector3(1f, fillScale, 1f);
            _emptyTankTransform.localScale = new Vector3(1f, 1f - fillScale, 1f);
        }
    }
}