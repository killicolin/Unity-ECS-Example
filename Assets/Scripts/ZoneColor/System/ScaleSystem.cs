using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;

//scale TransformMatrix system
[UpdateAfter(typeof(TransformSystem))]
public class ScaleSystem : JobComponentSystem
{
    struct ScaleData
    {
        public readonly int Length;
        public ComponentDataArray<Scale> Scales;
        public ComponentDataArray<TransformMatrix> Matrices;
    }
    private bool first = true;
    [Inject] private ScaleData _Data;

    [BurstCompile]
    private struct JobScale : IJobParallelFor
    {
        public ScaleData Data;
        public void Execute(int i)
        {
            var baseMat = Data.Matrices[i];
            var scaleMat = math.float4x4(Data.Scales[i].Value.x, 0, 0, 0, 0, Data.Scales[i].Value.y, 0, 0, 0, 0, Data.Scales[i].Value.z, 0, 0, 0, 0, 1);
            var newMat = math.mul(baseMat.Value, scaleMat);
            baseMat.Value = newMat;
            Data.Matrices[i] = baseMat;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobScale jobcompute;
        jobcompute = new JobScale() { Data = _Data };
        JobHandle compute;
        compute = jobcompute.Schedule(_Data.Length, 64, inputDeps);
        return compute;
    }
}