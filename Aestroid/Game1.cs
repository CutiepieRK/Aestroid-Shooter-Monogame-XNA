using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media; // REQUIRED FOR MUSIC
using System;
using System.Collections.Generic;

namespace Aestroid;

public enum GameState { Start, Playing, GameOver }

public class DelayedSound {
    public SoundEffect Sfx;
    public float Timer;
    public float Volume;
    public float Pitch;
}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;

    // Music and Sounds
    private Song _bgMusic;
    private SoundEffect _clickSfx, _explosionSfx, _laserSfx, _pickupSfx, _powerUpSfx;
    private List<DelayedSound> _soundQueue = new List<DelayedSound>();

    Sprite player;
    Texture2D dotTexture, enemyTexture;
    List<Bullet> playerBullets = new List<Bullet>();
    List<Enemy> enemies = new List<Enemy>();

    GameState currentState = GameState.Start;
    int score = 0, highScore = 0;
    bool highScoreBeaten = false;
    float shootTimer = 0f, spawnTimer = 2.0f, difficultyTimer = 0f, currentSpawnRate = 2.0f;
    Random rng = new Random();
    int screenWidth = 800, screenHeight = 800;

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

        // Load Sounds
        _clickSfx = Content.Load<SoundEffect>("click");
        _explosionSfx = Content.Load<SoundEffect>("explosion");
        _laserSfx = Content.Load<SoundEffect>("laserShoot (1)");
        _pickupSfx = Content.Load<SoundEffect>("pickupCoin");
        _powerUpSfx = Content.Load<SoundEffect>("powerUp");

        // --- BACKGROUND MUSIC SETUP ---
        _bgMusic = Content.Load<Song>("White");
        MediaPlayer.IsRepeating = true;      // Enable Looping
        MediaPlayer.Volume = 0.1f;           // 10% Volume
        MediaPlayer.Play(_bgMusic);          // Start the music

        dotTexture = new Texture2D(GraphicsDevice, 1, 1);
        dotTexture.SetData(new[] { Color.White });
    }

    private void ResetGame()
    {
        _clickSfx.Play(1.0f, 0.5f, 0f); 
        score = 0;
        highScoreBeaten = false;
        currentSpawnRate = 2.0f;
        difficultyTimer = 0f;
        enemies.Clear();
        playerBullets.Clear();
        _soundQueue.Clear();
        player.position = new Vector2(screenWidth / 2, screenHeight / 2);
        currentState = GameState.Playing;
    }

    protected override void Update(GameTime gameTime)
    {
        InputManager.Update();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = _soundQueue.Count - 1; i >= 0; i--)
        {
            _soundQueue[i].Timer -= deltaTime;
            if (_soundQueue[i].Timer <= 0)
            {
                _soundQueue[i].Sfx.Play(_soundQueue[i].Volume, _soundQueue[i].Pitch, 0f);
                _soundQueue.RemoveAt(i);
            }
        }

        if (currentState != GameState.Playing)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Space)) ResetGame();
            return;
        }

        difficultyTimer += deltaTime;
        currentSpawnRate = Math.Max(0.35f, 2.0f - (float)Math.Floor(difficultyTimer / 10f) * 0.2f);

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
            _laserSfx.Play(0.1f, 0.2f, 0f); 
            playerBullets.Add(new Bullet(player.position, new Vector2((float)Math.Cos(moveAngle), (float)Math.Sin(moveAngle)) * 750f));
            shootTimer = 0.18f;
        }

        spawnTimer -= deltaTime;
        if (spawnTimer <= 0)
        {
            Vector2 spawnPos = Vector2.Zero;
            int side = rng.Next(4);
            if (side == 0) spawnPos = new Vector2(rng.Next(screenWidth), -60);
            else if (side == 1) spawnPos = new Vector2(rng.Next(screenWidth), screenHeight + 60);
            else if (side == 2) spawnPos = new Vector2(-60, rng.Next(screenHeight));
            else spawnPos = new Vector2(screenWidth + 60, rng.Next(screenHeight));

            enemies.Add(new Enemy(spawnPos, player.position, rng));
            spawnTimer = currentSpawnRate;
        }

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            enemies[i].Update(deltaTime);

            if (Vector2.Distance(player.position, enemies[i].position) < 42)
            {
                _explosionSfx.Play(0.8f, -0.4f, 0f); 
                if (score > highScore) highScore = score;
                currentState = GameState.GameOver;
                
                if (highScoreBeaten) {
                    _soundQueue.Add(new DelayedSound { Sfx = _powerUpSfx, Timer = 0.5f, Volume = 1.0f, Pitch = 0.8f });
                }
            }

            for (int j = playerBullets.Count - 1; j >= 0; j--)
            {
                if (Vector2.Distance(playerBullets[j].position, enemies[i].position) < 35)
                {
                    _explosionSfx.Play(0.3f, (float)rng.NextDouble() - 0.2f, 0f);
                    _soundQueue.Add(new DelayedSound { Sfx = _pickupSfx, Timer = 0.1f, Volume = 0.5f, Pitch = 0.3f });

                    enemies.RemoveAt(i);
                    playerBullets.RemoveAt(j);
                    score += 100;

                    if (score > highScore && highScore > 0 && !highScoreBeaten)
                    {
                        _soundQueue.Add(new DelayedSound { Sfx = _powerUpSfx, Timer = 0.4f, Volume = 1.0f, Pitch = 0.8f });
                        highScoreBeaten = true;
                    }
                    break;
                }
            }
        }

        for (int i = playerBullets.Count - 1; i >= 0; i--)
        {
            playerBullets[i].position += playerBullets[i].velocity * deltaTime;
            if (playerBullets[i].position.X < -100 || playerBullets[i].position.X > 900 || playerBullets[i].position.Y < -100 || playerBullets[i].position.Y > 900)
                playerBullets.RemoveAt(i);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Vector2 mid = new Vector2(screenWidth / 2, screenHeight / 2);

        if (currentState == GameState.Start)
        {
            DrawCenteredText("AESTROID SHOOTER", mid.Y - 50, Color.White);
            DrawCenteredText("PRESS SPACE TO START", mid.Y + 20, Color.Yellow);
        }
        else
        {
            _spriteBatch.Draw(player.texture, player.position, null, Color.White, player.rotation, player.origin, 4f, SpriteEffects.None, 0f);
            foreach (var e in enemies)
                _spriteBatch.Draw(enemyTexture, e.position, null, Color.White, e.rotation, new Vector2(enemyTexture.Width / 2, enemyTexture.Height / 2), 4f, SpriteEffects.None, 0f);
            foreach (var b in playerBullets)
                _spriteBatch.Draw(dotTexture, b.position, null, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);

            string scoreLabel = highScoreBeaten ? "NEW RECORD: " : "SCORE: ";
            Color scoreColor = highScoreBeaten ? Color.Gold : Color.White;
            _spriteBatch.DrawString(_font, $"{scoreLabel}{score}", new Vector2(20, 20), scoreColor);

            string hiLabel = $"HI-SCORE: {Math.Max(score, highScore)}";
            Vector2 hiSize = _font.MeasureString(hiLabel);
            _spriteBatch.DrawString(_font, hiLabel, new Vector2(screenWidth - hiSize.X - 20, 20), Color.Gray);

            _spriteBatch.DrawString(_font, "W: MOVE | LMB: SHOOT | MOUSE: AIM", new Vector2(20, 760), Color.Gray);

            if (currentState == GameState.GameOver)
            {
                DrawCenteredText("GAME OVER", mid.Y - 80, Color.Red);
                DrawCenteredText($"FINAL SCORE: {score}", mid.Y - 20, Color.White);
                
                if (highScoreBeaten)
                    DrawCenteredText("NEW HIGH SCORE!", mid.Y + 20, Color.Gold);
                else
                    DrawCenteredText($"HIGH SCORE: {highScore}", mid.Y + 20, Color.Gray);
                
                DrawCenteredText("PRESS SPACE TO RESTART", mid.Y + 80, Color.Yellow);
            }
        }
        _spriteBatch.End();
    }

    private void DrawCenteredText(string text, float y, Color color)
    {
        Vector2 size = _font.MeasureString(text);
        _spriteBatch.DrawString(_font, text, new Vector2(screenWidth / 2 - size.X / 2, y), color);
    }
}