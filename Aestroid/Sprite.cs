using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aestroid;

public class Sprite
{
    public Texture2D texture;
    public Vector2 position;
    public Vector2 origin;
    public float scale;
    public Color color;
    public float rotation;
    public float speed;

    public Sprite(Texture2D texture, Vector2 position, float scale, Color color, float speed)
    {
        this.texture = texture;
        this.position = position;
        this.scale = scale;
        this.color = color;
        this.speed = speed;
        this.origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
    }
}