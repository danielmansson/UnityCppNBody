using NativeScript;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NativeSimulation : ISimulation
{
	struct Body
	{
		public Vector2 pos;
		public Vector2 vel;
		public Vector2 acc;
	}

	unsafe struct GameState
	{
		public float adjust;
		public int count;
		public Body* bodies;
	}

	public unsafe void Init(int count)
	{
		GameState* state = (GameState*)Bindings.memory.ToPointer();

		state->count = count;
		state->adjust = 1f / count;

		for (int i = 0; i < state->count; i++)
		{
			state->bodies[i] = new Body()
			{
				pos = UnityEngine.Random.insideUnitCircle * 10f,
				vel = UnityEngine.Random.insideUnitCircle,
				acc = Vector2.zero
			};
		}
	}

	public void Step(float timeStep, Vector2 mousePosition, int buttons)
	{
		Bindings.StepSimulation(timeStep, mousePosition, buttons);
	}

	public unsafe void UpdateViews(List<Transform> views)
	{
		GameState* state = (GameState*)Bindings.memory.ToPointer();

		for (int i = 0; i < state->count; i++)
		{
			views[i].position = state->bodies[i].pos;
		}
	}

	public void Shutdown()
	{
	}
}
