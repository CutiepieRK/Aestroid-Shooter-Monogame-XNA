using Microsoft.Xna.Framework;

namespace Aestroid;

public class Bullet
{
    public Vector2 position;
    public Vector2 velocity;

    public Bullet(Vector2 pos, Vector2 vel)
    {
        this.position = pos;
        this.velocity = vel;
    }
}