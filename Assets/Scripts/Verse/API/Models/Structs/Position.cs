using System;

namespace Verse.API.Models {
    public struct Position {
        public readonly float x;
        public readonly float y;

        public Position(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public override string ToString() {
            return "PlayerPosition(" + x + ", " + y + ")";
        }

        public float SqrMagnitude => (float) (x * (double) x + y * (double) y);

        public float Magnitude => (float) Math.Sqrt(SqrMagnitude);

        public static float Distance(Position from, Position to) {
            return (from - to).Magnitude;
        }

        public Position normalized {
            get {
                float magnitude = Magnitude;
                if ((double) magnitude > 9.99999974737875E-06)
                    return this / magnitude;
                return Zero;
            }
        }

        public TilePosition NearestTilePosition => new TilePosition((int) Math.Round(x), (int) Math.Round(y));
        public TilePosition CurrentTilePosition => new TilePosition((int) x, (int) y);

        public static Position Zero = new Position(0, 0);
        public static Position Max = new Position(float.MaxValue, float.MaxValue);
        public static Position Min = new Position(float.MinValue, float.MinValue);


        public static Position operator +(Position a, Position b) {
            return new Position(a.x + b.x, a.y + b.y);
        }

        public static Position operator -(Position a, Position b) {
            return new Position(a.x - b.x, a.y - b.y);
        }

        public static Position operator *(Position a, Position b) {
            return new Position(a.x * b.x, a.y * b.y);
        }

        public static Position operator /(Position a, Position b) {
            return new Position(a.x / b.x, a.y / b.y);
        }

        public static Position operator *(float a, Position b) {
            return new Position(a * b.x, a * b.y);
        }

        public static Position operator *(Position a, float b) {
            return new Position(a.x * b, a.y * b);
        }

        public static Position operator /(float a, Position b) {
            return new Position(a / b.x, a / b.y);
        }

        public static Position operator /(Position a, float b) {
            return new Position(a.x / b, a.y / b);
        }

        public static bool operator ==(Position a, Position b) {
            return (a - b).SqrMagnitude < 9.99999943962493E-11;
        }

        public static bool operator !=(Position a, Position b) {
            return !(a == b);
        }
    }
}