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
    NativeArray<float2> resForce = new NativeArray<float2>(attractionGroup.Length* attractionGroup.Length, Allocator.Temp);
    JobComputeAcceleration jobcompute = new JobComputeAcceleration() { attractions = attractionGroup, forceResult = resForce};
    var compute = jobcompute.Schedule2(attractionGroup.Length, 64, inputDeps);
    JobCAssignAcceleration jobAssign = new JobCAssignAcceleration() { attractions = attractionGroup, forceResult = resForce };
    var finish = jobAssign.Schedule(attractionGroup.Length, 64, compute);
    finish.Complete();
    //free the NativeArray
    resForce.Dispose();
    return finish;
}

[BurstCompile]
private struct JobComputeAcceleration : IJobParallelForFor
{
    [WriteOnly]
    public NativeArray<float2> forceResult;
    [ReadOnly]
    public AttractionGroup attractions;
    //compute the attraction on each Celestial body and affect the result in forceResult
    public void Execute(int i,int j)
    {
        var celestBi = attractions.celestialB[i];
        //the next step is to parallelize this loop !
        var force = attractionComputation(celestBi, attractions.celestialB[j]);
        forceResult[i* attractions.Length + j] = force;
        forceResult[j* attractions.Length + i] = -force;
    }

    // compute the attraction between two CelestialBody 
    private float2 attractionComputation(CelestialBody cb1, CelestialBody cb2)
    {
            //the attraction direction
            float2 direction = math.normalize(cb2.position - cb1.position);
            //the attraction magnitude
            float magnitude=0;
            bool2 samePosition= cb1.position != cb2.position;
            if(samePosition.x || samePosition.y)
                magnitude = math.pow(10, -4) * (6.67f * cb1.mass * cb2.mass) / distancePow2(cb1.position, cb2.position);
            return magnitude * direction;
    }

    // compute the distance² between two positions
    private float distancePow2(float2 pos1, float2 pos2)
    {
        return (math.pow(pos1.x - pos2.x, 2) + math.pow(pos1.y - pos2.y, 2));
    }
}

[BurstCompile]
private struct JobCAssignAcceleration : IJobParallelFor
{
    public AttractionGroup attractions;
    [ReadOnly]
    public NativeArray<float2> forceResult;
    //Assign the Acceleration on each CelestialBody from forceResult
    public void Execute(int i)
    {
        var celestBi = attractions.celestialB[i];
        float2 tmp =0;
        for (int j = 0; j < attractions.Length; j++)
        {
            tmp += forceResult[i*attractions.Length+j];
        }
        celestBi.acceleration = tmp;
        attractions.celestialB[i] = celestBi;
    }
}
}




/* Same implementation with only IJobParallelFor, less performant !
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
        NativeArray<float3> resForce = new NativeArray<float3>(attractionGroup.Length* attractionGroup.Length, Allocator.Temp);
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
            //the attraction direction
            float3 direction =Vector3.Normalize(cb2.position - cb1.position);
            //the attraction magnitude
            float magnitude=0;
            bool3 samePosition= cb1.position != cb2.position;
            if(samePosition.x || samePosition.y || samePosition.z)
                magnitude = Mathf.Pow(10, -4) * (6.67f * cb1.mass * cb2.mass) / distancePow2(cb1.position, cb2.position);
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
            celestBi.acceleration = forceResult[i];// celestBi.mass;
            attractions.celestialB[i] = celestBi;
        }
    }
}*/
