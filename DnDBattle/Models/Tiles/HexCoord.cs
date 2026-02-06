using System;
using System.Collections.Generic;
using DnDBattle.Models;
using DnDBattle.Models.Combat;
using DnDBattle.Models.Combat.Actions;
using DnDBattle.Models.Creatures;
using DnDBattle.Models.Effects;
using DnDBattle.Models.Encounters;
using DnDBattle.Models.Environment;
using DnDBattle.Models.Networking;
using DnDBattle.Models.Spells;
using DnDBattle.Models.Tiles;

namespace DnDBattle.Models.Tiles
{
    /// <summary>
    /// Axial hex coordinate supporting flat-top and pointy-top layouts.
    /// Uses cube-coordinate rounding for precision.
    /// </summary>
    public struct HexCoord : IEquatable<HexCoord>
    {
        /// <summary>Column (axial Q).</summary>
        public int Q { get; set; }

        /// <summary>Row (axial R).</summary>
        public int R { get; set; }

        public HexCoord(int q, int r) { Q = q; R = r; }

        // ── Pixel conversion ──

        /// <summary>Convert a pixel position to the nearest hex coordinate.</summary>
        public static HexCoord FromPixel(double px, double py, double hexSize, GridType type)
        {
            double q, r;
            if (type == GridType.HexFlatTop)
            {
                q = (2.0 / 3.0 * px) / hexSize;
                r = (-1.0 / 3.0 * px + Math.Sqrt(3.0) / 3.0 * py) / hexSize;
            }
            else // PointyTop
            {
                q = (Math.Sqrt(3.0) / 3.0 * px - 1.0 / 3.0 * py) / hexSize;
                r = (2.0 / 3.0 * py) / hexSize;
            }

            return Round(q, r);
        }

        /// <summary>Get pixel center of this hex.</summary>
        public void ToPixel(double hexSize, GridType type, out double x, out double y)
        {
            if (type == GridType.HexFlatTop)
            {
                x = hexSize * (1.5 * Q);
                y = hexSize * (Math.Sqrt(3.0) / 2.0 * Q + Math.Sqrt(3.0) * R);
            }
            else
            {
                x = hexSize * (Math.Sqrt(3.0) * Q + Math.Sqrt(3.0) / 2.0 * R);
                y = hexSize * (1.5 * R);
            }
        }

        // ── Distance & neighbors ──

        /// <summary>Hex distance (number of steps) to another coordinate.</summary>
        public int DistanceTo(HexCoord other)
        {
            return (Math.Abs(Q - other.Q)
                  + Math.Abs(Q + R - other.Q - other.R)
                  + Math.Abs(R - other.R)) / 2;
        }

        /// <summary>Return the six neighboring hex coordinates.</summary>
        public List<HexCoord> GetNeighbors()
        {
            return new List<HexCoord>(6)
            {
                new HexCoord(Q + 1, R),
                new HexCoord(Q + 1, R - 1),
                new HexCoord(Q, R - 1),
                new HexCoord(Q - 1, R),
                new HexCoord(Q - 1, R + 1),
                new HexCoord(Q, R + 1)
            };
        }

        // ── Rounding (cube-coordinate) ──

        private static HexCoord Round(double q, double r)
        {
            double s = -q - r;
            int rq = (int)Math.Round(q);
            int rr = (int)Math.Round(r);
            int rs = (int)Math.Round(s);

            double qDiff = Math.Abs(rq - q);
            double rDiff = Math.Abs(rr - r);
            double sDiff = Math.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
                rq = -rr - rs;
            else if (rDiff > sDiff)
                rr = -rq - rs;

            return new HexCoord(rq, rr);
        }

        // ── Equality ──

        public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
        public override bool Equals(object obj) => obj is HexCoord h && Equals(h);
        public override int GetHashCode() => HashCode.Combine(Q, R);
        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);

        public override string ToString() => $"Hex({Q},{R})";
    }
}
