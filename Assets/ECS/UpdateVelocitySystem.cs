using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class UpdateVelocitySystem : JobComponentSystem
{
	struct Job : IJobProcessComponentDataWithEntity<Velocity, Force>
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
			deltaTime = Time.deltaTime,
		};

		return job.Schedule(this, inputDeps);
	}
}

