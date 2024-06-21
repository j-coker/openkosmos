using Unity.Entities;
using UnityEngine;

namespace Prototype.OrbitalPhysics.Authoring
{
    public struct BuildingPrefab : IComponentData
    {
        public Entity PrefabBuilding;
        public Entity PrefabGround;
        public Entity PhysicsSpherePrefab;
    }
    
    public class ProtoBuildingPrefabAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private GameObject _groundPrefab;
        [SerializeField] private GameObject _physicsSpherePrefab;
        
        private class ProtoBuildingPrefabAuthoringBaker : Baker<ProtoBuildingPrefabAuthoring>
        {
            public override void Bake(ProtoBuildingPrefabAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var prefabEntity = GetEntity(authoring._prefab, TransformUsageFlags.Dynamic);
                var groundPrefabEntity = GetEntity(authoring._groundPrefab, TransformUsageFlags.Dynamic);
                var physicsSpherePrefabEntity = GetEntity(authoring._physicsSpherePrefab, TransformUsageFlags.Dynamic);
                AddComponent(entity, new BuildingPrefab()
                {
                    PrefabBuilding = prefabEntity,
                    PrefabGround = groundPrefabEntity,
                    PhysicsSpherePrefab = physicsSpherePrefabEntity
                });
            }
        }
    }
}