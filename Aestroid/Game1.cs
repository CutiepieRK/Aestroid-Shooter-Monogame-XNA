using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace Aestroid;

public enum GameState { MainMenu, Options, Playing, Paused, GameOver, Shop }

// --- PERSISTENT DATA ---
public static class SaveData
{
    public static int Money = 0;
    public static int FireRateLevel = 0;
    public static int SpeedLevel = 0;
    public static float GetFireRate() => Math.Max(0.05f, 0.18f - (FireRateLevel * 0.02f));
    public static float GetSpeed() => 300f + (SpeedLevel * 40f);
}

// --- PROCEDURAL OBJECTIVES ---
public class Objective
{
    public string Description;
    public int Target;
    public int Current;
    public int Reward;
    public bool IsComplete => Current >= Target;

    public static Objective GenerateNew(Random rng)
    {
        string[] types = { "Destroy Rocks", "Survival Time" };
        int type = rng.Next(2);
        int level = (SaveData.FireRateLevel + SaveData.SpeedLevel) + 1;

        if (type == 0) return new Objective { Description = "Destroy Rocks", Target = 5 * level, Reward = 50 * level };
        return new Objective { Description = "Survival Time", Target = 10 * level, Reward = 60 * level };
    }
}

// --- INPUT MANAGER ---
public static class InputManager
{
    public static KeyboardState KState, PrevKState;
    public static MouseState MState, PrevMState;

    public static void Update()
    {
        PrevKState = KState;
        KState = Keyboard.GetState();
        PrevMState = MState;
        MState = Mouse.GetState();
    }

