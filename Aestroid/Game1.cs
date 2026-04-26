using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace Aestroid;

public enum GameState { MainMenu, Options, Playing, Paused, GameOver, Shop }

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Texture2D dotTexture, enemyTexture;
    private Song _bgMusic;
    private SoundEffect _clickSfx, _explosionSfx, _laserSfx, _powerUpSfx, _pickupSfx;

    // State & UI
    GameState currentState = GameState.MainMenu;
    int selectedIndex = 0;
    Rectangle[] menuButtons = new Rectangle[3];
    Rectangle[] shopButtons = new Rectangle[2];

    // Objects
    Sprite player;
    List<Enemy> enemies = new List<Enemy>();
    List<Bullet> bullets = new List<Bullet>();
    Objective currentObjective;

    // Logic
    int score, highScore;
    bool highScoreBeaten;
    float shootTimer, spawnTimer, difficultyTimer, currentSpawnRate = 2.0f;
    float masterVol = 1f, musicVol = 0.1f;
    Random rng = new Random();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 800;
        _graphics.ApplyChanges();
        currentObjective = Objective.GenerateNew(rng);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("ScoreFont");
        player = new Sprite(Content.Load<Texture2D>("SPACE SHIP"), new Vector2(400, 400), 4f, Color.White, 300f);
        enemyTexture = Content.Load<Texture2D>("ROCKS");
        
        _clickSfx = Content.Load<SoundEffect>("click");
        _explosionSfx = Content.Load<SoundEffect>("explosion");
        _laserSfx = Content.Load<SoundEffect>("laserShoot (1)");
        _powerUpSfx = Content.Load<SoundEffect>("powerUp");
        _pickupSfx = Content.Load<SoundEffect>("pickupCoin");
        
        _bgMusic = Content.Load<Song>("White");
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = musicVol;
        MediaPlayer.Play(_bgMusic);

        dotTexture = new Texture2D(GraphicsDevice, 1, 1);
        dotTexture.SetData(new[] { Color.White });

        // Define Button Areas for Mouse Support
        for (int i = 0; i < 3; i++) menuButtons[i] = new Rectangle(300, 300 + (i * 60), 200, 40);
        for (int i = 0; i < 2; i++) shopButtons[i] = new Rectangle(100 + (i * 350), 300, 250, 100);
    }

    protected override void Update(GameTime gameTime)
    {
        InputManager.Update();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        MouseState ms = Mouse.GetState();
        Point mousePoint = new Point(ms.X, ms.Y);

        switch (currentState)
        {
            case GameState.MainMenu:
                UpdateMenuNavigation(ms, mousePoint, 2);
                if (InputManager.IsKeyPressed(Keys.Enter) || (ms.LeftButton == ButtonState.Pressed && InputManager.OldMouse.LeftButton == ButtonState.Released))
                {
                    _clickSfx.Play(masterVol, 0.5f, 0);
                    if (selectedIndex == 0) currentState = GameState.Shop;
                    if (selectedIndex == 1) currentState = GameState.Options;
                }
                break;

            case GameState.Shop:
                UpdateMenuNavigation(ms, mousePoint, 2, true);
                if (InputManager.IsKeyPressed(Keys.Enter) || (ms.LeftButton == ButtonState.Pressed && InputManager.OldMouse.LeftButton == ButtonState.Released))
                {
                    if (selectedIndex == 0 && SaveData.Money >= 100) { SaveData.Money -= 100; SaveData.FireRateLevel++; _powerUpSfx.Play(masterVol, 0, 0); }
                    if (selectedIndex == 1 && SaveData.Money >= 100) { SaveData.Money -= 100; SaveData.SpeedLevel++; _powerUpSfx.Play(masterVol, 0, 0); }
                }
                if (InputManager.IsKeyPressed(Keys.Space)) { ResetGame(); currentState = GameState.Playing; }
                if (InputManager.IsKeyPressed(Keys.Escape)) currentState = GameState.MainMenu;
                break;

            case GameState.Playing:
                UpdateGameplay(dt, ms);
                break;

            case GameState.Paused:
                if (InputManager.IsKeyPressed(Keys.Escape)) currentState = GameState.Playing;
                break;

            case GameState.GameOver:
                if (InputManager.IsKeyPressed(Keys.Space)) currentState = GameState.Shop;
                break;
        }

        base.Update(gameTime);
    }

    private void UpdateMenuNavigation(MouseState ms, Point mousePt, int max, bool horizontal = false)
    {
        // Keyboard Support
        if (!horizontal) {
            if (InputManager.IsKeyPressed(Keys.W) || InputManager.IsKeyPressed(Keys.Up)) selectedIndex = Math.Max(0, selectedIndex - 1);
            if (InputManager.IsKeyPressed(Keys.S) || InputManager.IsKeyPressed(Keys.Down)) selectedIndex = Math.Min(max - 1, selectedIndex + 1);
        } else {
            if (InputManager.IsKeyPressed(Keys.A) || InputManager.IsKeyPressed(Keys.Left)) selectedIndex = Math.Max(0, selectedIndex - 1);
            if (InputManager.IsKeyPressed(Keys.D) || InputManager.IsKeyPressed(Keys.Right)) selectedIndex = Math.Min(max - 1, selectedIndex + 1);
        }

        // Mouse Hover Support
        Rectangle[] activeRects = (currentState == GameState.Shop) ? shopButtons : menuButtons;
        for (int i = 0; i < max; i++) {
            if (activeRects[i].Contains(mousePt)) selectedIndex = i;
        }
    }

    private void UpdateGameplay(float dt, MouseState ms)
    {
        if (InputManager.IsKeyPressed(Keys.Escape)) currentState = GameState.Paused;

        // Player Stats & Controls
        player.speed = SaveData.GetSpeed();
        Vector2 lookDir = new Vector2(ms.X, ms.Y) - player.position;
        player.rotation = (float)Math.Atan2(lookDir.Y, lookDir.X) + MathHelper.PiOver2;

        if (InputManager.IsKeyDown(Keys.W) || InputManager.IsKeyDown(Keys.Up)) {
            Vector2 fwd = new Vector2((float)Math.Cos(player.rotation - MathHelper.PiOver2), (float)Math.Sin(player.rotation - MathHelper.PiOver2));
            player.position += fwd * player.speed * dt;
        }

        // Shooting
        shootTimer -= dt;
        if (ms.LeftButton == ButtonState.Pressed && shootTimer <= 0) {
            _laserSfx.Play(0.1f * masterVol, 0.2f, 0);
            bullets.Add(new Bullet(player.position, lookDir));
            shootTimer = SaveData.GetFireRate();
        }

        // Enemy Spawning
        spawnTimer -= dt;
        if (spawnTimer <= 0) {
            enemies.Add(new Enemy(new Vector2(rng.Next(800), -50), player.position, rng));
            spawnTimer = currentSpawnRate;
        }

        // Logic Loops
        for (int i = enemies.Count - 1; i >= 0; i--) {
            enemies[i].Update(dt);
            if (Vector2.Distance(player.position, enemies[i].position) < 40) currentState = GameState.GameOver;

            for (int j = bullets.Count - 1; j >= 0; j--) {
                if (Vector2.Distance(bullets[j].position, enemies[i].position) < 35) {
                    _explosionSfx.Play(0.4f * masterVol, 0, 0);
                    _pickupSfx.Play(0.4f * masterVol, 0, 0);
                    if (currentObjective.Description == "Destroy Rocks") currentObjective.Current++;
                    enemies.RemoveAt(i); bullets.RemoveAt(j); score += 100;
                    break;
                }
            }
        }
        
        foreach (var b in bullets) b.position += b.velocity * dt;
        bullets.RemoveAll(b => Vector2.Distance(player.position, b.position) > 1000);

        if (currentObjective.IsComplete) {
            SaveData.Money += currentObjective.Reward;
            currentObjective = Objective.GenerateNew(rng);
            _powerUpSfx.Play(masterVol, 0.5f, 0);
        }
    }

    private void ResetGame() {
        score = 0; highScoreBeaten = false;
        enemies.Clear(); bullets.Clear();
        player.position = new Vector2(400, 400);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        switch (currentState)
        {
            case GameState.MainMenu: DrawMenu(); break;
            case GameState.Shop: DrawShop(); break;
            case GameState.Playing: DrawGameScene(gameTime); break;
            case GameState.Paused: DrawGameScene(gameTime); DrawPauseOverlay(); break;
            case GameState.Options: DrawOptions(); break;
            case GameState.GameOver: DrawGameOver(gameTime); break;
        }

        _spriteBatch.End();
    }

    private void DrawGameScene(GameTime gt)
    {
        // PLAYER & ENEMIES MUST BE DRAWN HERE
        _spriteBatch.Draw(player.texture, player.position, null, Color.White, player.rotation, player.origin, 4f, SpriteEffects.None, 0f);
        foreach (var e in enemies) _spriteBatch.Draw(enemyTexture, e.position, null, Color.White, e.rotation, new Vector2(25, 25), 4f, SpriteEffects.None, 0f);
        foreach (var b in bullets) _spriteBatch.Draw(dotTexture, b.position, null, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0f);
        
        DrawHUD(gt);
    }

    private void DrawHUD(GameTime gt)
    {
        _spriteBatch.DrawString(_font, $"SCRAP: ${SaveData.Money}", new Vector2(20, 20), Color.Gold);
        _spriteBatch.DrawString(_font, $"GOAL: {currentObjective.Description} ({currentObjective.Current}/{currentObjective.Target})", new Vector2(20, 50), Color.Cyan);
        _spriteBatch.DrawString(_font, $"SCORE: {score}", new Vector2(20, 80), Color.White);
    }

    private void DrawShop()
    {
        DrawCenteredText("UPGRADE SHOP", 100, Color.Gold);
        DrawCenteredText($"YOUR SCRAP: ${SaveData.Money}", 160, Color.White);

        for (int i = 0; i < 2; i++) {
            _spriteBatch.Draw(dotTexture, shopButtons[i], selectedIndex == i ? Color.DarkSlateGray : Color.Black);
            string label = i == 0 ? "FIRE RATE" : "ENGINE SPEED";
            string cost = "$100";
            _spriteBatch.DrawString(_font, label, new Vector2(shopButtons[i].X + 20, shopButtons[i].Y + 20), Color.White);
            _spriteBatch.DrawString(_font, cost, new Vector2(shopButtons[i].X + 20, shopButtons[i].Y + 50), Color.Yellow);
        }
        
        DrawCenteredText("PRESS [SPACE] TO START MISSION", 600, Color.LimeGreen);
        DrawCenteredText("[ESC] BACK TO MENU", 650, Color.Gray);
    }

    private void DrawMenu() {
        DrawCenteredText("AESTROID", 200, Color.White);
        string[] labels = { "START MISSION", "SETTINGS" };
        for (int i = 0; i < 2; i++)
            DrawCenteredText(labels[i], 300 + (i * 60), selectedIndex == i ? Color.Yellow : Color.Gray);
    }

    private void DrawPauseOverlay() {
        _spriteBatch.Draw(dotTexture, new Rectangle(0, 0, 800, 800), Color.Black * 0.5f);
        DrawCenteredText("PAUSED", 380, Color.White);
    }

    private void DrawOptions() {
        DrawCenteredText("SETTINGS", 200, Color.White);
        DrawCenteredText($"MASTER VOLUME: {(int)(masterVol * 100)}%", 350, Color.Yellow);
        DrawCenteredText("[LEFT/RIGHT] TO ADJUST | [ESC] BACK", 500, Color.Gray);
    }

    private void DrawGameOver(GameTime gt) {
        DrawCenteredText("GAME OVER", 300, Color.Red);
        DrawCenteredText($"SCORE: {score}", 360, Color.White);
        DrawCenteredText("PRESS [SPACE] TO RETURN TO SHOP", 500, Color.Yellow);
    }

    private void DrawCenteredText(string t, float y, Color c) {
        Vector2 s = _font.MeasureString(t);
        _spriteBatch.DrawString(_font, t, new Vector2(400 - s.X / 2, y), c);
    }
}