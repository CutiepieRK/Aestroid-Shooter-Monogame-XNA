using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Aestroid;

public enum GameState { MainMenu, Playing, Shop, GameOver }

public static class SaveData {
    public static int Money = 0;
    public static int FireRateLevel = 0;
    public static int SpeedLevel = 0;
    public static float GetFireRate() => Math.Max(0.08f, 0.25f - (FireRateLevel * 0.03f));
    public static float GetSpeed() => 300f + (SpeedLevel * 40f);
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
            case GameState.Shop: UpdateShop(); break;
            case GameState.GameOver: if (Input.Pressed(Keys.Space) || Input.Pressed(Buttons.A)) state = GameState.MainMenu; break;
        }
        base.Update(gameTime);
    }

    private void UpdateMenu() {
        if (Input.Pressed(Keys.W) || Input.Pressed(Buttons.DPadUp)) { menuSelect = 0; _click.Play(); }
        if (Input.Pressed(Keys.S) || Input.Pressed(Buttons.DPadDown)) { menuSelect = 1; _click.Play(); }

        if (Input.Pressed(Keys.Enter) || Input.Pressed(Buttons.A) || Input.LeftClick()) {
            if (menuSelect == 0) StartNewGame();
            else state = GameState.Shop;
        }
    }

    private void StartNewGame() {
        score = 0;
        rocks.Clear();
        bullets.Clear();
        player.position = new Vector2(400, 400);
        state = GameState.Playing;
    }

    private void UpdateGame(float dt) {
        // Player Rotation (Mouse or Right Stick)
        Vector2 mousePos = new Vector2(Input.M.X, Input.M.Y);
        Vector2 lookDir = mousePos - player.position;
        if (Input.G.ThumbSticks.Right.Length() > 0.1f) 
            player.rotation = (float)Math.Atan2(Input.G.ThumbSticks.Right.X, -Input.G.ThumbSticks.Right.Y);
        else 
            player.rotation = (float)Math.Atan2(lookDir.Y, lookDir.X) + MathHelper.PiOver2;

        // Movement
        Vector2 move = Vector2.Zero;
        if (Input.K.IsKeyDown(Keys.W)) move = new Vector2((float)Math.Cos(player.rotation - MathHelper.PiOver2), (float)Math.Sin(player.rotation - MathHelper.PiOver2));
        if (Input.G.ThumbSticks.Left.Length() > 0.1f) move = new Vector2(Input.G.ThumbSticks.Left.X, -Input.G.ThumbSticks.Left.Y);
        player.position += move * SaveData.GetSpeed() * dt;

        // Shooting (L-Click or Controller A/RT)
        shootTimer -= dt;
        if ((Input.M.LeftButton == ButtonState.Pressed || Input.Pressed(Buttons.A) || Input.G.Triggers.Right > 0.5f) && shootTimer <= 0) {
            _laser.Play(0.2f, 0, 0);
            Vector2 bDir = new Vector2((float)Math.Cos(player.rotation - MathHelper.PiOver2), (float)Math.Sin(player.rotation - MathHelper.PiOver2));
            bullets.Add(new Bullet(player.position, bDir * 600f));
            shootTimer = SaveData.GetFireRate();
        }

        // Rocks
        spawnTimer -= dt;
        if (spawnTimer <= 0) {
            rocks.Add(new Enemy(new Vector2(rng.Next(800), -50), player.position, rng));
            spawnTimer = 1.2f;
        }

        for (int i = rocks.Count - 1; i >= 0; i--) {
            rocks[i].Update(dt);
            if (Vector2.Distance(player.position, rocks[i].position) < 30) { _boom.Play(); state = GameState.GameOver; }

            for (int j = bullets.Count - 1; j >= 0; j--) {
                if (Vector2.Distance(bullets[j].position, rocks[i].position) < 35) {
                    rocks.RemoveAt(i); bullets.RemoveAt(j);
                    score += 100; SaveData.Money += 10; _boom.Play(0.4f, 0.5f, 0);
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
        if (Input.Pressed(Keys.Escape) || Input.Pressed(Buttons.B)) state = GameState.MainMenu;

        if (Input.Pressed(Keys.Enter) || Input.Pressed(Buttons.A) || Input.LeftClick()) {
            if (menuSelect == 0 && SaveData.Money >= 200) { SaveData.Money -= 200; SaveData.FireRateLevel++; }
            if (menuSelect == 1 && SaveData.Money >= 200) { SaveData.Money -= 200; SaveData.SpeedLevel++; }
        }
    }

    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (state == GameState.MainMenu) {
            DrawBtn("START GAME", 350, menuSelect == 0);
            DrawBtn("UPGRADE SHOP", 420, menuSelect == 1);
            DrawCentered($"MONEY: ${SaveData.Money}", 700, Color.Gold);
        }
        else if (state == GameState.Shop) {
            DrawCentered("SHOP - $200 PER UPGRADE", 200, Color.Gold);
            DrawBtn("FIRE RATE", 350, menuSelect == 0, 100);
            DrawBtn("SHIP SPEED", 350, menuSelect == 1, 450);
            DrawCentered("ESC TO EXIT", 600, Color.Gray);
        }
        else if (state == GameState.Playing) {
            _spriteBatch.Draw(shipTex, player.position, null, Color.White, player.rotation, player.origin, 4f, SpriteEffects.None, 0f);
            foreach (var r in rocks) _spriteBatch.Draw(rockTex, r.position, null, Color.White, r.rotation, new Vector2(25,25), 4f, SpriteEffects.None, 0f);
            foreach (var b in bullets) _spriteBatch.Draw(pixel, new Rectangle((int)b.position.X, (int)b.position.Y, 8, 8), Color.Yellow);
            _spriteBatch.DrawString(_font, $"SCORE: {score}", new Vector2(20,20), Color.White);
            _spriteBatch.DrawString(_font, $"$: {SaveData.Money}", new Vector2(20,50), Color.Gold);
        }
        else if (state == GameState.GameOver) {
            DrawCentered("WRECKED", 350, Color.Red);
            DrawCentered($"FINAL SCORE: {score}", 400, Color.White);
            DrawCentered("PRESS SPACE TO MENU", 500, Color.Gray);
        }

        _spriteBatch.End();
    }

    private void DrawBtn(string t, float y, bool sel, float x = 400) {
        float scale = sel ? 1.2f : 1.0f;
        Color c = sel ? Color.Yellow : Color.White;
        Vector2 size = _font.MeasureString(t) * scale;
        _spriteBatch.DrawString(_font, t, new Vector2(x - size.X/2, y), c, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawCentered(string t, float y, Color c) {
        Vector2 size = _font.MeasureString(t);
        _spriteBatch.DrawString(_font, t, new Vector2(400 - size.X/2, y), c);
    }
}

// --- ESSENTIAL UTILS ---
public static class Input {
    public static KeyboardState K, PK;
    public static MouseState M, PM;
    public static GamePadState G, PG;
    public static void Update() {
        PK = K; K = Keyboard.GetState();
        PM = M; M = Mouse.GetState();
        PG = G; G = GamePad.GetState(PlayerIndex.One);
    }
    public static bool Pressed(Keys k) => K.IsKeyDown(k) && PK.IsKeyUp(k);
    public static bool Pressed(Buttons b) => G.IsButtonDown(b) && PG.IsButtonUp(b);
    public static bool LeftClick() => M.LeftButton == ButtonState.Pressed && PM.LeftButton == ButtonState.Released;
}