    public static bool IsKeyPressed(Keys k) => KState.IsKeyDown(k) && PrevKState.IsKeyUp(k);
    public static bool IsKeyDown(Keys k) => KState.IsKeyDown(k);
    public static bool IsLeftClick() => MState.LeftButton == ButtonState.Pressed && PrevMState.LeftButton == ButtonState.Released;
}

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Texture2D dotTexture, enemyTexture;
    private Song _bgMusic;
    private SoundEffect _clickSfx, _explosionSfx, _laserSfx, _powerUpSfx, _pickupSfx;

    GameState currentState = GameState.MainMenu;
    int selectedIndex = 0;
    Rectangle[] menuButtons = new Rectangle[2];
    Rectangle[] shopButtons = new Rectangle[2];

    Sprite player;
    List<Enemy> enemies = new List<Enemy>();
    List<Bullet> bullets = new List<Bullet>();
    Objective currentObjective;

    int score;
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

        for (int i = 0; i < 2; i++) menuButtons[i] = new Rectangle(300, 300 + (i * 60), 200, 40);
        for (int i = 0; i < 2; i++) shopButtons[i] = new Rectangle(100 + (i * 350), 300, 250, 100);
    }

    protected override void Update(GameTime gameTime)
    {
        InputManager.Update();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Point mousePt = new Point(InputManager.MState.X, InputManager.MState.Y);

        switch (currentState)
        {
            case GameState.MainMenu:
                UpdateMenuNav(mousePt, 2, false);
                if (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsLeftClick())
                {
                    _clickSfx.Play(masterVol, 0, 0);
                    if (selectedIndex == 0) currentState = GameState.Shop;
                    else currentState = GameState.Options;
                }
                break;

            case GameState.Shop:
                UpdateMenuNav(mousePt, 2, true);
                if (InputManager.IsKeyPressed(Keys.Enter) || InputManager.IsLeftClick())
                {
                    if (selectedIndex == 0 && SaveData.Money >= 100) { SaveData.Money -= 100; SaveData.FireRateLevel++; _powerUpSfx.Play(masterVol, 0, 0); }
                    if (selectedIndex == 1 && SaveData.Money >= 100) { SaveData.Money -= 100; SaveData.SpeedLevel++; _powerUpSfx.Play(masterVol, 0, 0); }
                }
                if (InputManager.IsKeyPressed(Keys.Space)) { ResetGame(); currentState = GameState.Playing; }
                if (InputManager.IsKeyPressed(Keys.Escape)) currentState = GameState.MainMenu;
                break;

            case GameState.Playing:
                UpdateGameplay(dt);
                break;

            case GameState.Paused:
                if (InputManager.IsKeyPressed(Keys.Escape)) currentState = GameState.Playing;
                break;

            case GameState.Options:
                if (InputManager.IsKeyPressed(Keys.Escape)) currentState = GameState.MainMenu;
                if (InputManager.IsKeyDown(Keys.Left)) masterVol = MathHelper.Clamp(masterVol - 0.01f, 0, 1);
                if (InputManager.IsKeyDown(Keys.Right)) masterVol = MathHelper.Clamp(masterVol + 0.01f, 0, 1);
                break;

            case GameState.GameOver:
                if (InputManager.IsKeyPressed(Keys.Space)) currentState = GameState.Shop;
                break;
        }
        base.Update(gameTime);
    }

    private void UpdateMenuNav(Point mousePt, int max, bool horizontal)
    {
        if (!horizontal)
        {
            if (InputManager.IsKeyPressed(Keys.W) || InputManager.IsKeyPressed(Keys.Up)) selectedIndex = Math.Max(0, selectedIndex - 1);
            if (InputManager.IsKeyPressed(Keys.S) || InputManager.IsKeyPressed(Keys.Down)) selectedIndex = Math.Min(max - 1, selectedIndex + 1);
        }
        else
        {
            if (InputManager.IsKeyPressed(Keys.A) || InputManager.IsKeyPressed(Keys.Left)) selectedIndex = Math.Max(0, selectedIndex - 1);
            if (InputManager.IsKeyPressed(Keys.D) || InputManager.IsKeyPressed(Keys.Right)) selectedIndex = Math.Min(max - 1, selectedIndex + 1);
        }

        Rectangle[] rects = (currentState == GameState.Shop) ? shopButtons : menuButtons;
        for (int i = 0; i < max; i++) if (rects[i].Contains(mousePt)) selectedIndex = i;
    }

    private void UpdateGameplay(float dt)
    {
        if (InputManager.IsKeyPressed(Keys.Escape)) currentState = GameState.Paused;

        player.speed = SaveData.GetSpeed();
        Vector2 mousePos = new Vector2(InputManager.MState.X, InputManager.MState.Y);
        Vector2 lookDir = mousePos - player.position;
        player.rotation = (float)Math.Atan2(lookDir.Y, lookDir.X) + MathHelper.PiOver2;

        if (InputManager.IsKeyDown(Keys.W))
        {
            Vector2 fwd = new Vector2((float)Math.Cos(player.rotation - MathHelper.PiOver2), (float)Math.Sin(player.rotation - MathHelper.PiOver2));
            player.position += fwd * player.speed * dt;
        }

        shootTimer -= dt;
        if (InputManager.MState.LeftButton == ButtonState.Pressed && shootTimer <= 0)
        {
            _laserSfx.Play(0.1f * masterVol, 0, 0);
            bullets.Add(new Bullet(player.position, lookDir));
            shootTimer = SaveData.GetFireRate();
        }

        spawnTimer -= dt;
        if (spawnTimer <= 0)
        {
            // Change this in UpdateGameplay
            enemies.Add(new Enemy(new Vector2(rng.Next(800), -150), player.position, rng));
            spawnTimer = currentSpawnRate;
        }

        if (currentObjective.Description == "Survival Time")
        {
            difficultyTimer += dt;
            if (difficultyTimer >= 1f) { currentObjective.Current++; difficultyTimer = 0; }
        }

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            enemies[i].Update(dt);
            if (Vector2.Distance(player.position, enemies[i].position) < 30) currentState = GameState.GameOver;

            for (int j = bullets.Count - 1; j >= 0; j--)
            {
                if (Vector2.Distance(bullets[j].position, enemies[i].position) < 35)
                {
                    _explosionSfx.Play(0.3f * masterVol, 0, 0);
                    if (currentObjective.Description == "Destroy Rocks") currentObjective.Current++;
                    enemies.RemoveAt(i); bullets.RemoveAt(j); score += 100;
                    break;
                }
            }
        }

        foreach (var b in bullets) b.position += b.velocity * dt;
        bullets.RemoveAll(b => Vector2.Distance(player.position, b.position) > 1000);

        if (currentObjective.IsComplete)
        {
            SaveData.Money += currentObjective.Reward;
            currentObjective = Objective.GenerateNew(rng);
            _powerUpSfx.Play(masterVol, 0.5f, 0);
        }
    }

    private void ResetGame()
    {
        score = 0; enemies.Clear(); bullets.Clear();
        player.position = new Vector2(400, 400);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (currentState == GameState.MainMenu) DrawMenu();
        else if (currentState == GameState.Shop) DrawShop();
        else if (currentState == GameState.Playing || currentState == GameState.Paused) DrawGameScene();
        else if (currentState == GameState.Options) DrawOptions();
        else if (currentState == GameState.GameOver) DrawGameOver();

        _spriteBatch.End();
    }

    private void DrawGameScene()
    {
        _spriteBatch.Draw(player.texture, player.position, null, Color.White, player.rotation, player.origin, 4f, SpriteEffects.None, 0f);
        foreach (var e in enemies) _spriteBatch.Draw(enemyTexture, e.position, null, Color.White, e.rotation, new Vector2(25, 25), 4f, SpriteEffects.None, 0f);
        foreach (var b in bullets) _spriteBatch.Draw(dotTexture, new Rectangle((int)b.position.X, (int)b.position.Y, 5, 5), Color.White);

        _spriteBatch.DrawString(_font, $"SCRAP: ${SaveData.Money}", new Vector2(20, 20), Color.Gold);
        _spriteBatch.DrawString(_font, $"GOAL: {currentObjective.Description} ({currentObjective.Current}/{currentObjective.Target})", new Vector2(20, 50), Color.Cyan);
        if (currentState == GameState.Paused) DrawCenteredText("PAUSED", 400, Color.White);
    }

    private void DrawShop()
    {
        DrawCenteredText("UPGRADE SHOP", 100, Color.Gold);
        DrawCenteredText($"YOUR SCRAP: ${SaveData.Money}", 160, Color.White);
        for (int i = 0; i < 2; i++)
        {
            _spriteBatch.Draw(dotTexture, shopButtons[i], selectedIndex == i ? Color.DarkSlateGray : Color.Black);
            _spriteBatch.DrawString(_font, i == 0 ? "FIRE RATE" : "ENGINE SPEED", new Vector2(shopButtons[i].X + 20, shopButtons[i].Y + 20), Color.White);
            _spriteBatch.DrawString(_font, "$100", new Vector2(shopButtons[i].X + 20, shopButtons[i].Y + 50), Color.Yellow);
        }
        DrawCenteredText("PRESS [SPACE] TO START MISSION", 600, Color.LimeGreen);
    }

    private void DrawMenu()
    {
        DrawCenteredText("AESTROID", 200, Color.White);
        DrawCenteredText("START MISSION", 300, selectedIndex == 0 ? Color.Yellow : Color.Gray);
        DrawCenteredText("SETTINGS", 360, selectedIndex == 1 ? Color.Yellow : Color.Gray);
    }

    private void DrawOptions()
    {
        DrawCenteredText("SETTINGS", 200, Color.White);
        DrawCenteredText($"MASTER VOLUME: {(int)(masterVol * 100)}%", 350, Color.Yellow);
        DrawCenteredText("[LEFT/RIGHT] TO ADJUST", 400, Color.Gray);
    }

    private void DrawGameOver()
    {
        DrawCenteredText("GAME OVER", 300, Color.Red);
        DrawCenteredText($"SCORE: {score}", 360, Color.White);
        DrawCenteredText("PRESS [SPACE] TO RETURN TO SHOP", 500, Color.Yellow);
    }

    private void DrawCenteredText(string t, float y, Color c)
    {
        Vector2 s = _font.MeasureString(t);
        _spriteBatch.DrawString(_font, t, new Vector2(400 - s.X / 2, y), c);
    }
}