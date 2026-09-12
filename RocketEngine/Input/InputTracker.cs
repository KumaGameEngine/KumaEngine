/*
    Taken from: https://github.com/mellinoe/veldrid-samples/tree/master/src/SampleBase
    Modified by: RocketEngine Team
*/

using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace KumaEngine.Input
{
    public static class InputTracker
    {
        private static HashSet<Key> _currentlyPressedKeys = new HashSet<Key>();
        private static HashSet<Key> _newKeysThisFrame = new HashSet<Key>();
        private static HashSet<Key> _KeysPreviousFrame = new HashSet<Key>();

        private static List<char> _pressedKeyChars = new List<char>();

        private static HashSet<MouseButton> _currentlyPressedMouseButtons = new HashSet<MouseButton>();
        private static HashSet<MouseButton> _newMouseButtonsThisFrame = new HashSet<MouseButton>();
        private static HashSet<MouseButton> _MouseButtonsPreviousFrame = new HashSet<MouseButton>();

        public static Vector2 MousePosition;
        public static float ScrollDelta;
        public static InputSnapshot FrameSnapshot { get; private set; }

        public static bool GetKey(Key key)
        {
            return _currentlyPressedKeys.Contains(key);
        }

        public static bool GetKeyDown(Key key)
        {
            return _newKeysThisFrame.Contains(key);
        }

        public static bool GetKeyRelesed(Key key)
        {
            return _KeysPreviousFrame.Contains(key);
        }

        public static Key[] GetKeysDown() =>
            _newKeysThisFrame.ToArray();

        public static char[] GetPressedKeyChars() =>
            _pressedKeyChars.ToArray();

        public static Key[] GetKeys() =>
            _currentlyPressedKeys.ToArray();

        public static Key[] GetReleasedKeys() =>
            _KeysPreviousFrame.ToArray();

        public static bool GetMouseButton(MouseButton button)
        {
            return _currentlyPressedMouseButtons.Contains(button);
        }

        public static bool GetMouseReleased(MouseButton button)
        {
            return _MouseButtonsPreviousFrame.Contains(button);
        }

        public static MouseButton[] GetMouseButtons() =>
            _currentlyPressedMouseButtons.ToArray();

        public static MouseButton[] GetReleasedMouseButtons() =>
            _MouseButtonsPreviousFrame.ToArray();

        public static bool GetMouseButtonDown(MouseButton button)
        {
            return _newMouseButtonsThisFrame.Contains(button);
        }

        public static void UpdateFrameInput(InputSnapshot snapshot)
        {
            FrameSnapshot = snapshot;
            _newKeysThisFrame.Clear();
            _newMouseButtonsThisFrame.Clear();
            _KeysPreviousFrame.Clear();
            _MouseButtonsPreviousFrame.Clear();
            
            _pressedKeyChars.Clear();
            _pressedKeyChars.AddRange(snapshot.KeyCharPresses);

            ScrollDelta = snapshot.WheelDelta;
            
            MousePosition = snapshot.MousePosition;
            for (int i = 0; i < snapshot.KeyEvents.Count; i++)
            {
                KeyEvent ke = snapshot.KeyEvents[i];
                
                if (ke.Down)
                {
                    KeyDown(ke.Key);
                }
                else
                {
                    KeyUp(ke.Key);
                }
            }
            for (int i = 0; i < snapshot.MouseEvents.Count; i++)
            {
                MouseEvent me = snapshot.MouseEvents[i];
                if (me.Down)
                {
                    MouseDown(me.MouseButton);
                }
                else
                {
                    MouseUp(me.MouseButton);
                }
            }
        }

        private static void MouseUp(MouseButton mouseButton)
        {
            _currentlyPressedMouseButtons.Remove(mouseButton);
            _newMouseButtonsThisFrame.Remove(mouseButton);
            _MouseButtonsPreviousFrame.Add(mouseButton);
        }

        private static void MouseDown(MouseButton mouseButton)
        {
            if (_currentlyPressedMouseButtons.Add(mouseButton))
            {
                _newMouseButtonsThisFrame.Add(mouseButton);
            }
        }

        private static void KeyUp(Key key)
        {
            _currentlyPressedKeys.Remove(key);
            _newKeysThisFrame.Remove(key);
            _KeysPreviousFrame.Add(key);
        }

        private static void KeyDown(Key key)
        {
            if (_currentlyPressedKeys.Add(key))
            {
                _newKeysThisFrame.Add(key);
            }
        }
    }
}
