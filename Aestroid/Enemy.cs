using Microsoft.Xna.Framework;
using System;

namespace Aestroid;

public class Enemy
{
    public Vector2 position;
    public float rotation;
    public float speed = 150f;

    public Enemy(Vector2 pos)
    {
        position = pos;
    }

    public void Update(Vector2 playerPos, float deltaTime)
    {
        // Look at player
        Vector2 direction = playerPos - position;
        rotation = (float)Math.Atan2(direction.Y, direction.X);

        // Move toward player
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
            position += direction * speed * deltaTime;
        }
    }
}