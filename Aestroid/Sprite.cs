using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Aestroid;

public class Sprite
{
    public Texture2D texture;
    public Rectangle rect;
    public Color color;

    public Sprite(Texture2D texture, Vector2 position, float scale, Color color)
    {
        this.texture = texture;
        this.color = color;
        
        // Calculate the rectangle using texture size * multiplier
        int width = (int)(texture.Width * scale);
        int height = (int)(texture.Height * scale);
        
        this.rect = new Rectangle((int)position.X, (int)position.Y, width, height);
    }
}