using System;
using System.Collections.Generic;
using Kosmos.Camera.Effects;
using Prototype.FuelSystem.Components;
using Prototype.FuelSystem.Systems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Kosmos.Prototype.FuelSystem
{
    public class FuelSystemUIController : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float _placementOffset = 7.5f;
        [SerializeField] private float _tankHeightOffset = 11f;
        
        [Header("Buttons")] 
        [SerializeField] private Button _launchButton;
        [SerializeField] private Button _addEnginesButton;
        [SerializeField] private Button _addTanksButton;

        [Header("References")] 
        [SerializeField] private CameraShake _cameraShake;
        [SerializeField] private AudioSource _launchSound;

        [Header("Prefabs")] 
        [SerializeField] private SimpleEngine _enginePrefab;
        [SerializeField] private SimpleTank _tankPrefab;
        
        private List<SimpleEngine> _engines = new List<SimpleEngine>();
        private List<SimpleTank> _tanks = new List<SimpleTank>();

        private int _engineAddRequests = 0;
        private int _tankAddRequests = 0;

        private float _currentEnginePlacementOffset = 0f;
        private float _currentTankPlacementOffset = 0f;
        
        private bool _launchEffectsRunning = false;
        
        private void Awake()
        {
            _launchButton.onClick.AddListener(OnLaunchButtonClicked);
            _addEnginesButton.onClick.AddListener(OnAddEnginesButtonClicked);
            _addTanksButton.onClick.AddListener(OnAddTanksButtonClicked);
        }
        
        private void OnLaunchButtonClicked()
        {
            Debug.Log("Launch!");
            SetConnectionGraph();
            
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var launchEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(launchEntity, new LaunchInputTag());
            
            ToggleLaunchEffects(true);
        }
        
        private void OnAddEnginesButtonClicked()
        {
            Debug.Log("Adding engines...");
            
            _engineAddRequests++;

            if (_engineAddRequests == 1)
            {
                // Only add one engine in center the first time
                var engine = Instantiate(_enginePrefab, Vector3.zero, Quaternion.identity);
                _engines.Add(engine);
            }
            else
            {
                // Add six engines in a circle around the center according to offset
                for (var i = 0; i < 6; i++)
                {
                    var angle = i * 60f;
                    var offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * _currentEnginePlacementOffset;
                    var engine = Instantiate(_enginePrefab, offset, Quaternion.identity);
                    _engines.Add(engine);
                }
            }
            
            // Increase offset for next request
            _currentEnginePlacementOffset += _placementOffset;
        }
        
        private void OnAddTanksButtonClicked()
        {
            Debug.Log("Adding tanks...");
            
            _tankAddRequests++;
            
            if (_tankAddRequests == 1)
            {
                // Only add one tank in center the first time
                var pos = new Vector3(0f, _tankHeightOffset, 0f);
                var tank = Instantiate(_tankPrefab, pos, Quaternion.identity);
                _tanks.Add(tank);
            }
            else
            {
                // Add six tanks in a circle around the center according to offset
                for (var i = 0; i < 6; i++)
                {
                    var angle = i * 60f;
                    var offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * _currentTankPlacementOffset;
                    offset.y = _tankHeightOffset;
                    var tank = Instantiate(_tankPrefab, offset, Quaternion.identity);
                    _tanks.Add(tank);
                }
            }
            
            // Increase offset for next request
            _currentTankPlacementOffset += _placementOffset;
        }

        private void SetConnectionGraph()
        {
            var engineArray = new NativeArray<Entity>(_engines.Count, Allocator.Persistent);
            var tankArray = new NativeArray<Entity>(_tanks.Count, Allocator.Persistent);
            
            for (var i = 0; i < _engines.Count; i++)
            {
                engineArray[i] = _engines[i].Entity;
            }
            
            for (var i = 0; i < _tanks.Count; i++)
            {
                tankArray[i] = _tanks[i].Entity;
            }

            var engineTankConnection = new EngineTankConnection()
            {
                EngineEntities = engineArray,
                TankEntities = tankArray
            };
            
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var connectionEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(connectionEntity, engineTankConnection);
            entityManager.AddComponentData(connectionEntity, new EngineIsRunningTag());
        }

        public void ToggleLaunchEffects(bool value)
        {
            _launchEffectsRunning = value;
            
            if (value)
            {
                _launchSound.Play();
                _cameraShake.SetShakeEnabled(true);
            }
            else
            {
                _launchSound.Stop();
                _cameraShake.SetShakeEnabled(false);
            }
        }

        private void Update()
        {
            var engineIsRunningQuery = World.DefaultGameObjectInjectionWorld.EntityManager
                .CreateEntityQuery(typeof(EngineIsRunningTag));

            if (engineIsRunningQuery.CalculateEntityCount() == 0 && _launchEffectsRunning)
            { 
                ToggleLaunchEffects(false);
            }
        }
    }
}