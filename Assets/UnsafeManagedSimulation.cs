using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class UnsafeManagedSimulation : ISimulation
{
	public struct Body
	{
		public Vector2 pos;
		public Vector2 vel;
		public Vector2 acc;
	}

	private float m_adjust;
	private int m_count;
	private IntPtr m_statePtr;

	public unsafe void Init(int count)
	{
		m_statePtr = Marshal.AllocHGlobal(count * sizeof(Body));
		m_count = count;

		Body* state = (Body*)m_statePtr;

		for (int i = 0; i < m_count; i++)
		{
			state[i] = new Body()
			{
				pos = UnityEngine.Random.insideUnitCircle * 10f,
				vel = UnityEngine.Random.insideUnitCircle,
				acc = Vector2.zero
			};
		}

		m_adjust = 1f / count;
	}

	public void Shutdown()
	{
		Marshal.FreeHGlobal(m_statePtr);
		m_statePtr = IntPtr.Zero;
	}

	public unsafe void Step(float timeStep, Vector2 mousePosition, int buttons)
	{
		Body* state = (Body*)m_statePtr;

		for (int i = 0; i < m_count; i++)
		{
			var b1 = state + i;
			for (int j = i + 1; j < m_count; j++)
			{
				var b2 = state + j;

				var vec = b2->pos - b1->pos;
				var distance = vec.magnitude;
				vec /= distance;

				vec *= (5f / (0.1f + distance * 0.01f)) * m_adjust;

				b1->acc += vec;
				b2->acc -= vec;
			}
		}

		if (buttons == 1)
		{
			state[0].pos = mousePosition;
			state[0].vel = Vector2.zero;

			for (int i = 0; i < m_count; i++)
			{
				var b = state + i;

				var vec = mousePosition - b->pos;
				var distance = vec.magnitude;
				if (distance > float.Epsilon)
				{
					vec /= distance;
				}

				vec *= (3f / (0.1f + distance * 0.001f));

				b->acc += vec;
			}
		}

		for (int i = 0; i < m_count; i++)
		{
			var b = state + i;

			if (b->vel.sqrMagnitude > 2500f)
			{
				b->acc -= b->vel;
			}

			b->vel += timeStep * b->acc;
			b->pos += timeStep * b->vel;
			b->acc = Vector2.zero;
		}
	}

	public unsafe void UpdateViews(List<Transform> views)
	{
		Body* state = (Body*)m_statePtr;

		for (int i = 0; i < m_count; i++)
		{
			views[i].position = state[i].pos;
		}
	}
}