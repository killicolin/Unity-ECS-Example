
using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
//nead unable Unsafe
namespace Unity.Jobs
{
    [JobProducerType(typeof(IJobParallelForForExtensions.ParallelForForJobStruct<>))]
    public interface IJobParallelForFor
    {
        // Notice that this job type has a different signature for its Execute.
        void Execute(int index1, int index2);
    }

    public static class IJobParallelForForExtensions
    {
        internal struct ParallelForForJobStruct<T> where T : struct, IJobParallelForFor
        {
            public static IntPtr jobReflectionData;

            public static IntPtr Initialize()
            {
                // IJobParallelFor uses JobType.ParallelFor instead of Single
                if (jobReflectionData == IntPtr.Zero)
                    jobReflectionData = JobsUtility.CreateJobReflectionData(
                        typeof(T),
                        JobType.ParallelFor,
                        (ExecuteJobFunction)Execute);
                return jobReflectionData;
            }

            // The Execute delegate and function have the same signature as IJob
            public delegate void ExecuteJobFunction(
                ref T data,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);

            public static unsafe void Execute(
                ref T jobData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                // Loop until we're done executing ranges of indices
                while (true)
                {
                    // Get the range of indices to execute
                    // If this returns false, we're done
                    int begin;
                    int end;
                    if (!JobsUtility.GetWorkStealingRange(
                        ref ranges,
                        jobIndex,
                        out begin,
                        out end))
                        break;

                    // Call the job's Execute for each index in the range
                    for (var i = begin; i < end; ++i)
                        for (var j = i+1; j < end; ++j)
                            jobData.Execute(i,j);
                }
            }
        }

        unsafe public static JobHandle Schedule2<T>(
            this T jobData,
            int arrayLength,
            int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                ParallelForForJobStruct<T>.Initialize(),
                dependsOn,
                ScheduleMode.Batched);
            return JobsUtility.ScheduleParallelFor(
                ref scheduleParams,
                arrayLength,
                innerloopBatchCount);
        }

        unsafe public static void Run<T>(this T jobData, int arrayLength)
            where T : struct, IJobParallelForFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                ParallelForForJobStruct<T>.Initialize(),
                new JobHandle(),
                ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(
                ref scheduleParams,
                arrayLength,
                arrayLength);
        }
    }
}