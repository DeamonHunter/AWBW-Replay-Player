using System;

namespace AWBWApp.Game.Game.Logic
{
    //Todo: Better way of moddability
    [Flags]
    public enum MovementType
    {
        None = 0,
        LightInf = 1,
        HeavyInf = 2,
        Tire = 3,
        Tread = 4,
        Sea = 5,
        Lander = 6,
        Air = 7,
        Pipe = 8
    }
}
