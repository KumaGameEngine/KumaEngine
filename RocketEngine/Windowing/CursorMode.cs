using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KumaEngine.Windowing
{
    public enum CursorMode
    {
        Normal = 0,
        Hidden = 1,
        Disabled = 2,
        Captured = 3
    }

    public enum GamepadButton
    {
        A = 0,
        B = 1,
        X = 2,
        Y = 3,
        LeftBumper = 4,
        RightBumper = 5,
        Back = 6,
        Start = 7,
        Guide = 8,
        LeftThumb = 9,
        RightThumb = 10,
        DpadUp = 11,
        DpadRight = 12,
        DpadDown = 13,
        DpadLeft = 14,
        Cross = 0,
        Circle = 1,
        Square = 2,
        Triangle = 3
    }

    public enum GamepadAxis
    {
        LeftX = 0,
        LeftY = 1,
        RightX = 2,
        RightY = 3,
        LeftTrigger = 4,
        RightTrigger = 5
    }
}
