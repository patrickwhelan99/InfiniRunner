using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class RandomSystem : SystemBase
{
    public static NativeArray<Unity.Mathematics.Random> random;
    const int MAX_THREADS = Unity.Jobs.LowLevel.Unsafe.JobsUtility.MaxJobThreadCount;
    
    protected override void OnCreate()
    {
        random = new NativeArray<Random>(MAX_THREADS, Allocator.Persistent);
        System.Random Seeder = new System.Random();

        for (int i = 0; i < MAX_THREADS; i++)
        {
            random[i] = new Random((uint)Seeder.Next());
        }
    }

    protected override void OnUpdate()
    {
       
    }

    protected override void OnDestroy()
    {
        random.Dispose();
    }
}
