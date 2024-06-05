using Unity.Collections;
using Unity.Entities;

namespace Prototype.FuelSystem.Components
{
    public struct EngineTankConnection : IComponentData
    {
        public NativeArray<Entity> EngineEntities;
        public NativeArray<Entity> TankEntities;
    }
}