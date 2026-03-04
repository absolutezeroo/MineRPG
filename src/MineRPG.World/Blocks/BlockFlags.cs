namespace MineRPG.World.Blocks;

[Flags]
public enum BlockFlags
{
    None         = 0,
    Solid        = 1 << 0,
    Transparent  = 1 << 1,
    Liquid       = 1 << 2,
    Emissive     = 1 << 3,
    Interactable = 1 << 4,
}
