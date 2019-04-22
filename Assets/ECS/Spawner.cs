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
		Entity entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
		var entityManager = World.Active.EntityManager;

		for (int i = 0; i < count; i++)
		{
			var instance = entityManager.Instantiate(entityPrefab);

			entityManager.SetComponentData(instance, new Translation { Value = transform.TransformPoint((Vector3)UnityEngine.Random.insideUnitCircle * 10f) });
			entityManager.AddComponentData(instance, new Velocity { Value = (Vector3)UnityEngine.Random.insideUnitCircle });
			entityManager.AddComponentData(instance, new Force { Value = Vector3.zero });
		}
	}
}
