using Microsoft.Xna.Framework;
using System;

namespace Aestroid;

public class Enemy
{
    public Vector2 position;
    public Vector2 velocity;
    public float rotation;
    public float rotationSpeed;

    public Enemy(Vector2 spawnPos, Vector2 targetPos, Random rng)
    {
        position = spawnPos;
        
        // Direction from spawn point toward the player
        Vector2 direction = targetPos - position;
        direction.Normalize();

        // Add random drift so they don't all go to the exact same center point
        float drift = (float)(rng.NextDouble() * 0.6 - 0.3); 
        float angle = (float)Math.Atan2(direction.Y, direction.X) + drift;
        
        float speed = rng.Next(120, 250);
        velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);

        // Random rotation speed for the "tumbling" effect
        rotationSpeed = (float)(rng.NextDouble() * 5 - 2.5); 
    }

    public void Update(float deltaTime)
    {
        position += velocity * deltaTime;
        rotation += rotationSpeed * deltaTime;
    }
}