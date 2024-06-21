using Unity.Entities;
using Unity.Mathematics;

namespace Kosmos.FloatingOrigin
{
    public struct FloatingPositionParent : IComponentData
    {
        public Entity ParentEntity;
        public double3 LocalPosition;
        public quaternion LocalRotation;
    }
}