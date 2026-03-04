namespace MineRPG.Core.Math;

/// <summary>
/// A 3D integer direction vector. Used for face directions in meshing and lighting.
/// </summary>
/// <param name="Dx">X component of the direction.</param>
/// <param name="Dy">Y component of the direction.</param>
/// <param name="Dz">Z component of the direction.</param>
public readonly record struct Direction3D(int Dx, int Dy, int Dz);
