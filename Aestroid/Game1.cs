using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio; // Required for SoundEffect
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

    // --- SOUND EFFECTS ---
    private SoundEffect _clickSfx;
    private SoundEffect _explosionSfx;
    private SoundEffect _laserSfx;
    private SoundEffect _pickupSfx;
    private SoundEffect _powerUpSfx;

    Sprite player;
    Texture2D dotTexture;
    Texture2D enemyTexture;
    List<Bullet> playerBullets = new List<Bullet>();
    List<Enemy> enemies = new List<Enemy>();

    GameState currentState = GameState.Start;
    int score = 0;
    int highScore = 0;
    bool highScoreBeaten = false; // To play powerUp sound once per game

    float shootTimer = 0f;
    float spawnTimer = 2.0f;
    float difficultyTimer = 0f;
    float currentSpawnRate = 2.0f;
    Random rng = new Random();

    int screenWidth = 800;
    int screenHeight = 800;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
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

        // --- LOAD SOUNDS ---
        _clickSfx = Content.Load<SoundEffect>("click");
        _explosionSfx = Content.Load<SoundEffect>("explosion");
        _laserSfx = Content.Load<SoundEffect>("laserShoot (1)");
        _pickupSfx = Content.Load<SoundEffect>("pickupCoin");
        _powerUpSfx = Content.Load<SoundEffect>("powerUp");

        dotTexture = new Texture2D(GraphicsDevice, 1, 1);
        dotTexture.SetData(new[] { Color.White });
    }

    private void ResetGame()
    {
        _clickSfx.Play(); // Play click when starting/restarting
        score = 0;
        highScoreBeaten = false;
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

        // --- SHOOTING WITH SOUND ---
        if (shootTimer > 0) shootTimer -= deltaTime;
        if (mouse.LeftButton == ButtonState.Pressed && shootTimer <= 0)
        {
            _laserSfx.Play(0.5f, 0f, 0f); // Volume at 50% so it's not too loud
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

            // PLAYER DIES
            if (Vector2.Distance(player.position, enemies[i].position) < 40)
            {
                _explosionSfx.Play(); 
                if (score > highScore) highScore = score;
                currentState = GameState.GameOver;
            }

            // ENEMY DIES (Explosion + Coin)
            for (int j = playerBullets.Count - j >= 0; j--)
            {
                if (Vector2.Distance(playerBullets[j].position, enemies[i].position) < 32)
                {
                    _explosionSfx.Play(0.6f, 0.2f, 0f); // Slightly pitch shifted for variety
                    _pickupSfx.Play(0.6f, 0f, 0f);

                    enemies.RemoveAt(i);
                    playerBullets.RemoveAt(j);
                    score += 100;

                    // CHECK HIGH SCORE BEATEN
                    if (score > highScore && highScore > 0 && !highScoreBeaten)
                    {
                        _powerUpSfx.Play();
                        highScoreBeaten = true;
                    }
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

        Vector2 screenMid = new Vector2(screenWidth / 2, screenHeight / 2);

        if (currentState == GameState.Start)
        {
            string title = "AESTROID SHOOTER";
            Vector2 titleSize = _font.MeasureString(title);
            _spriteBatch.DrawString(_font, title, new Vector2(screenMid.X - titleSize.X / 2, screenMid.Y - 50), Color.White);

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

            _spriteBatch.DrawString(_font, $"SCORE: {score}", new Vector2(20, 20), Color.White);
            _spriteBatch.DrawString(_font, "W: MOVE | LMB: SHOOT | MOUSE: AIM", new Vector2(20, 760), Color.Gray);

            if (currentState == GameState.GameOver)
            {
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