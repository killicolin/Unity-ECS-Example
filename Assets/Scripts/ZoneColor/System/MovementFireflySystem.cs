using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;



[UpdateAfter(typeof(AttractionJobSystem))]
public class MovementFireflySystem : JobComponentSystem
{
    public struct MovementGroup
    {
        public readonly int Length;
        public readonly int GroupIndex;
        public ComponentDataArray<Heading> heading;
        public ComponentDataArray<MoveSpeed> speed;
        public ComponentDataArray<Position> position;
        public ComponentDataArray<Firefly> firefly;
    }

    [Inject] MovementGroup movementGroup;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Job job = new Job() { movement = movementGroup };
        var finish = job.Schedule(movementGroup.Length, 64, inputDeps);
        return finish;
    }

    [BurstCompile]
    private struct Job : IJobParallelFor
    {
        public MovementGroup movement;
        public float camSize;
        public void Execute(int i)
        {
            var heading = movement.heading[i];
            var pos = movement.position[i];
            var speed = movement.speed[i];
            var firefly = movement.firefly[i];
            bool outX, outY;
            outX = firefly.position.x > 5f || firefly.position.x < -5f;
            outY = firefly.position.y > 5f || firefly.position.y < -5f;
            if (outX && outY){
                heading.Value = -heading.Value;
            }
            else if (outX) {
                heading.Value = math.reflect(heading.Value, math.normalize(new float3(heading.Value.x, 0, 0)));
            }
            else if (outY) {
                heading.Value = math.reflect(heading.Value, math.normalize(new float3(0,heading.Value.y,0)));
            }
            pos.Value+= heading.Value*speed.speed;
            firefly.position = pos.Value.xy;
            movement.heading[i] = heading;
            movement.position[i] = pos;
            movement.speed[i] = speed;
            movement.firefly[i] = firefly;
        }
    }
}
