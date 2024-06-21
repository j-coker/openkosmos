using Kosmos.Prototype.OrbitalPhysics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Kosmos.FloatingOrigin
{
    [UpdateBefore(typeof(FloatingPositionToWorldPositionUpdateSystem))]
    [UpdateAfter(typeof(OrbitToFloatingPositionUpdateSystem))]
    public partial struct FloatingPositionParentUpdateSystem : ISystem
    {
        [ReadOnly] private ComponentLookup<FloatingPositionData> _floatingPositionLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _floatingPositionLookup = state.GetComponentLookup<FloatingPositionData>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _floatingPositionLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new FloatingPositionParentUpdateJob()
            {
                FloatingPositionLookup = _floatingPositionLookup,
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency.Complete();
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }

    public partial struct FloatingPositionParentUpdateJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<FloatingPositionData> FloatingPositionLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(
                Entity entity,
                [ChunkIndexInQuery] int sortKey,
                ref LocalTransform localTransform,
                in FloatingPositionParent parentData
                )
        {
            // Set floating position
            var parentFloatingPosition = FloatingPositionLookup[parentData.ParentEntity];
            
            var floatingPositionData = FloatingOriginMath.Add(parentFloatingPosition, parentData.LocalPosition);
            Ecb.SetComponent(sortKey, entity, floatingPositionData);
            
            // Add local rotation to parent rotation
            localTransform.Rotation =
                parentData.LocalRotation;  //math.mul(parentData.LocalRotation, parentFloatingPosition.Rotation);
        }
    }
}