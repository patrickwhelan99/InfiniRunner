using UnityEngine;
using Unity.Entities;

public class PrefabConverter
{
    private BlobAssetStore BlobStore;
    private GameObjectConversionSettings Settings;

    public static PrefabConverter _instance;
    public static PrefabConverter Instance => _instance ??= new PrefabConverter();

    public static Entity Convert(GameObject Obj)
    {
        Instance.BlobStore ??= new BlobAssetStore();
        Instance.Settings ??= GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, Instance.BlobStore);

        Entity E = GameObjectConversionUtility.ConvertGameObjectHierarchy(Obj, Instance.Settings);

        return E;
    }

    public static void Dispose()
    {
        Instance.BlobStore.Dispose();
    }
}