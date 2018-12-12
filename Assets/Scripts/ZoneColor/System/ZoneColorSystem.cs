using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;

//Update the color on each ZoneColor, this color is defined by the nearest Firefly
public class ZoneColorSystem : JobComponentSystem {

    public struct ZoneColorGroup
    {
        public readonly int Length;
        public readonly int GroupIndex;
        public ComponentDataArray<ZoneColor> zoneColor;
    }

    public struct FireflyGroup
    {
        public readonly int Length;
        public readonly int GroupIndex;
        public ComponentDataArray<Firefly> fireflyArray;
    }

    [Inject] ZoneColorGroup zoneColorGroup;
    [Inject] FireflyGroup fireflyGroup;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobDefineColor jobcompute = new JobDefineColor() { zoneColorG = zoneColorGroup, fireflyG = fireflyGroup };
        var compute = jobcompute.Schedule(zoneColorGroup.Length, 64, inputDeps);
        return compute;
    }

    [BurstCompile]
    private struct JobDefineColor : IJobParallelFor
    {
        public ZoneColorGroup zoneColorG;
        [ReadOnly]
        public FireflyGroup fireflyG;
        public void Execute(int i)
        {
            var zoneColori = zoneColorG.zoneColor[i];
            zoneColori.color = new float3(0.8f,0.8f,0.8f);
            float resRadius=float.MaxValue;
            float tmpRadius;
            for (int j = 0; j < fireflyG.Length; j++)
            {
                var firefly = fireflyG.fireflyArray[j];
                tmpRadius = math.distance(firefly.position, zoneColori.position);
                if(tmpRadius<= firefly.radius && tmpRadius < resRadius)
                {
                    resRadius = tmpRadius;
                    zoneColori.color = firefly.color;
                }
            }
            zoneColorG.zoneColor[i] = zoneColori;
        }
    }
}
