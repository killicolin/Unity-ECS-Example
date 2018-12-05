using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;

public class AttractionJobSystem : JobComponentSystem
{
    
    public struct AttractionGroup
    {
        public readonly int Length;
        public readonly int GroupIndex;
        public ComponentDataArray<CelestialBody> celestialB;
    }

    [Inject] AttractionGroup attractionGroup;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //we need this Native array to fix the restricted IJobParallelFor range problem 
        NativeArray<float3> resForce = new NativeArray<float3>(attractionGroup.Length, Allocator.Temp);
        JobComputeAcceleration jobcompute = new JobComputeAcceleration() { attractions = attractionGroup, forceResult = resForce };
        var compute = jobcompute.Schedule(attractionGroup.Length, 64, inputDeps);
        JobCAssignAcceleration jobAssign = new JobCAssignAcceleration() { attractions = attractionGroup, forceResult = resForce };
        var finish = jobAssign.Schedule(attractionGroup.Length, 64, compute);
        finish.Complete();
        //free the NativeArray
        resForce.Dispose();
        return finish;
    }

    [BurstCompile]
    private struct JobComputeAcceleration : IJobParallelFor
    {
        public NativeArray<float3> forceResult;
        [ReadOnly]
        public AttractionGroup attractions;
        //compute the attraction on each Celestial body and affect the result in forceResult
        public void Execute(int i)
        {
            var celestBi = attractions.celestialB[i];
            //the next step is to parallelize this loop !
            for (int j = 0; j < attractions.Length; j++)
            {
                var force = attractionComputation(celestBi, attractions.celestialB[j]);
                force.z = 0;
                forceResult[i] += force;
            }
        }

        // compute the attraction between two CelestialBody 
        private float3 attractionComputation(CelestialBody cb1, CelestialBody cb2)
        {
            //the attraction magnitude
            float magnitude = Mathf.Pow(10, -4) * (6.67f * cb1.mass * cb2.mass) / distancePow2(cb1.position, cb2.position.x);
            //the attraction direction
            float3 direction = cb2.position.x - cb1.position;
            return magnitude * direction;
        }

        // compute the distance² between two positions
        private float distancePow2(float3 pos1, float3 pos2)
        {
            return (Mathf.Pow(pos1.x - pos2.x, 2) + Mathf.Pow(pos1.y - pos2.y, 2) + Mathf.Pow(pos1.z - pos2.z, 2));
        }
    }

    [BurstCompile]
    private struct JobCAssignAcceleration : IJobParallelFor
    {
        public NativeArray<float3> forceResult;
        public AttractionGroup attractions;
        //Assign the Acceleration on each CelestialBody from forceResult
        public void Execute(int i)
        {
            var celestBi = attractions.celestialB[i];
            celestBi.acceleration = forceResult[i]/ celestBi.mass;
            attractions.celestialB[i] = celestBi;
        }
    }
}