using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input; // FIX: Adds Keys, Keyboard, GamePad
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace Aestroid;

public enum GameState { MainMenu, Options, Playing, Paused, GameOver }

// --- INPUT MANAGER (Fixed with proper namespaces) ---
public static class InputManager
{
    private static KeyboardState kState, prevKState;
    private static GamePadState pState, prevPState;

    public static void Update()
    {
        prevKState = kState;
        kState = Keyboard.GetState();
        prevPState = pState;
        pState = GamePad.GetState(PlayerIndex.One);
    }

    public static bool IsKeyPressed(Keys k) => kState.IsKeyDown(k) && prevKState.IsKeyUp(k);
    public static bool IsKeyDown(Keys k) => kState.IsKeyDown(k);
    public static bool IsPadPressed(Buttons b) => pState.IsButtonDown(b) && prevPState.IsButtonUp(b);
    public static bool IsPadDown(Buttons b) => pState.IsButtonDown(b);
}

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

    private Song _bgMusic;
    private SoundEffect _clickSfx, _explosionSfx, _laserSfx, _pickupSfx, _powerUpSfx;
    private Texture2D dotTexture, enemyTexture;
    
    Sprite player;
    List<Bullet> playerBullets = new List<Bullet>();
    List<Enemy> enemies = new List<Enemy>();
    List<DelayedSound> _soundQueue = new List<DelayedSound>();

    GameState currentState = GameState.MainMenu;
    int score = 0, highScore = 0;
    bool highScoreBeaten = false;
    float masterVolume = 1.0f, musicVolume = 0.1f;
    int selectedOption = 0; 
    bool isUsingGamepad = false;

    float shootTimer, spawnTimer, difficultyTimer, currentSpawnRate;
    Random rng = new Random();
    int screenWidth = 800, screenHeight = 800;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
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
        _font = Content.Load<SpriteFont>("ScoreFont");
        player = new Sprite(Content.Load<Texture2D>("SPACE SHIP"), new Vector2(400, 400), 4.0f, Color.White, 300f);
        enemyTexture = Content.Load<Texture2D>("ROCKS");

        _clickSfx = Content.Load<SoundEffect>("click");
        _explosionSfx = Content.Load<SoundEffect>("explosion");
        _laserSfx = Content.Load<SoundEffect>("laserShoot (1)");
        _pickupSfx = Content.Load<SoundEffect>("pickupCoin");
        _powerUpSfx = Content.Load<SoundEffect>("powerUp");
        
        _bgMusic = Content.Load<Song>("White");
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = musicVolume;
        MediaPlayer.Play(_bgMusic);

        dotTexture = new Texture2D(GraphicsDevice, 1, 1);
        dotTexture.SetData(new[] { Color.White });
    }

    private void ResetGame()
    {
        _clickSfx.Play(masterVolume, 0.5f, 0f);
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
        
        // Detect Controller
        if (GamePad.GetState(PlayerIndex.One).IsConnected) isUsingGamepad = true;

        // Sound Processor
        for (int i = _soundQueue.Count - 1; i >= 0; i--) {
            _soundQueue[i].Timer -= deltaTime;
            if (_soundQueue[i].Timer <= 0) {
                _soundQueue[i].Sfx.Play(_soundQueue[i].Volume * masterVolume, _soundQueue[i].Pitch, 0f);
                _soundQueue.RemoveAt(i);
            }
        }

        switch (currentState)
        {
            case GameState.MainMenu: UpdateMainMenu(); break;
            case GameState.Options: UpdateOptions(); break;
            case GameState.Paused: UpdatePause(); break;
            case GameState.Playing: UpdateGameplay(deltaTime); break;
            case GameState.GameOver: if (InputManager.IsKeyPressed(Keys.Space) || InputManager.IsPadPressed(Buttons.Start)) ResetGame(); break;
        }

        base.Update(gameTime);
    }

    private void UpdateMainMenu()
    {
        if (InputManager.IsKeyPressed(Keys.W) || InputManager.IsPadPressed(Buttons.DPadUp)) { selectedOption = 0; _clickSfx.Play(0.4f * masterVolume, 0.5f, 0f); }
        if (InputManager.IsKeyPressed(Keys.S) || InputManager.IsPadPressed(Buttons.DPadDown)) { selectedOption = 1; _clickSfx.Play(0.4f * masterVolume, 0.5f, 0f); }

        if (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsPadPressed(Buttons.A)) {
            _clickSfx.Play(masterVolume, 1f, 0f);
            if (selectedOption == 0) ResetGame();
            else currentState = GameState.Options;
        }
    }

    private void UpdateOptions()
    {
        if (InputManager.IsKeyPressed(Keys.W) || InputManager.IsPadPressed(Buttons.DPadUp)) selectedOption = Math.Max(0, selectedOption - 1);
        if (InputManager.IsKeyPressed(Keys.S) || InputManager.IsPadPressed(Buttons.DPadDown)) selectedOption = Math.Min(2, selectedOption + 1);

        if (selectedOption == 0) {
            if (InputManager.IsKeyDown(Keys.D) || InputManager.IsPadDown(Buttons.DPadRight)) masterVolume = MathHelper.Clamp(masterVolume + 0.01f, 0, 1);
            if (InputManager.IsKeyDown(Keys.A) || InputManager.IsPadDown(Buttons.DPadLeft)) masterVolume = MathHelper.Clamp(masterVolume - 0.01f, 0, 1);
        }
        if (selectedOption == 1) {
            if (InputManager.IsKeyDown(Keys.D) || InputManager.IsPadDown(Buttons.DPadRight)) musicVolume = MathHelper.Clamp(musicVolume + 0.01f, 0, 1);
            if (InputManager.IsKeyDown(Keys.A) || InputManager.IsPadDown(Buttons.DPadLeft)) musicVolume = MathHelper.Clamp(musicVolume - 0.01f, 0, 1);
            MediaPlayer.Volume = musicVolume;
        }

        if (InputManager.IsKeyPressed(Keys.Escape) || InputManager.IsPadPressed(Buttons.B) || (selectedOption == 2 && InputManager.IsKeyPressed(Keys.Enter))) {
            _clickSfx.Play(masterVolume, 0.5f, 0f);
            currentState = GameState.MainMenu;
        }
    }

    private void UpdatePause()
    {
        if (InputManager.IsKeyPressed(Keys.Escape) || InputManager.IsPadPressed(Buttons.Start)) currentState = GameState.Playing;
    }

    private void UpdateGameplay(float deltaTime)
    {
        if (InputManager.IsKeyPressed(Keys.Escape) || InputManager.IsPadPressed(Buttons.Start)) { currentState = GameState.Paused; return; }

        difficultyTimer += deltaTime;
        currentSpawnRate = Math.Max(0.35f, 2.0f - (float)Math.Floor(difficultyTimer / 10f) * 0.2f);
        
        GamePadState pad = GamePad.GetState(PlayerIndex.One);
        MouseState mouse = Mouse.GetState();

        // Control Logic
        Vector2 moveDir = Vector2.Zero;
        if (isUsingGamepad) moveDir = pad.ThumbSticks.Left;
        else if (InputManager.IsKeyDown(Keys.W)) {
            float rot = player.rotation - MathHelper.PiOver2;
            moveDir = new Vector2((float)Math.Cos(rot), (float)Math.Sin(rot));
        }
        player.position += moveDir * player.speed * deltaTime;

        if (isUsingGamepad && pad.ThumbSticks.Right.Length() > 0.2f)
            player.rotation = (float)Math.Atan2(pad.ThumbSticks.Right.X, -pad.ThumbSticks.Right.Y);
        else if (!isUsingGamepad) {
            Vector2 dir = new Vector2(mouse.X, mouse.Y) - player.position;
            player.rotation = (float)Math.Atan2(dir.Y, dir.X) + MathHelper.PiOver2;
        }

        shootTimer -= deltaTime;
        if ((mouse.LeftButton == ButtonState.Pressed || pad.Triggers.Right > 0.5f) && shootTimer <= 0) {
            _laserSfx.Play(0.1f * masterVolume, 0.2f, 0f);
            float bRot = player.rotation - MathHelper.PiOver2;
            playerBullets.Add(new Bullet(player.position, new Vector2((float)Math.Cos(bRot), (float)Math.Sin(bRot)) * 750f));
            shootTimer = 0.18f;
        }

        // Enemies
        spawnTimer -= deltaTime;
        if (spawnTimer <= 0) {
            enemies.Add(new Enemy(new Vector2(rng.Next(screenWidth), -50), player.position, rng));
            spawnTimer = currentSpawnRate;
        }

        for (int i = enemies.Count - 1; i >= 0; i--) {
            enemies[i].Update(deltaTime);
            if (Vector2.Distance(player.position, enemies[i].position) < 42) {
                _explosionSfx.Play(1.0f * masterVolume, -0.4f, 0f);
                if (score > highScore) highScore = score;
                currentState = GameState.GameOver;
                if (highScoreBeaten) _powerUpSfx.Play(1.0f * masterVolume, 0.5f, 0f);
            }
            for (int j = playerBullets.Count - 1; j >= 0; j--) {
                if (Vector2.Distance(playerBullets[j].position, enemies[i].position) < 35) {
                    _explosionSfx.Play(0.3f * masterVolume, 0f, 0f);
                    enemies.RemoveAt(i); playerBullets.RemoveAt(j); score += 100;
                    if (score > highScore && highScore > 0 && !highScoreBeaten) {
                        highScoreBeaten = true;
                        _powerUpSfx.Play(1.0f * masterVolume, 0.8f, 0f);
                    }
                    break;
                }
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        Color flash = (gameTime.TotalGameTime.Milliseconds % 400 < 200) ? Color.Yellow : Color.Red;

        if (currentState == GameState.MainMenu) {
            DrawCenteredText("AESTROID SHOOTER", 250, Color.White);
            DrawCenteredText("START GAME", 400, selectedOption == 0 ? Color.Yellow : Color.Gray);
            DrawCenteredText("OPTIONS", 460, selectedOption == 1 ? Color.Yellow : Color.Gray);
        }
        else if (currentState == GameState.Options) {
            DrawCenteredText("SETTINGS", 150, Color.White);
            DrawCenteredText($"MASTER VOLUME: {(int)(masterVolume * 100)}%", 300, selectedOption == 0 ? Color.Yellow : Color.White);
            DrawCenteredText($"MUSIC VOLUME: {(int)(musicVolume * 100)}%", 360, selectedOption == 1 ? Color.Yellow : Color.White);
            DrawCenteredText("BACK", 450, selectedOption == 2 ? Color.Yellow : Color.White);
        }
        else if (currentState == GameState.Playing || currentState == GameState.Paused) {
            _spriteBatch.Draw(player.texture, player.position, null, Color.White, player.rotation, player.origin, 4f, SpriteEffects.None, 0f);
            foreach (var e in enemies) _spriteBatch.Draw(enemyTexture, e.position, null, Color.White, e.rotation, new Vector2(25,25), 4f, SpriteEffects.None, 0f);
            foreach (var b in playerBullets) _spriteBatch.Draw(dotTexture, b.position, null, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
            
            _spriteBatch.DrawString(_font, highScoreBeaten ? $"NEW RECORD: {score}" : $"SCORE: {score}", new Vector2(20,20), highScoreBeaten ? flash : Color.White);
            if (currentState == GameState.Paused) DrawCenteredText("PAUSED", 400, Color.White);
        }
        else if (currentState == GameState.GameOver) {
            DrawCenteredText("GAME OVER", 300, Color.Red);
            DrawCenteredText($"SCORE: {score}", 360, Color.White);
            if (highScoreBeaten) DrawCenteredText("!!! NEW HIGH SCORE !!!", 420, flash);
            DrawCenteredText("SPACE / START TO RESTART", 600, Color.Gray);
        }

        _spriteBatch.End();
    }

    private void DrawCenteredText(string text, float y, Color color) {
        Vector2 size = _font.MeasureString(text);
        _spriteBatch.DrawString(_font, text, new Vector2(screenWidth/2 - size.X/2, y), color);
    }
}