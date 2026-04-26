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
        
        // Directional vector toward player
        Vector2 direction = targetPos - position;
        direction.Normalize();

        // Add random drift (chaos) to the trajectory
        float drift = (float)(rng.NextDouble() * 0.6 - 0.3); 
        float angle = (float)Math.Atan2(direction.Y, direction.X) + drift;
        
        float speed = rng.Next(130, 260);
        velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);

        // Constant tumbling rotation
        rotationSpeed = (float)(rng.NextDouble() * 5 - 2.5); 
    }

    public void Update(float deltaTime)
    {
        position += velocity * deltaTime;
        rotation += rotationSpeed * deltaTime;
    }
}