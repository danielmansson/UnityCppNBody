using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISimulation
{
	void Init(int count);
	void Step(float timeStep, Vector2 mousePosition, int buttons);
	void UpdateViews(List<Transform> views);
	void Shutdown();
}
