namespace Math
{
    public class Line3D
    {
        private VectorLF3 __src;
        private VectorLF3 __dst;

        public VectorLF3 src
        {
            get
            {
                return __src;
            }
            set
            {
                __src = value;
                ResetDirLine();
            }
        }

        public VectorLF3 dst
        {
            get
            {
                return __dst;
            }
            set
            {
                __dst = value;
                ResetDirLine();
            }
        }

        public VectorLF3 line
        {
            get;
            private set;
        }

        public VectorLF3 dir
        {
            get;
            private set;
        }

        public Line3D() : this(VectorLF3.zero, VectorLF3.zero)
        { }

        public Line3D(VectorLF3 src, VectorLF3 dst)
        {
            __src = src;
            __dst = dst;
            ResetDirLine();
        }

        private void ResetDirLine()
        {
            line = __dst - __src;
            dir = line.normalized;
        }
    }

    public class Plane3D
    {
        public VectorLF3 normal
        {
            get;
            set;
        }

        public VectorLF3 ponit
        {
            get;
            set;
        }

        public Plane3D() : this(VectorLF3.zero, VectorLF3.zero)
        { }

        public Plane3D(VectorLF3 ponit, VectorLF3 normal)
        {
            this.ponit = ponit;
            this.normal = normal;
        }

        public bool IsParallel(Line3D line)
        {
            return VectorLF3.Dot(line.dir, normal) == 0;
        }

        public VectorLF3 GetIntersection(Line3D line)
        {
#if LEGACY
            double rhs = VectorLF3.Dot(__normal, __ponit) - VectorLF3.Dot(__normal, line.src);
#else
            double rhs = VectorLF3.Dot(normal, (ponit - line.src));
#endif
            double lhs = rhs / VectorLF3.Dot(normal, line.dir);

            return line.src + (line.dir * lhs);
        }

        public VectorLF3 GetAnyPoint()
        {
            VectorLF3 point = VectorLF3.zero;

            System.Random rand = new System.Random();
            do
            {
                double r1 = rand.NextDouble() * 1000;
                double r2 = rand.NextDouble() * 1000;

                double rhs = normal.x * (ponit.x - r1) + normal.y * (ponit.y - r2) + normal.z * ponit.z;
                if (-0.00000001 < rhs && rhs < 0.00000001)
                    continue;

                double r3 = rhs / normal.z;

                point.x = r1;
                point.y = r2;
                point.z = r3;

                break;
            } while (true);

            return point;
        }
    }
}