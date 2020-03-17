using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

[UpdateAfter(typeof(UpdateVelocitySystem))]
public class UpdatePositionSystem : JobComponentSystem
{
	[BurstCompile]
	struct Job : IJobForEachWithEntity<Translation, Velocity>
	{
		public float deltaTime;

		public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref Velocity velocity)
		{
			translation.Value += velocity.Value * deltaTime;
		}
	}

	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		var job = new Job()
		{
			deltaTime = UnityEngine.Time.deltaTime,
		};

		return job.Schedule(this, inputDeps);
	}
}

