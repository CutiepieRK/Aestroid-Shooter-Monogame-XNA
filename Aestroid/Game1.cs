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
public static class SaveData {
    public static int Money = 0;
    public static int FireRateLevel = 0;
    public static int SpeedLevel = 0;
    public static int HighScore = 0;
    public static float GetFireRate() => Math.Max(0.06f, 0.18f - (FireRateLevel * 0.015f));
    public static float GetSpeed() => 300f + (SpeedLevel * 35f);
}

public class Objective {
    public string Description;
    public int Target, Current, Reward;
    public bool IsComplete => Current >= Target;

    public static Objective GenerateNew(Random rng) {
        int level = (SaveData.FireRateLevel + SaveData.SpeedLevel) / 2 + 1;
        return new Objective { Description = "Destroy Rocks", Target = 5 + (5 * level), Reward = 50 * level };
    }
}

// --- FULL HYBRID INPUT MANAGER ---
public static class Input {
    public static KeyboardState K, PrevK;
    public static MouseState M, PrevM;
    public static GamePadState G, PrevG;

    public static void Update() {
        PrevK = K; K = Keyboard.GetState();
        PrevM = M; M = Mouse.GetState();
        PrevG = G; G = GamePad.GetState(PlayerIndex.One);
    }

    public static bool Pressed(Keys k) => K.IsKeyDown(k) && PrevK.IsKeyUp(k);
    public static bool Pressed(Buttons b) => G.IsButtonDown(b) && PrevG.IsButtonUp(b);
    public static bool LeftClick() => M.LeftButton == ButtonState.Pressed && PrevM.LeftButton == ButtonState.Released;
}

public class Game1 : Game {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Texture2D dotTexture, enemyTexture;
    private SoundEffect _laserSfx, _explosionSfx, _clickSfx, _powerUpSfx;

    GameState currentState = GameState.MainMenu;
    int selectedIndex = 0;
    Rectangle[] menuButtons = new Rectangle[2];
    Rectangle[] shopButtons = new Rectangle[2];

    Sprite player;
    List<Enemy> enemies = new List<Enemy>();
    List<Bullet> bullets = new List<Bullet>();
    Objective currentObjective;

    int currentScore = 0;
    float shootTimer, spawnTimer;
    Random rng = new Random();

