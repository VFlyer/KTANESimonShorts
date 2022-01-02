using System;

namespace WireGenerator
{
    public struct Point : IEquatable<Point>
    {
        public double X; public double Y; public double Z;
        public override string ToString() { return string.Format("({0:R}, {1:R}, {2:R})", X, Y, Z); }
        public Point(double x, double y, double z) { X = x; Y = y; Z = z; }
        public Point Add(double x = 0, double y = 0, double z = 0) { return new Point(X + x, Y + y, Z + z); }
        public Point Set(double? x = null, double? y = null, double? z = null) { return new Point(x ?? X, y ?? Y, z ?? Z); }

        public static bool operator ==(Point one, Point two) { return one.X == two.X && one.Y == two.Y && one.Z == two.Z; }
        public static bool operator !=(Point one, Point two) { return one.X != two.X || one.Y != two.Y || one.Z != two.Z; }
        public override bool Equals(object obj) { return obj is Point && ((Point)obj) == this; }
        public override int GetHashCode() { return unchecked((X.GetHashCode() * 31 + Y.GetHashCode()) * 31 + Z.GetHashCode()); }
        public bool Equals(Point other) { return other == this; }

        public static Point operator +(Point one, Point two) { return new Point(one.X + two.X, one.Y + two.Y, one.Z + two.Z); }
        public static Point operator -(Point one, Point two) { return new Point(one.X - two.X, one.Y - two.Y, one.Z - two.Z); }
        public static Point operator *(Point one, double two) { return new Point(one.X * two, one.Y * two, one.Z * two); }
        public static Point operator *(double one, Point two) { return new Point(two.X * one, two.Y * one, two.Z * one); }
        public static Point operator /(Point one, double two) { return new Point(one.X / two, one.Y / two, one.Z / two); }
        public static Point operator -(Point one) { return new Point(-one.X, -one.Y, -one.Z); }

        // Vector cross product
        public static Point operator *(Point a, Point b) { return new Point(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }

        public bool IsZero { get { return X == 0 && Y == 0 && Z == 0; } }

        public Point Normalize()
        {
            var d = Math.Sqrt(X * X + Y * Y + Z * Z);
            if (d == 0)
                return this;
            return new Point(X / d, Y / d, Z / d);
        }

        public Point Rotate(Point axisStart, Point axisEnd, double angle)
        {
            var a = axisStart.X;
            var b = axisStart.Y;
            var c = axisStart.Z;
            var u = axisEnd.X - a;
            var v = axisEnd.Y - b;
            var w = axisEnd.Z - c;
            var nf = Math.Sqrt(u * u + v * v + w * w);
            u /= nf;
            v /= nf;
            w /= nf;
            var θ = angle * Math.PI / 180;
            var cosθ = Math.Cos(θ);
            var sinθ = Math.Sin(θ);

            return new Point(
                (a * (v * v + w * w) - u * (b * v + c * w - u * X - v * Y - w * Z)) * (1 - cosθ) + X * cosθ + (-c * v + b * w - w * Y + v * Z) * sinθ,
                (b * (u * u + w * w) - v * (a * u + c * w - u * X - v * Y - w * Z)) * (1 - cosθ) + Y * cosθ + (c * u - a * w + w * X - u * Z) * sinθ,
                (c * (u * u + v * v) - w * (a * u + b * v - u * X - v * Y - w * Z)) * (1 - cosθ) + Z * cosθ + (-b * u + a * v - v * X + u * Y) * sinθ);
        }

        public Point ProjectOntoPlane(Point planeNormal)
        {
            planeNormal = planeNormal.Normalize();
            return this - Dot(planeNormal) * planeNormal;
        }

        public double Dot(Point other)
        {
            return X * other.X + Y * other.Y + Z * other.Z;
        }

        public double Distance(Point other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}