/// <summary>
/// Internals of the bindings between native and .NET code.
/// Game code shouldn't go here.
/// </summary>
/// <author>
/// Jackson Dunstan, 2017, http://JacksonDunstan.com
/// </author>
/// <license>
/// MIT
/// </license>

#include "Bindings.h"
#include "Game.h"
#include <assert.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include "b2Math.h"

// Support placement new
void* operator new(size_t, void* p)
{
	return p;
}

// Macro to put before functions that need to be exposed to C#
#ifdef _WIN32
	#define DLLEXPORT extern "C" __declspec(dllexport)
#else
	#define DLLEXPORT extern "C"
#endif

namespace Plugin
{
	void (*SetException)(int32_t handle);
}


enum class InitMode : uint8_t
{
	FirstBoot,
	Reload
};

extern void PluginMain(
	uint8_t* memory,
	int32_t memorySize,
	bool isFirstBoot);

// Init the plugin
DLLEXPORT void Init(
	uint8_t* memory,
	int32_t memorySize,
	InitMode initMode,
	void(*setException)(int32_t handle))
{
	Plugin::SetException = setException;

	if (initMode == InitMode::FirstBoot)
	{
		memset(memory, 0, memorySize);
	}

	try
	{
		PluginMain(
			memory,
			(int32_t)(memorySize),
			initMode == InitMode::FirstBoot);
	}
	catch (int code)
	{
		Plugin::SetException(code);
	}
	catch (...)
	{
		Plugin::SetException(-1);
	}
}

extern void PluginStep(float timeStep, float x, float y, int buttons);

DLLEXPORT void Step(float timeStep, b2Vec2 mp, int buttons)
{
	try
	{
		PluginStep(
			timeStep,
			mp.x,
			mp.y,
			buttons);
	}
	catch (int code)
	{
		Plugin::SetException(code);
	}
	catch (...)
	{
		Plugin::SetException(-1);
	}
}


