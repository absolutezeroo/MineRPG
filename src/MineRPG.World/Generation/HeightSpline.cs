using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MineRPG.World.Generation;

/// <summary>
/// Piecewise monotone cubic spline that maps a noise input [-1, 1]
/// to a world Y height or multiplier. Control points must be sorted by InputValue.
///
/// Uses Catmull-Rom tangent estimation clamped for monotonicity (Fritsch-Carlson)
/// so the spline never overshoots between adjacent control points.
/// Thread-safe: immutable after construction.
/// </summary>
public sealed class HeightSpline
{
    private const int MinControlPoints = 2;
    private const float MonotonicityThreshold = 1e-10f;
    private const float MaxTangentRatio = 3f;

    private readonly SplinePoint[] _points;
    private readonly float[] _tangents;

    /// <summary>
    /// Creates a height spline from the given control points.
    /// </summary>
    /// <param name="points">At least 2 control points.</param>
    public HeightSpline(IReadOnlyList<SplinePoint> points)
    {
        if (points.Count < MinControlPoints)
        {
            throw new ArgumentException(
                "HeightSpline requires at least 2 control points.", nameof(points));
        }

        _points = new SplinePoint[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            _points[i] = points[i];
        }

        Array.Sort(_points, (a, b) => a.InputValue.CompareTo(b.InputValue));
        _tangents = ComputeTangents(_points);
    }

    /// <summary>
    /// Creates a default spline that linearly maps [-1, 1] to
    /// [baseHeight - variance, baseHeight + variance].
    /// </summary>
    /// <param name="baseHeight">Center height value.</param>
    /// <param name="variance">Height variance from center.</param>
    /// <returns>A new linear height spline.</returns>
    public static HeightSpline CreateDefault(float baseHeight, float variance)
    {
        return new HeightSpline(
        [
            new SplinePoint(-1f, baseHeight - variance),
            new SplinePoint(0f, baseHeight),
            new SplinePoint(1f, baseHeight + variance),
        ]);
    }

    /// <summary>
    /// Evaluate the spline at the given noise input value.
    /// Clamps to the first/last point's OutputY outside the defined range.
    /// </summary>
    /// <param name="input">Noise input value, typically in [-1, 1].</param>
    /// <returns>The interpolated output height.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float input)
    {
        if (input <= _points[0].InputValue)
        {
            return _points[0].OutputY;
        }

        int last = _points.Length - 1;

        if (input >= _points[last].InputValue)
        {
            return _points[last].OutputY;
        }

        int segment = FindSegment(input);
        return HermiteInterpolate(segment, input);
    }

    private int FindSegment(float input)
    {
        int lo = 0;
        int hi = _points.Length - 2;

        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;

            if (_points[mid].InputValue <= input)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float HermiteInterpolate(int segmentIndex, float input)
    {
        float x0 = _points[segmentIndex].InputValue;
        float x1 = _points[segmentIndex + 1].InputValue;
        float y0 = _points[segmentIndex].OutputY;
        float y1 = _points[segmentIndex + 1].OutputY;
        float m0 = _tangents[segmentIndex];
        float m1 = _tangents[segmentIndex + 1];

        float dx = x1 - x0;
        float t = (input - x0) / dx;
        float t2 = t * t;
        float t3 = t2 * t;

        // Hermite basis functions
        float h00 = 2f * t3 - 3f * t2 + 1f;
        float h10 = t3 - 2f * t2 + t;
        float h01 = -2f * t3 + 3f * t2;
        float h11 = t3 - t2;

        return h00 * y0 + h10 * m0 * dx + h01 * y1 + h11 * m1 * dx;
    }

    private static float[] ComputeTangents(SplinePoint[] points)
    {
        int pointCount = points.Length;
        float[] tangents = new float[pointCount];

        // Catmull-Rom tangent estimation
        for (int i = 0; i < pointCount; i++)
        {
            if (i == 0)
            {
                tangents[i] = (points[1].OutputY - points[0].OutputY)
                              / (points[1].InputValue - points[0].InputValue);
            }
            else if (i == pointCount - 1)
            {
                tangents[i] = (points[pointCount - 1].OutputY - points[pointCount - 2].OutputY)
                              / (points[pointCount - 1].InputValue - points[pointCount - 2].InputValue);
            }
            else
            {
                tangents[i] = (points[i + 1].OutputY - points[i - 1].OutputY)
                              / (points[i + 1].InputValue - points[i - 1].InputValue);
            }
        }

        // Fritsch-Carlson monotonicity clamping
        // Pass 1: compute per-segment deltas
        float[] deltas = new float[pointCount - 1];

        for (int i = 0; i < pointCount - 1; i++)
        {
            float dx = points[i + 1].InputValue - points[i].InputValue;
            deltas[i] = dx > 0f ? (points[i + 1].OutputY - points[i].OutputY) / dx : 0f;
        }

        // Pass 2: clamp each tangent independently based on its adjacent deltas
        for (int i = 0; i < pointCount; i++)
        {
            if (i > 0 && i < pointCount - 1)
            {
                // Interior point: check both adjacent segments
                if (MathF.Abs(deltas[i - 1]) < MonotonicityThreshold
                    || MathF.Abs(deltas[i]) < MonotonicityThreshold
                    || (deltas[i - 1] > 0) != (deltas[i] > 0))
                {
                    tangents[i] = 0f;
                    continue;
                }
            }

            // Clamp tangent relative to left segment
            if (i < pointCount - 1 && MathF.Abs(deltas[i]) > MonotonicityThreshold)
            {
                float ratio = tangents[i] / deltas[i];

                if (ratio > MaxTangentRatio)
                {
                    tangents[i] = MaxTangentRatio * deltas[i];
                }
                else if (ratio < -MaxTangentRatio)
                {
                    tangents[i] = -MaxTangentRatio * deltas[i];
                }
            }

            // Clamp tangent relative to right segment
            if (i > 0 && MathF.Abs(deltas[i - 1]) > MonotonicityThreshold)
            {
                float ratio = tangents[i] / deltas[i - 1];

                if (ratio > MaxTangentRatio)
                {
                    tangents[i] = MaxTangentRatio * deltas[i - 1];
                }
                else if (ratio < -MaxTangentRatio)
                {
                    tangents[i] = -MaxTangentRatio * deltas[i - 1];
                }
            }
        }

        return tangents;
    }
}
