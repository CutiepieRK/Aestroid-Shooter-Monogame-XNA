using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Aestroid;

public static class InputManager
{
    static KeyboardState kState, prevKState;
    static GamePadState pState, prevPState;

    public static void Update() {
        prevKState = kState; kState = Keyboard.GetState();
        prevPState = pState; pState = GamePad.GetState(PlayerIndex.One);
    }

    public static bool IsKeyPressed(Keys k) => kState.IsKeyDown(k) && prevKState.IsKeyUp(k);
    public static bool IsKeyDown(Keys k) => kState.IsKeyDown(k);
    public static bool IsPadPressed(Buttons b) => pState.IsButtonDown(b) && prevPState.IsButtonUp(b);
    public static bool IsPadDown(Buttons b) => pState.IsButtonDown(b);
}