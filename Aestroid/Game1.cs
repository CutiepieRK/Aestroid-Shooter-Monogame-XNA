using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Aestroid;

// States to manage the game flow
public enum GameState { Start, Playing, GameOver }

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font; // You need to add a SpriteFont in MGCB!

    // Game Objects
    Sprite player;
    Texture2D dotTexture;
    Texture2D enemyTexture;
    List<Bullet> playerBullets = new List<Bullet>();
    List<Enemy> enemies = new List<Enemy>();

    // Game Logic
    GameState currentState = GameState.Start;
    int score = 0;
    int highScore = 0;
    float shootTimer = 0f;
    float spawnTimer = 2.0f;
    float difficultyTimer = 0f;
    float currentSpawnRate = 2.0f; // Starts at 2 seconds
    Random rng = new Random();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        Window.Title = ("Aestroid Shooter");
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 800;
        _graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // --- LOAD YOUR ASSETS HERE ---
        player = new Sprite(Content.Load<Texture2D>("SPACE SHIP"), new Vector2(400, 400), 4.0f, Color.White, 300f);
        enemyTexture = Content.Load<Texture2D>("ROCKS"); // Use "ENEMY_SPRITE" if fixed
        _font = Content.Load<SpriteFont>("ScoreFont"); // Create a SpriteFont file in MGCB named 'ScoreFont'

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
        player.position = new Vector2(400, 400);
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

        // --- DIFFICULTY SCALING ---
        difficultyTimer += deltaTime;
        // Every 10 seconds, decrease spawn time by 0.2s (minimum 0.4s)
        currentSpawnRate = Math.Max(0.4f, 2.0f - (float)Math.Floor(difficultyTimer / 10f) * 0.2f);

        // --- PLAYER LOGIC ---
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

        // --- ENEMY SPAWNING ---
        spawnTimer -= deltaTime;
        if (spawnTimer <= 0)
        {
            // Spawn randomly around the edges
            Vector2 spawnPos = new Vector2(rng.Next(0, 800), -50);
            enemies.Add(new Enemy(spawnPos));
            spawnTimer = currentSpawnRate; 
        }

        // --- ENEMY UPDATES & COLLISION ---
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            enemies[i].Update(player.position, deltaTime);

            // COLLISION: Enemy vs Player
            if (Vector2.Distance(player.position, enemies[i].position) < 40)
            {
                if (score > highScore) highScore = score;
                currentState = GameState.GameOver;
            }

            // COLLISION: Player Bullet vs Enemy
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

        // --- BULLET MOVEMENT ---
        for (int i = playerBullets.Count - 1; i >= 0; i--)
        {
            playerBullets[i].position += playerBullets[i].velocity * deltaTime;
            if (playerBullets[i].position.X < -50 || playerBullets[i].position.X > 850 || 
                playerBullets[i].position.Y < -50 || playerBullets[i].position.Y > 850)
                playerBullets.RemoveAt(i);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (currentState == GameState.Start)
        {
            _spriteBatch.DrawString(_font, "AESTROID SHOOTER", new Vector2(250, 300), Color.White);
            _spriteBatch.DrawString(_font, "PRESS SPACE TO START", new Vector2(240, 350), Color.Yellow);
        }
        else if (currentState == GameState.Playing || currentState == GameState.GameOver)
        {
            // Draw Player
            _spriteBatch.Draw(player.texture, player.position, null, player.color, player.rotation, player.origin, player.scale, SpriteEffects.None, 0f);

            // Draw Enemies
            foreach (var e in enemies)
            {
                Vector2 eOrigin = new Vector2(enemyTexture.Width / 2, enemyTexture.Height / 2);
                _spriteBatch.Draw(enemyTexture, e.position, null, Color.Red, e.rotation + MathHelper.PiOver2, eOrigin, 4f, SpriteEffects.None, 0f);
            }

            // Draw Bullets
            foreach (var b in playerBullets) 
                _spriteBatch.Draw(dotTexture, b.position, null, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);

            // UI
            _spriteBatch.DrawString(_font, $"SCORE: {score}", new Vector2(20, 20), Color.White);
            _spriteBatch.DrawString(_font, "W: MOVE | LMB: SHOOT | MOUSE: AIM", new Vector2(20, 760), Color.Gray);

            if (currentState == GameState.GameOver)
            {
                _spriteBatch.DrawString(_font, "GAME OVER", new Vector2(320, 300), Color.Red);
                _spriteBatch.DrawString(_font, $"FINAL SCORE: {score}", new Vector2(300, 340), Color.White);
                _spriteBatch.DrawString(_font, $"HIGH SCORE: {highScore}", new Vector2(300, 370), Color.Gold);
                _spriteBatch.DrawString(_font, "PRESS SPACE TO RESTART", new Vector2(240, 420), Color.Yellow);
            }
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}