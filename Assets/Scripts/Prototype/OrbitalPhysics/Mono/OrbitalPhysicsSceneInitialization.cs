using System;
using System.Collections.Generic;
using System.IO;
using Kosmos.FloatingOrigin;
using Kosmos.Time;
using Prototype.OrbitalPhysics.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kosmos.Prototype.OrbitalPhysics
{
    public class OrbitalPhysicsSceneInitialization : MonoBehaviour
    {
        [SerializeField] private Mesh _sphereMesh;
        [SerializeField] private Material _sphereMaterial;
        [SerializeField] private Material _earthMaterial;
        
        private Dictionary<StarSystemFileBodyEntry, Entity> _orbitalEntities = 
            new Dictionary<StarSystemFileBodyEntry, Entity>();

        private Dictionary<string, StarSystemFileBodyEntry> _idMap = 
            new Dictionary<string, StarSystemFileBodyEntry>();
        
        private async void Start()
        {
            await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
            
            CreateTime();
            
            var starData = await DeserializeStarFile();
            
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            foreach (var body in starData.CelestialBodies)
            {
                // Create the root transform entity
                var rootEntity = entityManager.CreateEntity();
                entityManager.SetName(rootEntity, body.BodyData.FormalName);

                AddFloatingOriginComponents(entityManager, rootEntity, body);

                _orbitalEntities.Add(body, rootEntity);
                _idMap.Add(body.Id, body);

                // Create the entity that holds the body's geometry
                // -- Since each body will have separate mesh data, moving those shared components to
                // a separate entity keeps all the orbit calculation entities (rootEntity) in the same chunk.
                var geometryEntity = entityManager.CreateEntity();
                entityManager.SetName(geometryEntity, $"{body.BodyData.FormalName}_geo");
                
                // Parent the geometry entity to the root entity
                entityManager.AddComponentData(geometryEntity, new Parent()
                {
                    Value = rootEntity
                });
                
                // Create new material with specified color
                ColorUtility.TryParseHtmlString(body.BodyData.ColorCode, out var color);
                Material mat;
                if (body.Id == "proterra")
                {
                    mat = new Material(_earthMaterial);
                }
                else
                {
                    mat = new Material(_sphereMaterial);
                    mat.color = color;
                }
                
                
                // Add the body's geometry to the geometry entity
                OrbitalPhysicsPrototypeUtilities.AddBodyGeometryComponents(
                    entityManager, 
                    geometryEntity, 
                    _sphereMesh, 
                    mat, 
                    body.BodyData.EquatorialRadiusM);

                HandlePointsOfInterest(body);
            }
            
            ResolveBodyUpdateOrder();

            foreach (var orbitalEntity in _orbitalEntities)
            {
                AddCommonBodyComponents(entityManager, orbitalEntity.Value, orbitalEntity.Key);
                
                switch (orbitalEntity.Key.Type)
                {
                    case "star":
                    {
                        break;
                    }
                    case "planet":
                    {
                        AddOrbitalBodyComponents(entityManager, orbitalEntity.Value, orbitalEntity.Key);
                        break;
                    }
                }
            }
        }

        private void ResolveBodyUpdateOrder()
        {
            var bodiesLeftToResolve = _idMap.Count;
            
            // Determine the order in which each body needs to be updated to ensure that its parent body
            // is always updated first
            while (bodiesLeftToResolve > 0)
            {
                foreach (var entry in _idMap)
                {
                    var body = entry.Value;
                    
                    if (body.UpdateOrder != -1)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(body.ParentId))
                    {
                        body.UpdateOrder = 0;
                        bodiesLeftToResolve--;
                        continue;
                    }

                    if (_idMap.TryGetValue(body.ParentId, out var parentBody))
                    {
                        if (parentBody.UpdateOrder != -1)
                        {
                            body.UpdateOrder = parentBody.UpdateOrder + 1;
                            bodiesLeftToResolve--;
                        }
                    }
                }
            }
        }

        private async Awaitable<StarSystemFile> DeserializeStarFile()
        {
            var dirPath = Path.Combine(
                Application.streamingAssetsPath,
                "CelestialBodies"
            );
            
            var starFilePath = Path.Combine(
                dirPath,
                "protopia.star"
            );
            
            var starSystemDeserializer = new StarSystemFileDeserializer();
            var starSystemFile = await starSystemDeserializer.DeserializeStarSystemFile(starFilePath);

            foreach (var body in starSystemFile.CelestialBodies)
            {
                var bodyFilePath = Path.Combine(dirPath, body.BodyFile);
                body.BodyData = await starSystemDeserializer.DeserializeCelestialBodyData(bodyFilePath);
            }

            return starSystemFile;
        }

        private void CreateTime()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new UniversalTime()
            {
                Value = 0.0
            });
            entityManager.AddComponentData(entity, new UniversalTimeModifier()
            {
                Value = 1.0
            });
            entityManager.AddComponentData(entity, new UniversalTimePaused()
            {
                Value = false
            });
            entityManager.AddComponentData(entity, new IsCurrentPlayerTimelineTag());
        }

        private void AddCommonBodyComponents(
            EntityManager entityManager,
            Entity entity,
            StarSystemFileBodyEntry body)
        {
            entityManager.AddComponentData(entity, new Mass()
            {
                ValueKg = body.BodyData.MassKg
            });

            entityManager.AddComponentData(entity, new BodyRadius()
            {
                EquatorialRadiusMeters = body.BodyData.EquatorialRadiusM
            });

            entityManager.AddComponentData(entity, new BodyId()
            {
                Value = body.Id
            });
            
            OrbitalBodyEntityUtilities.AddUpdateOrderTagToEntity(entityManager, entity, body.UpdateOrder);
        }

        private void AddOrbitalBodyComponents(
            EntityManager entityManager,
            Entity entity,
            StarSystemFileBodyEntry body)
        {
            _idMap.TryGetValue(body.ParentId, out var parentData);

            if (parentData == null)
            {
                Debug.LogError($"[OrbitalPhysicsSceneInitialization] Failed to find parent body with id {body.ParentId}");
                return;
            }
            
            _orbitalEntities.TryGetValue(parentData, out var parentEntity);

            if (parentEntity == Entity.Null)
            {
                Debug.LogError($"[OrbitalPhysicsSceneInitialization] Failed to find parent entity with id {body.ParentId}");
                return;
            }
            
            var parentMass = parentData.BodyData.MassKg;
            
            var orbitalPeriod = Kosmos.Math.OrbitMath.ComputeOrbitalPeriodSeconds(
                body.Orbit.SemiMajorAxisM,
                parentMass
            );
            
            entityManager.AddComponentData(entity, new KeplerElements()
            {
                SemiMajorAxisMeters = body.Orbit.SemiMajorAxisM,
                Eccentricity = body.Orbit.Eccentricity,
                EclipticInclinationRadians = math.radians(body.Orbit.InclinationDeg),
                LongitudeOfAscendingNodeRadians = math.radians(body.Orbit.LongitudeAscNodeDeg),
                ArgumentOfPeriapsisRadians = math.radians(body.Orbit.ArgPeriapsisDeg),
                OrbitalPeriodInSeconds = orbitalPeriod
            });

            entityManager.AddComponentData(entity, new MeanAnomaly()
            {
                MeanAnomalyAtEpoch = math.radians(body.Orbit.MeanAnomalyAtEpochDeg)
            });

            entityManager.AddComponentData(entity, new BodyParentData()
            {
                ParentMassKg = parentMass,
                ParentEntity = parentEntity
            });
            
            entityManager.AddComponentData(entity, new ParentFloatingPositionData());
        }
        
        private void AddFloatingOriginComponents(
            EntityManager entityManager, 
            Entity entity,
            StarSystemFileBodyEntry body)
        {
            entityManager.AddComponentData(entity, new LocalToWorld());
            entityManager.AddComponentData(entity, new LocalTransform()
            {
                Scale = 1f
            });
            
            entityManager.AddComponentData(entity, new FloatingPositionData());
        }

        private void HandlePointsOfInterest(StarSystemFileBodyEntry body)
        {
            if (body.PointsOfInterest != null)
            {
                var bodyEntity = _orbitalEntities[body];
                
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                var prefabQuery = entityManager.CreateEntityQuery(typeof(BuildingPrefab));
                
                var prefabEntity = prefabQuery.GetSingletonEntity();
                var buildingPrefab = entityManager.GetComponentData<BuildingPrefab>(prefabEntity).PrefabBuilding;
                var groundPrefab = entityManager.GetComponentData<BuildingPrefab>(prefabEntity).PrefabGround;
                
                var bodyRadius = body.BodyData.EquatorialRadiusM;
                
                for (int i = 0; i < body.PointsOfInterest.Length; i++)
                {
                    var poi = body.PointsOfInterest[i];
                    
                    var lat = math.radians(poi.LatitudeDeg);
                    var lon = math.radians(poi.LongitudeDeg);
                    
                    var x = bodyRadius * math.cos(lat) * math.cos(lon);
                    var y = bodyRadius * math.cos(lat) * math.sin(lon);
                    var z = bodyRadius * math.sin(lat);
                    
                    var pos = new double3(x, y, z);
                    var posF = new float3(pos);
                    
                    // The poi's local up vector should point away from the body's center
                    // Take cross product of position vector and the body's local up vector
                    var away = math.cross(pos, new double3(0, 1, 0));
                    var awayF = new float3(away);
                    
                    var awayRotation = quaternion.LookRotationSafe(awayF, posF);
                    
                    var poiEntity = entityManager.CreateEntity();

                    entityManager.AddComponentData(poiEntity, new LocalToWorld());
                    entityManager.AddComponentData(poiEntity, new LocalTransform()
                    {
                        Scale = 1f
                    });
                    entityManager.AddComponentData(poiEntity, new FloatingPositionData());
                    entityManager.AddComponentData(poiEntity, new FloatingPositionParent()
                    {
                        ParentEntity = bodyEntity,
                        LocalPosition = pos,
                        LocalRotation = awayRotation
                    });
                    entityManager.AddComponentData(poiEntity, new BodyId()
                    {
                        Value = poi.Name
                    });
                    entityManager.AddComponentData(poiEntity, new FloatingScaleData()
                    {
                        Value = 1.0
                    });
                    
                    // Instantiate prefabs for poi
                    var groundEntity = entityManager.Instantiate(groundPrefab);
                    entityManager.AddComponentData(groundEntity, new Parent()
                    {
                        Value = poiEntity
                    });
                    
                    for (int cityX = -3; cityX < 4; cityX++)
                    {
                        for (int cityZ = -3; cityZ < 4; cityZ++)
                        {
                            var buildingEntity = entityManager.Instantiate(buildingPrefab);
                            entityManager.AddComponentData(buildingEntity, new Parent()
                            {
                                Value = poiEntity
                            });
                            entityManager.AddComponentData(buildingEntity, new LocalTransform()
                            {
                                Position = new float3(cityX * 75f, 
                                    0, 
                                    cityZ * 75f),
                                Scale = 1f
                            });
                        }
                    }
                }
            }
        }
    }
}
