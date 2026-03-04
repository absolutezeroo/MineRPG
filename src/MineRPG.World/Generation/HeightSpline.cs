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
    private readonly SplinePoint[] _points;
    private readonly float[] _tangents;

    public HeightSpline(IReadOnlyList<SplinePoint> points)
    {
        if (points.Count < 2)
            throw new ArgumentException("HeightSpline requires at least 2 control points.", nameof(points));

        _points = new SplinePoint[points.Count];
        for (var i = 0; i < points.Count; i++)
            _points[i] = points[i];

        Array.Sort(_points, (a, b) => a.InputValue.CompareTo(b.InputValue));
        _tangents = ComputeTangents(_points);
    }

    /// <summary>
    /// Creates a default spline that linearly maps [-1, 1] to
    /// [baseHeight - variance, baseHeight + variance].
    /// </summary>
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float input)
    {
        if (input <= _points[0].InputValue)
            return _points[0].OutputY;

        var last = _points.Length - 1;
        if (input >= _points[last].InputValue)
            return _points[last].OutputY;

        var i = FindSegment(input);
        return HermiteInterpolate(i, input);
    }

    private int FindSegment(float input)
    {
        var lo = 0;
        var hi = _points.Length - 2;

        while (lo < hi)
        {
            var mid = (lo + hi + 1) >> 1;
            if (_points[mid].InputValue <= input)
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float HermiteInterpolate(int i, float input)
    {
        var x0 = _points[i].InputValue;
        var x1 = _points[i + 1].InputValue;
        var y0 = _points[i].OutputY;
        var y1 = _points[i + 1].OutputY;
        var m0 = _tangents[i];
        var m1 = _tangents[i + 1];

        var dx = x1 - x0;
        var t = (input - x0) / dx;
        var t2 = t * t;
        var t3 = t2 * t;

        // Hermite basis functions
        var h00 = 2f * t3 - 3f * t2 + 1f;
        var h10 = t3 - 2f * t2 + t;
        var h01 = -2f * t3 + 3f * t2;
        var h11 = t3 - t2;

        return h00 * y0 + h10 * m0 * dx + h01 * y1 + h11 * m1 * dx;
    }

    private static float[] ComputeTangents(SplinePoint[] points)
    {
        var n = points.Length;
        var tangents = new float[n];

        // Catmull-Rom tangent estimation
        for (var i = 0; i < n; i++)
        {
            if (i == 0)
            {
                tangents[i] = (points[1].OutputY - points[0].OutputY)
                              / (points[1].InputValue - points[0].InputValue);
            }
            else if (i == n - 1)
            {
                tangents[i] = (points[n - 1].OutputY - points[n - 2].OutputY)
                              / (points[n - 1].InputValue - points[n - 2].InputValue);
            }
            else
            {
                tangents[i] = (points[i + 1].OutputY - points[i - 1].OutputY)
                              / (points[i + 1].InputValue - points[i - 1].InputValue);
            }
        }

        // Fritsch-Carlson monotonicity clamping (two-pass to avoid forward corruption)
        // Pass 1: compute per-segment deltas and alpha/beta ratios
        var deltas = new float[n - 1];
        for (var i = 0; i < n - 1; i++)
        {
            var dx = points[i + 1].InputValue - points[i].InputValue;
            deltas[i] = dx > 0f ? (points[i + 1].OutputY - points[i].OutputY) / dx : 0f;
        }

        // Pass 2: clamp each tangent independently based on its adjacent deltas
        for (var i = 0; i < n; i++)
        {
            if (i > 0 && i < n - 1)
            {
                // Interior point: check both adjacent segments
                if (MathF.Abs(deltas[i - 1]) < 1e-10f || MathF.Abs(deltas[i]) < 1e-10f
                    || (deltas[i - 1] > 0) != (deltas[i] > 0))
                {
                    tangents[i] = 0f;
                    continue;
                }
            }

            // Clamp tangent relative to left segment
            if (i < n - 1 && MathF.Abs(deltas[i]) > 1e-10f)
            {
                var ratio = tangents[i] / deltas[i];
                if (ratio > 3f)
                    tangents[i] = 3f * deltas[i];
                else if (ratio < -3f)
                    tangents[i] = -3f * deltas[i];
            }

            // Clamp tangent relative to right segment
            if (i > 0 && MathF.Abs(deltas[i - 1]) > 1e-10f)
            {
                var ratio = tangents[i] / deltas[i - 1];
                if (ratio > 3f)
                    tangents[i] = 3f * deltas[i - 1];
                else if (ratio < -3f)
                    tangents[i] = -3f * deltas[i - 1];
            }
        }

        return tangents;
    }
}
