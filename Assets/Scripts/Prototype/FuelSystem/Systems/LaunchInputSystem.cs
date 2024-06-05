using Kosmos.Prototype.FuelSystem;
using Prototype.FuelSystem.Components;
using Unity.Collections;
using Unity.Entities;

namespace Prototype.FuelSystem.Systems
{
    public partial class LaunchInputSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<LaunchInputTag>();
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            Entities
                .ForEach((ref Entity entity, in LaunchInputTag launchInputTag) =>
                {
                    
                    ecb.DestroyEntity(entity);
                    
                })
                .Run();

            Entities
                .ForEach((ref Entity entity, in SimpleEngine engine) =>
                {
                    
                    engine.Throttle = 1f;
                    
                })
                .WithoutBurst()
                .Run();
            
            ecb.Playback(EntityManager);
        }
    }
}