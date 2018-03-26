using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class SimulationRunner : MonoBehaviour
{
	public List<Transform> Views { get; private set; }

	[SerializeField]
	GameObject m_bodyView;

	ISimulation m_simulation;

	double m_workTime = 0.0;
	double m_simulationTime = 0.0;
	int m_steps = 0;
	bool m_stepSimulation = true;
	int m_count = 0;
	bool m_hideUI = false;
	float m_smoothDeltaTime = 1f;
	Stopwatch m_frameStopwatch = new Stopwatch();
	Stopwatch m_simulationStopwatch = new Stopwatch();
	Camera m_camera;

	void Start()
	{
		m_camera = Camera.main;
		Views = new List<Transform>();
		m_frameStopwatch.Start();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.S))
		{
			m_stepSimulation = !m_stepSimulation;
		}

		if (m_simulation != null)
		{
			m_simulation.UpdateViews(Views);
		}
	}

	private void LateUpdate()
	{
		m_frameStopwatch.Stop();
		m_smoothDeltaTime = Mathf.Lerp(m_smoothDeltaTime, (float)m_frameStopwatch.Elapsed.TotalSeconds, 0.3f);
		m_frameStopwatch.Reset();
		m_frameStopwatch.Start();
	}

	enum SimulationType
	{
		Native,
		Managed,
		Unsafe
	}

	void CreateSimulation(SimulationType type, int count)
	{
		if (m_simulation != null)
		{
			m_simulation.Shutdown();
			m_simulation = null;
		}

		if(type == SimulationType.Native)
			m_simulation = new NativeSimulation();
		else if (type == SimulationType.Managed)
			m_simulation = new ManagedSimulation();
		else
			m_simulation = new UnsafeManagedSimulation();

		m_simulation.Init(count);
		RecreateViews(count);
		m_simulation.UpdateViews(Views);

		foreach (var view in Views)
		{
			view.GetComponent<TrailRenderer>().Clear();
		}

		m_workTime = 0.0;
		m_simulationTime = 0.0;
		m_steps = 0;
		m_count = count;
	}

	void RecreateViews(int count)
	{
		foreach (var view in Views)
		{
			Destroy(view.gameObject);
		}
		Views.Clear();

		for (int i = 0; i < count; i++)
		{
			var go = Instantiate(m_bodyView);
			Views.Add(go.transform);
		}
	}

	private void FixedUpdate()
	{
		if (m_simulation != null && m_stepSimulation)
		{
			var ray = m_camera.ScreenPointToRay(Input.mousePosition);
			var wp = ray.origin - ray.direction * (ray.origin.z / ray.direction.z);

			m_simulationStopwatch.Reset();
			m_simulationStopwatch.Start();

			m_simulation.Step(Time.fixedDeltaTime, wp, Input.GetMouseButton(0) ? 1 : 0);

			m_simulationStopwatch.Stop();
			
			m_simulationTime += Time.fixedDeltaTime;
			m_workTime += m_simulationStopwatch.Elapsed.TotalSeconds;
			m_steps++;
		}
	}

	private void OnGUI()
	{
		GUI.matrix = Matrix4x4.Scale(Vector3.one * (Screen.width / 650f));

		using (new GUILayout.HorizontalScope())
		{
			if (GUILayout.Button(m_hideUI ? "Show": "Hide"))
			{
				m_hideUI = !m_hideUI;
			}

			if (m_hideUI)
				return;

			if (GUILayout.Button(m_stepSimulation ? "Stop Simulation" : "Resume simulation"))
			{
				m_stepSimulation = !m_stepSimulation;
			}
		}

		using (new GUILayout.HorizontalScope())
		{
			foreach (var count in new int[] { 10, 100, 200, 400, 800, 1600, 2400 })
			{
				if (GUILayout.Button("Managed " + count))
				{
					CreateSimulation(SimulationType.Managed, count);
				}
			}
		}

		using (new GUILayout.HorizontalScope())
		{
			foreach (var count in new int[] { 10, 100, 200, 400, 800, 1600, 2400 })
			{
				if (GUILayout.Button("Unsafe " + count))
				{
					CreateSimulation(SimulationType.Unsafe, count);
				}
			}
		}

		using (new GUILayout.HorizontalScope())
		{
			foreach (var count in new int[] { 10, 100, 200, 400, 800, 1600, 2400 })
			{
				if (GUILayout.Button("Native " + count))
				{
					CreateSimulation(SimulationType.Native, count);
				}
			}
		}

		GUILayout.Label("");
		GUILayout.Label("== Current ==");
		GUILayout.Label("Num bodies: " + m_count);
		GUILayout.Label("Steps: " + m_steps);
		GUILayout.Label("Avg step time: " + (1000.0 * m_workTime / m_steps).ToString("0.000") + " ms");
		GUILayout.Label("FPS: " + (1f / m_smoothDeltaTime).ToString("0.00"));
	}

	private void OnDestroy()
	{
		if (m_simulation != null)
		{
			m_simulation.Shutdown();
			m_simulation = null;
		}
	}
}