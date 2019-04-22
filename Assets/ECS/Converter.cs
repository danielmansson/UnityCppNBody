using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class Converter : MonoBehaviour, IConvertGameObjectToEntity
{
	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
	}
}
