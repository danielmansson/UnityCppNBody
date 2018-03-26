#include "Bindings.h"
#include "Game.h"

#include <cmath>
#include "b2Math.h"

struct Body
{
	b2Vec2 pos;
	b2Vec2 vel;
	b2Vec2 acc;
};

struct GameState
{
	float adjust;
	int count;
	Body* bodies;
};

static struct GameState* gameState;

void PluginMain(
	uint8_t* memory,
	int32_t memorySize,
	bool isFirstBoot)
{
	void* currentMemory = memory;

	gameState = (GameState*)memory;
	memory += sizeof(GameState);

	//Take the rest of the memory for the balls.
	gameState->bodies = (Body*)memory;
}

void PluginStep(
	float timeStep,
	float x,
	float y,
	int buttons)
{
	for (size_t i = 0; i < gameState->count; i++)
	{
		auto& b1 = gameState->bodies[i];
		for (size_t j = i + 1; j < gameState->count; j++)
		{
			auto& b2 = gameState->bodies[j];
			
			auto vec = b2.pos - b1.pos;
			auto distance = vec.Normalize();
			
			vec *= (5.f / (0.1f + distance * 0.01f)) * gameState->adjust;

			b1.acc += vec;
			b2.acc -= vec;
		}
	}

	if (buttons == 1)
	{
		b2Vec2 pos(x, y);

		gameState->bodies[0].pos = pos;
		gameState->bodies[0].vel.SetZero();

		for (size_t i = 0 ; i < gameState->count; i++)
		{
			auto& b = gameState->bodies[i];

			auto vec = pos - b.pos;
			auto distance = vec.Normalize();

			vec *= (3.f / (0.1f + distance * 0.001f));

			b.acc += vec;
		}
	}

	for (size_t i = 0; i < gameState->count; i++)
	{
		auto& b = gameState->bodies[i];

		if (b.vel.LengthSquared() > 2500.f)
		{
			b.acc -= b.vel;
		}

		b.vel += timeStep * b.acc;
		b.pos += timeStep * b.vel;
		b.acc.SetZero();
	}
}