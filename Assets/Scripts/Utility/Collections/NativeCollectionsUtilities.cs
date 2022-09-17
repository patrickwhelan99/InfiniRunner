using Unity.Burst;
using Unity.Collections;

namespace Paz.Utility.Collections
{
    public static class NativeCollectionsUtilities
    {
        [BurstCompile]
        public static void CombineNativeArrays<T>(NativeArray<T> ReturnArray, NativeArray<T> Array, NativeArray<T> Array2, Allocator Allocator) where T : struct
        {
            if (ReturnArray.IsCreated)
            {
                ReturnArray.Dispose();
            }
            ReturnArray = new NativeArray<T>(Array.Length + Array2.Length, Allocator);

            for (int i = 0; i < Array.Length; i++)
            {
                ReturnArray[i] = Array[i];
            }

            for (int i = 0; i < Array2.Length; i++)
            {
                ReturnArray[Array.Length - 1 + i] = Array2[i];
            }
        }

        [BurstCompile]
        public static void CombineNativeLists<T>(NativeList<T> ReturnList, NativeList<T> Array, NativeList<T> Array2) where T : unmanaged
        {
            if (ReturnList.IsCreated && ReturnList.Length > 0)
            {
                ReturnList.Clear();
            }

            ReturnList.AddRange(Array);
            ReturnList.AddRange(Array2);
        }
    }
}