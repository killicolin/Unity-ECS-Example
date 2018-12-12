using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
public struct ZoneColor : IComponentData
{
    public int2 index;
    public float2 position;
    public float3 color;
    public Entity fireflyTarget;
}