    public Game1() {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize() {
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 800;
        _graphics.ApplyChanges();
        currentObjective = Objective.GenerateNew(rng);
        
        // Define Hitboxes for Mouse Interaction
        for (int i = 0; i < 2; i++) menuButtons[i] = new Rectangle(300, 350 + (i * 60), 200, 40);
        for (int i = 0; i < 2; i++) shopButtons[i] = new Rectangle(100 + (i * 350), 350, 250, 80);
        
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("ScoreFont");
        player = new Sprite(Content.Load<Texture2D>("SPACE SHIP"), new Vector2(400, 400), 4f, Color.White, 300f);
        enemyTexture = Content.Load<Texture2D>("ROCKS");
        _laserSfx = Content.Load<SoundEffect>("laserShoot (1)");
        _explosionSfx = Content.Load<SoundEffect>("explosion");
        _clickSfx = Content.Load<SoundEffect>("click");
        _powerUpSfx = Content.Load<SoundEffect>("powerUp");
        dotTexture = new Texture2D(GraphicsDevice, 1, 1);
        dotTexture.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime) {
        Input.Update();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Point mousePos = new Point(Input.M.X, Input.M.Y);

        switch (currentState) {
            case GameState.MainMenu:
                UpdateMenuNav(mousePos, menuButtons);
                if (Input.Pressed(Keys.Enter) || Input.Pressed(Buttons.A) || Input.LeftClick()) {
                    _clickSfx.Play();
                    if (selectedIndex == 0) { ResetGame(); currentState = GameState.Playing; }
                    else currentState = GameState.Options;
                }
                break;

            case GameState.Playing:
                UpdateGameplay(dt);
                if (Input.Pressed(Keys.B) || Input.Pressed(Buttons.Y)) currentState = GameState.Shop;
                if (Input.Pressed(Keys.Escape) || Input.Pressed(Buttons.Start)) currentState = GameState.Paused;
                break;

            case GameState.Shop:
                UpdateMenuNav(mousePos, shopButtons);
                if (Input.Pressed(Keys.Enter) || Input.Pressed(Buttons.A) || Input.LeftClick()) {
                    if (selectedIndex == 0 && SaveData.Money >= 100) { SaveData.Money -= 100; SaveData.FireRateLevel++; _powerUpSfx.Play(); }
                    if (selectedIndex == 1 && SaveData.Money >= 100) { SaveData.Money -= 100; SaveData.SpeedLevel++; _powerUpSfx.Play(); }
                }
                if (Input.Pressed(Keys.B) || Input.Pressed(Keys.Escape) || Input.Pressed(Buttons.B)) currentState = GameState.Playing;
                break;

            case GameState.Paused:
                if (Input.Pressed(Keys.Escape) || Input.Pressed(Buttons.Start)) currentState = GameState.Playing;
                break;

            case GameState.GameOver:
                if (Input.Pressed(Keys.Space) || Input.Pressed(Buttons.A)) { ResetGame(); currentState = GameState.Playing; }
                if (Input.Pressed(Keys.B) || Input.Pressed(Buttons.Y)) currentState = GameState.Shop;
                break;
        }
        base.Update(gameTime);
    }

    private void UpdateMenuNav(Point mPos, Rectangle[] buttons) {
        // Keyboard/Gamepad Nav
        if (Input.Pressed(Keys.W) || Input.Pressed(Buttons.DPadUp)) selectedIndex = 0;
        if (Input.Pressed(Keys.S) || Input.Pressed(Buttons.DPadDown)) selectedIndex = 1;
        if (Input.Pressed(Keys.A) || Input.Pressed(Buttons.DPadLeft)) selectedIndex = 0;
        if (Input.Pressed(Keys.D) || Input.Pressed(Buttons.DPadRight)) selectedIndex = 1;

        // Mouse Hover Nav (Fixes the "Mouse Not Working" issue)
        for (int i = 0; i < buttons.Length; i++) {
            if (buttons[i].Contains(mPos) && (Input.M.Position != Input.PrevM.Position)) {
                selectedIndex = i;
            }
        }
    }

    private void UpdateGameplay(float dt) {
        player.speed = SaveData.GetSpeed();
        
        // Aiming Logic (Hybrid Mouse/Stick)
        Vector2 aimDir = Vector2.Zero;
        if (Input.G.ThumbSticks.Right.Length() > 0.1f) {
            aimDir = new Vector2(Input.G.ThumbSticks.Right.X, -Input.G.ThumbSticks.Right.Y);
            player.rotation = (float)Math.Atan2(aimDir.Y, aimDir.X) + MathHelper.PiOver2;
        } else {
            Vector2 mouseDir = new Vector2(Input.M.X, Input.M.Y) - player.position;
            player.rotation = (float)Math.Atan2(mouseDir.Y, mouseDir.X) + MathHelper.PiOver2;
        }

        // Movement (Hybrid WASD/Stick)
        Vector2 moveInput = Vector2.Zero;
        if (Input.K.IsKeyDown(Keys.W)) moveInput = new Vector2((float)Math.Cos(player.rotation - MathHelper.PiOver2), (float)Math.Sin(player.rotation - MathHelper.PiOver2));
        if (Input.G.ThumbSticks.Left.Length() > 0.1f) moveInput = new Vector2(Input.G.ThumbSticks.Left.X, -Input.G.ThumbSticks.Left.Y);
        player.position += moveInput * player.speed * dt;

        // Shooting
        shootTimer -= dt;
        if ((Input.M.LeftButton == ButtonState.Pressed || Input.G.Triggers.Right > 0.5f) && shootTimer <= 0) {
            _laserSfx.Play(0.1f, 0, 0);
            Vector2 bulletDir = new Vector2((float)Math.Cos(player.rotation - MathHelper.PiOver2), (float)Math.Sin(player.rotation - MathHelper.PiOver2));
            bullets.Add(new Bullet(player.position, bulletDir));
            shootTimer = SaveData.GetFireRate();
        }

        // Standard Spawning/Collision Logic
        spawnTimer -= dt;
        if (spawnTimer <= 0) {
            enemies.Add(new Enemy(new Vector2(rng.Next(800), -100), player.position, rng));
            spawnTimer = 1.5f;
        }

        for (int i = enemies.Count - 1; i >= 0; i--) {
            enemies[i].Update(dt);
            if (Vector2.Distance(player.position, enemies[i].position) < 28) {
                _explosionSfx.Play();
                if (currentScore > SaveData.HighScore) SaveData.HighScore = currentScore;
                currentState = GameState.GameOver;
            }
            for (int j = bullets.Count - 1; j >= 0; j--) {
                if (Vector2.Distance(bullets[j].position, enemies[i].position) < 32) {
                    enemies.RemoveAt(i); bullets.RemoveAt(j);
                    currentScore += 100; currentObjective.Current++;
                    _explosionSfx.Play(0.2f, 0.5f, 0);
                    break;
                }
            }
        }
        foreach (var b in bullets) b.position += b.velocity * dt;
        bullets.RemoveAll(b => Vector2.Distance(player.position, b.position) > 1000);

        if (currentObjective.IsComplete) {
            SaveData.Money += currentObjective.Reward;
            currentObjective = Objective.GenerateNew(rng);
            _powerUpSfx.Play();
        }
    }

    private void ResetGame() {
        currentScore = 0; enemies.Clear(); bullets.Clear();
        player.position = new Vector2(400, 400);
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (currentState == GameState.Playing || currentState == GameState.Paused || currentState == GameState.Shop) {
            _spriteBatch.Draw(player.texture, player.position, null, Color.White, player.rotation, player.origin, 4f, SpriteEffects.None, 0f);
            foreach (var e in enemies) _spriteBatch.Draw(enemyTexture, e.position, null, Color.White, e.rotation, new Vector2(25, 25), 4f, SpriteEffects.None, 0f);
            foreach (var b in bullets) _spriteBatch.Draw(dotTexture, new Rectangle((int)b.position.X, (int)b.position.Y, 6, 6), Color.Yellow);
            DrawHUD();
        }

        if (currentState == GameState.MainMenu) {
            DrawCenteredText("AESTROID", 250, Color.White);
            DrawCenteredText("START MISSION", 350, selectedIndex == 0 ? Color.Yellow : Color.Gray);
            DrawCenteredText("SETTINGS", 410, selectedIndex == 1 ? Color.Yellow : Color.Gray);
        }

        if (currentState == GameState.Shop) {
            _spriteBatch.Draw(dotTexture, new Rectangle(0,0,800,800), Color.Black * 0.7f);
            DrawCenteredText("SHOP (ESC TO CLOSE)", 250, Color.Gold);
            for (int i = 0; i < 2; i++) {
                _spriteBatch.Draw(dotTexture, shopButtons[i], selectedIndex == i ? Color.DarkRed : Color.DimGray);
                _spriteBatch.DrawString(_font, i == 0 ? "FIRE RATE ($100)" : "SPEED ($100)", new Vector2(shopButtons[i].X + 20, shopButtons[i].Y + 25), Color.White);
            }
        }

        if (currentState == GameState.GameOver) {
            DrawCenteredText("GAME OVER", 300, Color.Red);
            DrawCenteredText("PRESS [SPACE] OR [A] TO RESTART", 400, Color.White);
            DrawCenteredText("PRESS [B] OR [Y] FOR SHOP", 450, Color.Yellow);
        }

        _spriteBatch.End();
    }

    private void DrawHUD() {
        _spriteBatch.DrawString(_font, $"MONEY: ${SaveData.Money}", new Vector2(20, 20), Color.Gold);
        _spriteBatch.DrawString(_font, $"GOAL: {currentObjective.Description} ({currentObjective.Current}/{currentObjective.Target})", new Vector2(20, 50), Color.Cyan);
    }

    private void DrawCenteredText(string t, float y, Color c) {
        Vector2 s = _font.MeasureString(t);
        _spriteBatch.DrawString(_font, t, new Vector2(400 - s.X / 2, y), c);
    }
}