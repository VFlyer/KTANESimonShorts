using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WireGenerator
{
    public static class MeshGenerator
    {
        const double _wireRadius = .001;
        const double _wireRadiusHighlight = .0075;
        const int _numSegments = 4;

        const double _wireMaxSegmentDeviation = .0025;
        const double _wireMaxBézierDeviation = .0025;

        const double _firstControlHeight = 0; // was .01
        const double _interpolateHeight = 0;  // was .007
        const double _firstControlHeightHighlight = 0; // was .01
        const double _interpolateHeightHighlight = 0;  // was .007

        sealed class CPC { public Point ControlBefore, Point, ControlAfter; }

        public enum WirePiece { Uncut, Cut, Copper }

        public static Point[] GenerateWire(System.Random rnd, double _wireLength = .03)
        {
            var length = _wireLength;
            var firstControlHeight = _firstControlHeight;
            var interpolateHeight = _interpolateHeight;
            var start = pt(0, 0, 0);
            var startControl = pt(length / 10, 0, firstControlHeight);
            var endControl = pt(length * 9 / 10, 0, firstControlHeight);
            var end = pt(length, 0, 0);
            var numSegments = _numSegments;

            var bézierSteps = 16;

            var interpolateStart = pt(0, 0, interpolateHeight);
            var interpolateEnd = pt(length, 0, interpolateHeight);

            var intermediatePoints = newArray(numSegments - 1, i => interpolateStart + (interpolateEnd - interpolateStart) * (i + 1) / numSegments + pt(rnd.NextDouble() - .5, 0, rnd.NextDouble() - .5) * _wireMaxSegmentDeviation);
            var deviations = newArray(numSegments - 1, _ => pt(rnd.NextDouble(), 0, rnd.NextDouble()) * _wireMaxBézierDeviation);

            var points =
                new[] { new { ControlBefore = default(Point), Point = start, ControlAfter = startControl } }
                .Concat(intermediatePoints.Select((p, i) => new { ControlBefore = p - deviations[i], Point = p, ControlAfter = p + deviations[i] }))
                .Concat(new[] { new { ControlBefore = endControl, Point = end, ControlAfter = default(Point) } })
                .SelectConsecutivePairs(false, (one, two) => bézier(one.Point, one.ControlAfter, two.ControlBefore, two.Point, bézierSteps))
                .SelectMany((x, i) => i == 0 ? x : x.Skip(1))
                .ToArray();

            return points;
        }

        public static Mesh GenerateWireMesh(Point[] points, Color color)
        {
            return toMesh(createFaces(false, true, tubeFromCurve(points, _wireRadius, 16)), color);
        }

        sealed class VertexInfo
        {
            public Point Point;
            public Point Normal;
            public Vector3 V { get { return new Vector3((float)Point.X, (float)Point.Y, (float)Point.Z); } }
            public Vector3 N { get { return new Vector3((float)Normal.X, (float)Normal.Y, (float)Normal.Z); } }
        }

        private static Mesh toMesh(VertexInfo[][] triangles, Color color)
        {
            return new Mesh
            {
                vertices = triangles.SelectMany(t => t).Select(v => v.V).ToArray(),
                normals = triangles.SelectMany(t => t).Select(v => v.N).ToArray(),
                triangles = triangles.SelectMany(t => t).Select((v, i) => i).ToArray()
            };
        }

        // Converts a 2D array of vertices into triangles by joining each vertex with the next in each dimension
        private static VertexInfo[][] createFaces(bool closedX, bool closedY, VertexInfo[][] meshData)
        {
            var len = meshData[0].Length;
            return Enumerable.Range(0, meshData.Length).SelectManyConsecutivePairs(closedX, (i1, i2) =>
                Enumerable.Range(0, len).SelectManyConsecutivePairs(closedY, (j1, j2) => new[]
                {
                    // triangle 1
                    new[] { meshData[i1][j1], meshData[i2][j1], meshData[i2][j2] },
                    // triangle 2
                    new[] { meshData[i1][j1], meshData[i2][j2], meshData[i1][j2] }
                }))
                    .ToArray();
        }

        private static VertexInfo[][] tubeFromCurve(Point[] pts, double radius, int revSteps)
        {
            var normals = new Point[pts.Length];
            normals[0] = ((pts[1] - pts[0]) * pt(0, 1, 0)).Normalize() * radius;
            for(int i = 1; i < pts.Length - 1; i++)
                normals[i] = normals[i - 1].ProjectOntoPlane((pts[i + 1] - pts[i]) + (pts[i] - pts[i - 1])).Normalize() * radius;
            normals[pts.Length - 1] = normals[pts.Length - 2].ProjectOntoPlane(pts[pts.Length - 1] - pts[pts.Length - 2]).Normalize() * radius;

            var axes = pts.Select((p, i) =>
                i == 0 ? new { Start = pts[0], End = pts[1] } :
                i == pts.Length - 1 ? new { Start = pts[pts.Length - 2], End = pts[pts.Length - 1] } :
                new { Start = p, End = p + (pts[i + 1] - p) + (p - pts[i - 1]) }).ToArray();

            return Enumerable.Range(0, pts.Length)
                .Select(ix => new { Axis = axes[ix], Perp = pts[ix] + normals[ix], Point = pts[ix] })
                .Select(inf => Enumerable.Range(0, revSteps)
                    .Select(i => 360 * i / revSteps)
                    .Select(angle => inf.Perp.Rotate(inf.Axis.Start, inf.Axis.End, angle))
                    .Select(p => new VertexInfo { Point = p, Normal = p - inf.Point }).Reverse().ToArray())
                .ToArray();
        }

        private static IEnumerable<Point> bézier(Point start, Point control1, Point control2, Point end, int steps)
        {
            return Enumerable.Range(0, steps)
                .Select(i => (double)i / (steps - 1))
                .Select(t => pow(1 - t, 3) * start + 3 * pow(1 - t, 2) * t * control1 + 3 * (1 - t) * t * t * control2 + pow(t, 3) * end);
        }

        static double sin(double x)
        {
            return Math.Sin(x * Math.PI / 180);
        }

        static double cos(double x)
        {
            return Math.Cos(x * Math.PI / 180);
        }

        static double pow(double x, double y)
        {
            return Math.Pow(x, y);
        }

        static Point pt(double x, double y, double z)
        {
            return new Point(x, y, z);
        }

        static T[] newArray<T>(int size, Func<int, T> initialiser)
        {
            var result = new T[size];
            for(int i = 0; i < size; i++)
            {
                result[i] = initialiser(i);
            }
            return result;
        }
    }
}