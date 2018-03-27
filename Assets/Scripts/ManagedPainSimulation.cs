using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagedPainSimulation : ISimulation
{
	public class Body
	{
		public Vector2 pos;
		public Vector2 vel;
		public Vector2 acc;
	}

	private Body[] m_state;
	private float m_adjust;

	public void Init(int count)
	{
		m_state = new Body[count];

		for (int i = 0; i < m_state.Length; i++)
		{
			m_state[i] = new Body()
			{
				pos = UnityEngine.Random.insideUnitCircle * 10f,
				vel = UnityEngine.Random.insideUnitCircle,
				acc = Vector2.zero
			};
		}

		m_adjust = 1f / count;
	}

	public void Step(float timeStep, Vector2 mousePosition, int buttons)
	{
		for (int i = 0; i < m_state.Length; i++)
		{
			var b1 = m_state[i];
			for (int j = i + 1; j < m_state.Length; j++)
			{
				var b2 = m_state[j];

				float dx = b2.pos.x - b1.pos.x;
				float dy = b2.pos.y - b1.pos.y;

				float distance = (float)System.Math.Sqrt(dx * dx + dy * dy);

				float factor = (5f / (0.1f + distance * 0.01f)) * m_adjust / distance;

				dx *= factor;
				dy *= factor;

				b1.acc.x += dx;
				b1.acc.y += dy;

				b2.acc.x -= dx;
				b2.acc.y -= dy;
			}
		}

		if (buttons == 1)
		{
			m_state[0].pos = mousePosition;
			m_state[0].vel = Vector2.zero;

			for (int i = 0; i < m_state.Length; i++)
			{
				var b = m_state[i];

				var vec = mousePosition - b.pos;
				var distance = vec.magnitude;
				if (distance > float.Epsilon)
				{
					vec /= distance;
				}

				vec *= (3f / (0.1f + distance * 0.001f));

				b.acc += vec;
			}
		}

		for (int i = 0; i < m_state.Length; i++)
		{
			var b = m_state[i];

			if (b.vel.sqrMagnitude > 2500f)
			{
				b.acc -= b.vel;
			}

			b.vel += timeStep * b.acc;
			b.pos += timeStep * b.vel;
			b.acc = Vector2.zero;
		}
	}

	public void UpdateViews(List<Transform> views)
	{
		for (int i = 0; i < m_state.Length; i++)
		{
			views[i].position = m_state[i].pos;
		}
	}

	public void Shutdown()
	{
	}
}