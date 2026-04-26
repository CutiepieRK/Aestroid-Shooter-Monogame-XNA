using Microsoft.Xna.Framework.Input;

namespace Aestroid;

public static class InputManager
{
    private static KeyboardState _currentKeyState;
    private static KeyboardState _prevKeyState;

    public static void Update()
    {
        _prevKeyState = _currentKeyState;
        _currentKeyState = Keyboard.GetState();
    }

    public static bool IsKeyDown(Keys key)
    {
        return _currentKeyState.IsKeyDown(key);
    }

    public static bool IsKeyPressed(Keys key)
    {
        return _currentKeyState.IsKeyDown(key) && _prevKeyState.IsKeyUp(key);
    }
}