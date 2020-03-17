using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public class UpdateForceSystem : JobComponentSystem
{
	NativeArray<float3> positions;
	private EntityQuery bodyGroup;

	[BurstCompile]
	struct Job : IJobForEachWithEntity<Force, Translation, Velocity>
	{
		public float deltaTime;
		public float gravityFactor;
		public int bodyCount;
		[ReadOnly] public NativeArray<float3> positions;

		public void Execute(Entity entity, int index, ref Force force, [ReadOnly] ref Translation translation, [ReadOnly] ref Velocity velocity)
		{
			var position = translation.Value;
			var delta = float3.zero;
			force.Value = float3.zero;

			for (int i = 0; i < bodyCount; i++)
			{
				if (i != index)
				{
					var vec = positions[i] - position;
					var distance = math.length(vec);
					if (distance > 0.001f)
					{
						vec /= distance;

						vec *= (5f / (0.1f + distance * 0.01f)) * gravityFactor;
						delta += vec;
					}
				}
			}

			if (math.lengthsq(velocity.Value) > 2500)
			{
				force.Value -= velocity.Value;
			}

			force.Value += delta;
		}
	}

	[BurstCompile]
	struct CopyPositionsJob : IJobForEachWithEntity<Translation>
	{
		public NativeArray<float3> positions;

		public void Execute(Entity entity, int index, [ReadOnly]ref Translation translation)
		{
			positions[index] = translation.Value;
		}
	}

	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		var bodyCount = bodyGroup.CalculateEntityCount();

		var copyPositionsJob = new CopyPositionsJob()
		{
			positions = positions
		};
		var copyJobHandle = copyPositionsJob.Schedule(this, inputDeps);

		var updateForceJob = new Job()
		{
			deltaTime = UnityEngine.Time.deltaTime,
			gravityFactor = 1f / bodyCount,
			positions = positions,
			bodyCount = bodyCount
		};

		return updateForceJob.Schedule(this, copyJobHandle);
	}

	protected override void OnStopRunning()
	{
		positions.Dispose();
		base.OnStopRunning();
	}


	protected override void OnCreate()
	{
		positions = new NativeArray<float3>(10000, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

		var query = new EntityQueryDesc()
		{
			All = new[] { ComponentType.ReadOnly<Translation>() },
			Options = EntityQueryOptions.FilterWriteGroup
		};

		bodyGroup = GetEntityQuery(query);

		base.OnCreate();
	}
}

