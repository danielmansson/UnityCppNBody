using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

public class UpdateVelocitySystem : JobComponentSystem
{
	[BurstCompile]
	struct Job : IJobForEachWithEntity<Velocity, Force>
	{
		public float deltaTime;

		public void Execute(Entity entity, int index, ref Velocity velocity, [ReadOnly] ref Force force)
		{
			velocity.Value += force.Value * deltaTime;
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

