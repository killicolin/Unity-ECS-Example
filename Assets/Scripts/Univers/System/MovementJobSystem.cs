using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;



[UpdateAfter(typeof(AttractionJobSystem))]
public class MovementJobSystem : JobComponentSystem
{
    public struct MovementGroup
    {
        public readonly int Length;
        public readonly int GroupIndex;
        public ComponentDataArray<CelestialBody> celestialB;
        public ComponentDataArray<Position> positions;
    }

    [Inject] MovementGroup movementGroup;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Job job = new Job() { movement = movementGroup};
        var finish = job.Schedule( movementGroup.Length,64, inputDeps);
        return finish;
    }

    [BurstCompile]
    //Compute the deplacment of the CelestialBody in Parallele
    private struct Job :IJobParallelFor
    {
        public MovementGroup movement;
        public float camSize;
        public void Execute(int i)
        {
            var celestB = movement.celestialB[i];
            var pos = movement.positions[i];
            //apply the acceleration to the velocity
            celestB.velocity += celestB.acceleration;
            celestB.acceleration = 0;
            // update the new position
            pos.Value += new float3(celestB.velocity,0);
            celestB.position = pos.Value.xy;
            // update the entity data
            movement.celestialB[i] = celestB;
            movement.positions[i] = pos;
        }
    }
}
