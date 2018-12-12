using Unity.Mathematics;
using Unity.Entities;

public struct CelestialBody : IComponentData
{
    public float2 position;
    public float2 velocity;
    public float2 acceleration;
    public float mass;
}
