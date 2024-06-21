using Kosmos.FloatingOrigin;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Prototype.FloatingOrigin.Components
{
    public class FloatingPhysicsObjectAuthoring : MonoBehaviour
    {
        private class FloatingPhysicsObjectAuthoringBaker : Baker<FloatingPhysicsObjectAuthoring>
        {
            public override void Bake(FloatingPhysicsObjectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var floatingPosition = FloatingOriginMath.InitializeLocal(
                    (float3)authoring.transform.position);
                //AddComponent(entity, floatingPosition);
            }
        }
    }
}