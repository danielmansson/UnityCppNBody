using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class Spawner : MonoBehaviour
{
	public GameObject prefab;
	public int count = 100;

	void Start()
	{
		var assetStore = new BlobAssetStore();
		var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, assetStore);
		Entity entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);
		var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

		for (int i = 0; i < count; i++)
		{
			var instance = entityManager.Instantiate(entityPrefab);

			entityManager.SetComponentData(instance, new Translation { Value = transform.TransformPoint((Vector3)UnityEngine.Random.insideUnitCircle * 10f) });
			entityManager.AddComponentData(instance, new Velocity { Value = (Vector3)UnityEngine.Random.insideUnitCircle });
			entityManager.AddComponentData(instance, new Force { Value = Vector3.zero });
		}

		assetStore.Dispose();
	}
}
