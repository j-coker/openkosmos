using Kosmos.Prototype.FuelSystem;
using Prototype.FuelSystem.Components;
using Unity.Collections;
using Unity.Entities;

namespace Prototype.FuelSystem.Systems
{
    public partial class EngineRunningSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EngineTankConnection>();
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            Entities.ForEach((ref Entity entity, ref EngineTankConnection connection, in EngineIsRunningTag isRunningTag) =>
            {
                
                var engineConsumptionKgSec = 0f;
                var fuelAvailableKg = 0f;
                
                for (int i = 0; i < connection.EngineEntities.Length; i++)
                {
                    var engineEntity = connection.EngineEntities[i];
                    
                    var engine = EntityManager.GetComponentObject<SimpleEngine>(engineEntity);
                    
                    engineConsumptionKgSec += engine.FuelConsumptionKgSec * engine.Throttle;
                }
                
                var engineConsumptionKgSecPerTank = engineConsumptionKgSec / connection.TankEntities.Length;
                
                for (int i = 0; i < connection.TankEntities.Length; i++)
                {
                    var tankEntity = connection.TankEntities[i];
                    
                    var tank = EntityManager.GetComponentObject<SimpleTank>(tankEntity);
                    
                    tank.CurrentKgFuel -= engineConsumptionKgSecPerTank * SystemAPI.Time.DeltaTime;
                    
                    fuelAvailableKg += tank.CurrentKgFuel;
                }

                if (fuelAvailableKg <= 0)
                {
                    for (int i = 0; i < connection.EngineEntities.Length; i++)
                    {
                        var engineEntity = connection.EngineEntities[i];
                    
                        var engine = EntityManager.GetComponentObject<SimpleEngine>(engineEntity);
                    
                        engine.Throttle = 0f;
                    }
                    
                    ecb.RemoveComponent<EngineIsRunningTag>(entity);
                }

            })
            .WithoutBurst()
            .Run();
            
            ecb.Playback(EntityManager);
        }
    }
}