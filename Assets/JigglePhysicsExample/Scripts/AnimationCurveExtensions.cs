/*

Copyright (c) 2018 Pete Michaud, github.com/PeteMichaud

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AnimationCurveExtensions
{

    /// <summary>
    /// Find first derivative of curve at point x
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="x"></param>
    /// <returns>Slope of curve at point x as float</returns>
    public static float Differentiate(this AnimationCurve curve, float x) {
        return curve.Differentiate(x, curve[0].time, curve[curve.length-1].time);
    }

    const float Delta = .000001f;
    public static float Differentiate(this AnimationCurve curve, float x, float xMin, float xMax)
    {
        var x1 = Mathf.Max(xMin, x - Delta);
        var x2 = Mathf.Min(xMax, x + Delta);
        var y1 = curve.Evaluate(x1);
        var y2 = curve.Evaluate(x2);

        return (y2 - y1) / (x2 - x1);
    }


    static IEnumerable<float> GetPointSlopes(AnimationCurve curve, int resolution) {
        for (var i = 0; i < resolution; i++) {
            yield return curve.Differentiate((float)i / resolution);
        }
    }

    public static AnimationCurve Derivative(this AnimationCurve curve, int resolution = 100, float smoothing = .05f) {
        var slopes = GetPointSlopes(curve, resolution).ToArray();

        var curvePoints = slopes
            .Select((s, i) => new Vector2((float) i / resolution, s))
            .ToList();

        var simplifiedCurvePoints = new List<Vector2>();

        if (smoothing > 0) {
            LineUtility.Simplify(curvePoints, smoothing, simplifiedCurvePoints);
        } else {
            simplifiedCurvePoints = curvePoints;
        }

        var derivative = new AnimationCurve(
            simplifiedCurvePoints.Select(v => new Keyframe(v.x, v.y)).ToArray());

        for (int i = 0, len = derivative.keys.Length; i < len; i++) {
            derivative.SmoothTangents(i, 0);
        }

        return derivative;
    }

    /// <summary>
    /// Find integral of curve between xStart and xEnd using the trapezoidal rule
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="xStart"></param>
    /// <param name="xEnd"></param>
    /// <param name="sliceCount">Resolution of calculation. Increase for better
    /// precision, at cost of computation</param>
    /// <returns>Area under the curve between xStart and xEnd as float</returns>
    public static float Integrate(this AnimationCurve curve, float xStart = 0f, float xEnd = 1f, int sliceCount = 100)
    {
        var sliceWidth = (xEnd - xStart) / sliceCount;
        var accumulatedTotal = (curve.Evaluate(xStart) + curve.Evaluate(xEnd)) / 2;

        for (var i = 1; i < sliceCount; i++) {
            accumulatedTotal += curve.Evaluate(xStart + i * sliceWidth);
        }

        return sliceWidth * accumulatedTotal;
    }

#if UNITY_EDITOR
    public static void AutoSmooth(this AnimationCurve curve) {
        for (int i = 0; i < curve.keys.Length; ++i) {
            curve.SmoothTangents(i, 0);
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
    }
#endif

}
