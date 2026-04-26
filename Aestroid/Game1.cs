using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aestroid;

public enum GameState { MainMenu, Playing, Paused, Shop, GameOver }

public static class SaveData {
    public static int Money = 0;
    public static int FireRateLevel = 0;
    public static int SpeedLevel = 0;
    public static float GetFireRate() => Math.Max(0.07f, 0.30f - (FireRateLevel * 0.03f));
    public static float GetSpeed() => 300f + (SpeedLevel * 35f);
}

public class Game1 : Game {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Texture2D shipTex, rockTex, pixel;
    private SoundEffect _laser, _boom, _click;

    GameState state = GameState.MainMenu;
    int menuSelect = 0;
    float shootTimer, spawnTimer;
    int score = 0;

    Sprite player;
    List<Enemy> rocks = new List<Enemy>();
    List<Bullet> bullets = new List<Bullet>();
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
        base.Initialize();
    }

    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("ScoreFont");
        shipTex = Content.Load<Texture2D>("SPACE SHIP");
        rockTex = Content.Load<Texture2D>("ROCKS");
        _laser = Content.Load<SoundEffect>("laserShoot (1)");
        _boom = Content.Load<SoundEffect>("explosion");
        _click = Content.Load<SoundEffect>("click");

        player = new Sprite(shipTex, new Vector2(400, 400), 4f, Color.White, 300f);
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime) {
        Input.Update();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (state) {
            case GameState.MainMenu: UpdateMenu(); break;
            case GameState.Playing: UpdateGame(dt); break;
            case GameState.Paused: UpdatePause(); break;
            case GameState.Shop: UpdateShop(); break;
            case GameState.GameOver: if (Input.Pressed(Keys.Space) || Input.Pressed(Buttons.A)) state = GameState.MainMenu; break;
        }
        base.Update(gameTime);
    }

    private void UpdateMenu() {
        if (Input.Pressed(Keys.W) || Input.Pressed(Buttons.DPadUp)) menuSelect = 0;
        if (Input.Pressed(Keys.S) || Input.Pressed(Buttons.DPadDown)) menuSelect = 1;

        if (Input.Pressed(Keys.Enter) || Input.Pressed(Buttons.A)) {
            _click.Play();
            if (menuSelect == 0) StartNewGame();
            else state = GameState.Shop;
        }
    }

    private void StartNewGame() {
        score = 0; rocks.Clear(); bullets.Clear();
        player.position = new Vector2(400, 400);
        state = GameState.Playing;
    }

    private void UpdatePause() {
        if (Input.Pressed(Keys.W) || Input.Pressed(Buttons.DPadUp)) menuSelect = 0;
        if (Input.Pressed(Keys.S) || Input.Pressed(Buttons.DPadDown)) menuSelect = 1;

        if (Input.Pressed(Keys.Enter) || Input.Pressed(Buttons.A)) {
            if (menuSelect == 0) state = GameState.Playing;
            else state = GameState.Shop;
        }
        if (Input.Pressed(Keys.Escape) || Input.Pressed(Buttons.Start)) state = GameState.Playing;
    }

    private void UpdateGame(float dt) {
        if (Input.Pressed(Keys.Escape) || Input.Pressed(Buttons.Start)) { state = GameState.Paused; menuSelect = 0; return; }

        // --- AUTO-AIM LOGIC ---
        Enemy closest = null;
        float minDist = 500f; // Targeting range
        foreach (var r in rocks) {
            float d = Vector2.Distance(player.position, r.position);
            if (d < minDist) { minDist = d; closest = r; }
        }

        if (closest != null) {
            Vector2 lookDir = closest.position - player.position;
            player.rotation = (float)Math.Atan2(lookDir.Y, lookDir.X) + MathHelper.PiOver2;
            
            shootTimer -= dt;
            if (shootTimer <= 0) {
                _laser.Play(0.1f, 0, 0);
                Vector2 bDir = Vector2.Normalize(lookDir);
                bullets.Add(new Bullet(player.position, bDir * 700f));
                shootTimer = SaveData.GetFireRate();
            }
        }

        // --- MOVEMENT ---
        Vector2 move = Vector2.Zero;
        if (Input.K.IsKeyDown(Keys.W) || Input.K.IsKeyDown(Keys.Up)) move.Y = -1;
        if (Input.K.IsKeyDown(Keys.S) || Input.K.IsKeyDown(Keys.Down)) move.Y = 1;
        if (Input.K.IsKeyDown(Keys.A) || Input.K.IsKeyDown(Keys.Left)) move.X = -1;
        if (Input.K.IsKeyDown(Keys.D) || Input.K.IsKeyDown(Keys.Right)) move.X = 1;
        
        if (Input.G.ThumbSticks.Left.Length() > 0.1f) 
            move = new Vector2(Input.G.ThumbSticks.Left.X, -Input.G.ThumbSticks.Left.Y);

        if (move != Vector2.Zero) {
            move.Normalize();
            player.position += move * SaveData.GetSpeed() * dt;
        }

        // --- ROCKS & COLLISION ---
        spawnTimer -= dt;
        if (spawnTimer <= 0) {
            rocks.Add(new Enemy(new Vector2(rng.Next(800), -50), player.position, rng));
            spawnTimer = 1.0f;
        }

        for (int i = rocks.Count - 1; i >= 0; i--) {
            rocks[i].Update(dt);
            if (Vector2.Distance(player.position, rocks[i].position) < 30) { _boom.Play(); state = GameState.GameOver; }

            for (int j = bullets.Count - 1; j >= 0; j--) {
                if (Vector2.Distance(bullets[j].position, rocks[i].position) < 35) {
                    rocks.RemoveAt(i); bullets.RemoveAt(j);
                    score += 100; SaveData.Money += 15; _boom.Play(0.3f, 0.5f, 0);
                    break;
                }
            }
        }
        foreach (var b in bullets) b.position += b.velocity * dt;
        bullets.RemoveAll(b => b.position.Y < -50 || b.position.Y > 850 || b.position.X < -50 || b.position.X > 850);
    }

    private void UpdateShop() {
        if (Input.Pressed(Keys.A) || Input.Pressed(Buttons.DPadLeft)) menuSelect = 0;
        if (Input.Pressed(Keys.D) || Input.Pressed(Buttons.DPadRight)) menuSelect = 1;
        
        if (Input.Pressed(Keys.Enter) || Input.Pressed(Buttons.A)) {
            if (menuSelect == 0 && SaveData.Money >= 250) { SaveData.Money -= 250; SaveData.FireRateLevel++; _click.Play(); }
            if (menuSelect == 1 && SaveData.Money >= 250) { SaveData.Money -= 250; SaveData.SpeedLevel++; _click.Play(); }
        }
        
        if (Input.Pressed(Keys.Escape) || Input.Pressed(Buttons.B)) state = GameState.Paused;
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (state == GameState.Playing || state == GameState.Paused || state == GameState.Shop) {
            _spriteBatch.Draw(shipTex, player.position, null, Color.White, player.rotation, player.origin, 4f, SpriteEffects.None, 0f);
            foreach (var r in rocks) _spriteBatch.Draw(rockTex, r.position, null, Color.White, r.rotation, new Vector2(25,25), 4f, SpriteEffects.None, 0f);
            foreach (var b in bullets) _spriteBatch.Draw(pixel, new Rectangle((int)b.position.X, (int)b.position.Y, 10, 10), Color.White); // WHITE BULLETS
            
            // HUD
            _spriteBatch.DrawString(_font, $"CASH: ${SaveData.Money}", new Vector2(20, 20), Color.Gold);
            _spriteBatch.DrawString(_font, $"SCORE: {score}", new Vector2(20, 50), Color.White);
        }

        if (state == GameState.MainMenu) {
            DrawCentered("AESTROID ARCADE", 250, Color.White, 1.5f);
            DrawBtn("PILOT MISSION", 400, menuSelect == 0);
            DrawBtn("EQUIPMENT SHOP", 470, menuSelect == 1);
        }
        else if (state == GameState.Paused) {
            _spriteBatch.Draw(pixel, new Rectangle(0, 0, 800, 800), Color.Black * 0.5f);
            DrawCentered("PAUSED", 300, Color.Cyan, 1.2f);
            DrawBtn("RESUME", 400, menuSelect == 0);
            DrawBtn("VISIT SHOP", 470, menuSelect == 1);
        }
        else if (state == GameState.Shop) {
            _spriteBatch.Draw(pixel, new Rectangle(0, 0, 800, 800), Color.Black * 0.8f);
            DrawCentered("STATION SHOP - ALL UPGRADES $250", 200, Color.Gold);
            DrawCentered($"CURRENT CASH: ${SaveData.Money}", 250, Color.White);
            DrawBtn($"FIRE RATE (LVL {SaveData.FireRateLevel})", 400, menuSelect == 0, 200);
            DrawBtn($"THRUSTERS (LVL {SaveData.SpeedLevel})", 400, menuSelect == 1, 600);
            DrawCentered("PRESS [B] TO GO BACK", 600, Color.Gray);
        }
        else if (state == GameState.GameOver) {
            DrawCentered("SHIP DESTROYED", 350, Color.Red, 1.5f);
            DrawCentered("PRESS [A] FOR MAIN MENU", 450, Color.White);
        }

        _spriteBatch.End();
    }

    private void DrawBtn(string t, float y, bool sel, float x = 400) {
        Color c = sel ? Color.Yellow : Color.Gray;
        float scale = sel ? 1.1f : 0.9f;
        Vector2 size = _font.MeasureString(t) * scale;
        _spriteBatch.DrawString(_font, t, new Vector2(x - size.X/2, y), c, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawCentered(string t, float y, Color c, float scale = 1f) {
        Vector2 size = _font.MeasureString(t) * scale;
        _spriteBatch.DrawString(_font, t, new Vector2(400 - size.X/2, y), c, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}

public static class Input {
    public static KeyboardState K, PK;
    public static GamePadState G, PG;
    public static MouseState M;
    public static void Update() {
        PK = K; K = Keyboard.GetState();
        PG = G; G = GamePad.GetState(PlayerIndex.One);
        M = Mouse.GetState();
    }
    public static bool Pressed(Keys k) => K.IsKeyDown(k) && PK.IsKeyUp(k);
    public static bool Pressed(Buttons b) => G.IsButtonDown(b) && PG.IsButtonUp(b);
}