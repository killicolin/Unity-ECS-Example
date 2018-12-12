using Unity.Mathematics;
using Unity.Entities;

public struct Firefly : IComponentData
{
    public float2 position;
    public float radius;
    public float3 color;
}
