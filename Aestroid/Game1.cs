using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Aestroid;

public enum GameState { Start, Playing, GameOver }

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private SoundEffect _high;
    private SoundEffect _click;
    private SoundEffect _explode;
    private SoundEffect _shoot;
    private SoundEffect _kill;

    Sprite player;
    Texture2D dotTexture;
    Texture2D enemyTexture;
    List<Bullet> playerBullets = new List<Bullet>();
    List<Enemy> enemies = new List<Enemy>();

    GameState currentState = GameState.Start;
    int score = 0;
    int highScore = 0;
    float shootTimer = 0f;
    float spawnTimer = 2.0f;
    float difficultyTimer = 0f;
    float currentSpawnRate = 2.0f;
    Random rng = new Random();

    // Screen dimensions for easy centering
    int screenWidth = 800;
    int screenHeight = 800;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        // FIXED: Property assignment instead of method call
        Window.Title = "Aestroid Shooter";
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = screenWidth;
        _graphics.PreferredBackBufferHeight = screenHeight;
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        player = new Sprite(Content.Load<Texture2D>("SPACE SHIP"), new Vector2(400, 400), 4.0f, Color.White, 300f);
        enemyTexture = Content.Load<Texture2D>("ROCKS"); 
        _font = Content.Load<SpriteFont>("ScoreFont");

        dotTexture = new Texture2D(GraphicsDevice, 1, 1);
        dotTexture.SetData(new[] { Color.White });
    }

    private void ResetGame()
    {
        score = 0;
        currentSpawnRate = 2.0f;
        difficultyTimer = 0f;
        enemies.Clear();
        playerBullets.Clear();
        player.position = new Vector2(screenWidth / 2, screenHeight / 2);
        currentState = GameState.Playing;
    }

    protected override void Update(GameTime gameTime)
    {
        InputManager.Update();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (currentState == GameState.Start || currentState == GameState.GameOver)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Space)) ResetGame();
            return;
        }

        difficultyTimer += deltaTime;
        currentSpawnRate = Math.Max(0.4f, 2.0f - (float)Math.Floor(difficultyTimer / 10f) * 0.2f);

        MouseState mouse = Mouse.GetState();
        Vector2 dirToMouse = new Vector2(mouse.X, mouse.Y) - player.position;
        player.rotation = (float)Math.Atan2(dirToMouse.Y, dirToMouse.X) + MathHelper.PiOver2;
        float moveAngle = player.rotation - MathHelper.PiOver2;

        if (InputManager.IsKeyDown(Keys.W))
        {
            Vector2 forward = new Vector2((float)Math.Cos(moveAngle), (float)Math.Sin(moveAngle));
            player.position += forward * player.speed * deltaTime;
        }

        if (shootTimer > 0) shootTimer -= deltaTime;
        if (mouse.LeftButton == ButtonState.Pressed && shootTimer <= 0)
        {
            Vector2 shotDir = new Vector2((float)Math.Cos(moveAngle), (float)Math.Sin(moveAngle));
            playerBullets.Add(new Bullet(player.position, shotDir * 700f));
            shootTimer = 0.2f;
        }

        spawnTimer -= deltaTime;
        if (spawnTimer <= 0)
        {
            enemies.Add(new Enemy(new Vector2(rng.Next(0, screenWidth), -50)));
            spawnTimer = currentSpawnRate; 
        }

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            enemies[i].Update(player.position, deltaTime);

            if (Vector2.Distance(player.position, enemies[i].position) < 40)
            {
                if (score > highScore) highScore = score;
                currentState = GameState.GameOver;
            }

            for (int j = playerBullets.Count - 1; j >= 0; j--)
            {
                if (Vector2.Distance(playerBullets[j].position, enemies[i].position) < 32)
                {
                    enemies.RemoveAt(i);
                    playerBullets.RemoveAt(j);
                    score += 100;
                    break; 
                }
            }
        }

        for (int i = playerBullets.Count - 1; i >= 0; i--)
        {
            playerBullets[i].position += playerBullets[i].velocity * deltaTime;
            if (playerBullets[i].position.X < -50 || playerBullets[i].position.X > screenWidth + 50 || 
                playerBullets[i].position.Y < -50 || playerBullets[i].position.Y > screenHeight + 50)
                playerBullets.RemoveAt(i);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Calculate screen middle
        Vector2 screenMid = new Vector2(screenWidth / 2, screenHeight / 2);

        if (currentState == GameState.Start)
        {
            // CENTERED TITLE
            string title = "AESTROID SHOOTER";
            Vector2 titleSize = _font.MeasureString(title);
            _spriteBatch.DrawString(_font, title, new Vector2(screenMid.X - titleSize.X / 2, screenMid.Y - 50), Color.White);

            // CENTERED SUBTITLE
            string subTitle = "PRESS SPACE TO START";
            Vector2 subSize = _font.MeasureString(subTitle);
            _spriteBatch.DrawString(_font, subTitle, new Vector2(screenMid.X - subSize.X / 2, screenMid.Y + 20), Color.Yellow);
        }
        else if (currentState == GameState.Playing || currentState == GameState.GameOver)
        {
            _spriteBatch.Draw(player.texture, player.position, null, player.color, player.rotation, player.origin, player.scale, SpriteEffects.None, 0f);

            foreach (var e in enemies)
            {
                Vector2 eOrigin = new Vector2(enemyTexture.Width / 2, enemyTexture.Height / 2);
                _spriteBatch.Draw(enemyTexture, e.position, null, Color.White, e.rotation + MathHelper.PiOver2, eOrigin, 4f, SpriteEffects.None, 0f);
            }

            foreach (var b in playerBullets) 
                _spriteBatch.Draw(dotTexture, b.position, null, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);

            // SCORE AND CONTROLS (Static positions)
            _spriteBatch.DrawString(_font, $"SCORE: {score}", new Vector2(20, 20), Color.White);
            _spriteBatch.DrawString(_font, "W: MOVE | LMB: SHOOT | MOUSE: AIM", new Vector2(20, 760), Color.Gray);

            if (currentState == GameState.GameOver)
            {
                // CENTERED GAME OVER TEXTS
                string goText = "GAME OVER";
                Vector2 goSize = _font.MeasureString(goText);
                _spriteBatch.DrawString(_font, goText, new Vector2(screenMid.X - goSize.X / 2, screenMid.Y - 80), Color.Red);

                string fsText = $"FINAL SCORE: {score}";
                Vector2 fsSize = _font.MeasureString(fsText);
                _spriteBatch.DrawString(_font, fsText, new Vector2(screenMid.X - fsSize.X / 2, screenMid.Y - 20), Color.White);

                string hsText = $"HIGH SCORE: {highScore}";
                Vector2 hsSize = _font.MeasureString(hsText);
                _spriteBatch.DrawString(_font, hsText, new Vector2(screenMid.X - hsSize.X / 2, screenMid.Y + 20), Color.Gold);

                string resText = "PRESS SPACE TO RESTART";
                Vector2 resSize = _font.MeasureString(resText);
                _spriteBatch.DrawString(_font, resText, new Vector2(screenMid.X - resSize.X / 2, screenMid.Y + 80), Color.Yellow);
            }
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}