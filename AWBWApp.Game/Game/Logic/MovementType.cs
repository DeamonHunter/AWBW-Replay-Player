using System;

namespace AWBWApp.Game.Game.Logic
{
    //Todo: Better way of moddability
    [Flags]
    public enum MovementType
    {
        None = 0,
        LightInf = 1 << 0,
        HeavyInf = 1 << 1,
        Tire = 1 << 2,
        Tread = 1 << 3,
        Sea = 1 << 4,
        Lander = 1 << 5,
        Air = 1 << 6,
        Pipe = 1 << 7
    }
}
