using Unity.Mathematics;
using Unity.Entities;

public struct CelestialBody : IComponentData
{
    public float3 position;
    public float3 velocity;
    public float3 acceleration;
    public float mass;
}
