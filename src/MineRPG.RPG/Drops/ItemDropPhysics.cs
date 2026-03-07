using System;

namespace MineRPG.RPG.Drops;

/// <summary>
/// Stateless physics helper for dropped items. Handles gravity,
/// velocity integration, ground clamping, and horizontal damping.
/// Called by <see cref="ItemDropManager.UpdateDrops"/> each frame.
/// </summary>
public static class ItemDropPhysics
{
    /// <summary>Gravitational acceleration in units per second squared.</summary>
    public const float Gravity = 9.8f;

    /// <summary>
    /// Fraction of horizontal velocity remaining after 1 second.
    /// Applied as exponential decay: velocity *= Pow(Damping, deltaTime).
    /// </summary>
    public const float HorizontalDamping = 0.05f;

    /// <summary>Absolute velocity below which horizontal movement stops entirely.</summary>
    public const float VelocityDeadZone = 0.01f;

    /// <summary>Y offset so the item floats at the center of the block surface.</summary>
    public const float SurfaceOffset = 0.25f;

    /// <summary>
    /// Advances drop physics by one frame. Mutates
    /// <see cref="DroppedItem.WorldX"/>, <see cref="DroppedItem.WorldY"/>,
    /// <see cref="DroppedItem.WorldZ"/>, and <see cref="DroppedItem.Velocity"/>.
    /// </summary>
    /// <param name="drop">The dropped item to update.</param>
    /// <param name="deltaTime">Frame delta in seconds.</param>
    /// <param name="surfaceY">Terrain surface Y from ITerrainQuery. Negative means unloaded.</param>
    /// <returns>True if the drop is resting on the ground this frame.</returns>
    public static bool Step(DroppedItem drop, float deltaTime, float surfaceY)
    {
        float velocityX = drop.Velocity.X;
        float velocityY = drop.Velocity.Y;
        float velocityZ = drop.Velocity.Z;

        // Apply gravity
        velocityY -= Gravity * deltaTime;

        // Integrate position
        drop.WorldX += velocityX * deltaTime;
        drop.WorldY += velocityY * deltaTime;
        drop.WorldZ += velocityZ * deltaTime;

        bool isResting = false;

        // Ground collision (only if terrain is loaded)
        if (surfaceY >= 0f)
        {
            float restY = surfaceY + SurfaceOffset;

            if (drop.WorldY <= restY)
            {
                drop.WorldY = restY;
                velocityY = 0f;

                // Exponential damping on horizontal velocity
                float dampFactor = MathF.Pow(HorizontalDamping, deltaTime);
                velocityX *= dampFactor;
                velocityZ *= dampFactor;

                // Kill tiny velocities
                if (MathF.Abs(velocityX) < VelocityDeadZone)
                {
                    velocityX = 0f;
                }

                if (MathF.Abs(velocityZ) < VelocityDeadZone)
                {
                    velocityZ = 0f;
                }

                isResting = velocityX == 0f && velocityZ == 0f;
            }
        }

        drop.Velocity = new DropVelocity(velocityX, velocityY, velocityZ);
        return isResting;
    }
}
