using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralModeling {

    // Note
    // TreeData: Store tree global paramters
    // TreeBranch: branch units
    // TreeSegment: segment of a TreeBranch

    public class ProceduralTree : ProceduralModelingBase {

        [SerializeField] protected TreeData data;

        [SerializeField, Range(2, 7)] protected int generations = 5;
        [SerializeField, Range(0.5f, 5f)] protected float length = 1f;
        [SerializeField, Range(0.1f, 2f)] protected float radius = 0.15f;

        const float PI2 = Mathf.PI * 2;

        protected override Mesh Build() {
            return Build(data, generations, length, radius);
        }

        private Mesh Build(TreeData data, int generations, float length, float radius) {
            data.Setup();

            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var triangles = new List<int>();
            var root = new TreeBranch(generations, length, radius, data);

            float maxLength = TraverseMaxLength(root);

            Traverse(root, (branch) => {
                var offset = vertices.Count;

                var vOffset = branch.Offset / maxLength;
                var vLength = branch.Length / maxLength;

                for(int i=0; i < branch.Segments.Count; i++) {
                    var t = 1f * i / (branch.Segments.Count - 1);
                    var v = vOffset + vLength * t;

                    var segment = branch.Segments[i];
                    var N = segment.Frame.Normal;
                    var B = segment.Frame.Binormal;
                    for(int j=0; j <= data.radialSegments; j++) {
                        var u = 1f * j / data.radialSegments;
                        var rad = u * PI2;

                        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                        var normal = (cos * N + sin * B).normalized;
                        vertices.Add(segment.Position + segment.Radius * normal);
                        normals.Add(normal);
                        tangents.Add(segment.Frame.Tangent);
                        uv.Add(new Vector2(u, v));
                    }
                }

                for (var i = 0; i < data.heightSegments; i++) {
                    for (var j = 0; j < data.radialSegments; j++) {
                        var a = (data.radialSegments + 1) * i + j;
                        var b = (data.radialSegments + 1) * i + j + 1;
                        var c = (data.radialSegments + 1) * (i + 1) + j;
                        var d = (data.radialSegments + 1) * (i + 1) + j + 1;

                        a += offset;
                        b += offset;
                        c += offset;
                        d += offset;

                        triangles.Add(a); triangles.Add(b); triangles.Add(d);
                        triangles.Add(a); triangles.Add(d); triangles.Add(c);
                    }
                }
            });

            var mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uv.ToArray();
            mesh.tangents = tangents.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();

            return mesh;
        }

        static float TraverseMaxLength(TreeBranch branch) {
            float max = 0;
            branch.Children.ForEach(c => {
                max = Mathf.Max(max, TraverseMaxLength(c));
            });
            return branch.Length + max;
        }

        static void Traverse(TreeBranch from, Action<TreeBranch> action) {
            if (from.Children.Count > 0) {
                from.Children.ForEach(c => {
                    Traverse(c, action);
                });
            }
            action(from);
        }
    }

    [System.Serializable]
    public class TreeData {
        public int randomSeed = 0;
        [Range(0.5f, 0.99f)] public float lengthAttenuation = 0.9f, radiusAttenuation = 0.6f;
        [Range(1, 3)] public int branchesMin = 1, branchesMax = 3;
        [Range(-45f, 0f)] public float growthAngleMin = -15f;
        [Range(0f, 45f)] public float growthAngleMax = 15f;
        [Range(1f, 10f)] public float growthAngleScale = 4f;
        [Range(4, 20)] public int heightSegments = 10, radialSegments = 8;
        [Range(0.0f, 0.35f)] public float bendDegree = 0.1f;

        Rand rnd;

        public void Setup() {
            rnd = new Rand(randomSeed);
        }

        public float Range(float a, float b) {
            return rnd.Range(a, b);
        }

        public int Range(int a, int b) {
            return rnd.Range(a, b);
        }

        public int GetRandomBranches() {
            return rnd.Range(branchesMin, branchesMax + 1);
        }

        public float GetRandomGrowthAngle() {
            return rnd.Range(growthAngleMin, growthAngleMax);
        }

        public float GetRandomBendDegree() {
            return rnd.Range(-bendDegree, bendDegree);
        }
    }

    public class TreeBranch {
        
        public int Generation { get { return generation; } }
        public List<TreeSegment> Segments { get { return segments; } }
        public List<TreeBranch> Children { get { return children; } }

        public Vector3 From { get { return from; } }
        public Vector3 To { get { return to; } }
        public float Length { get { return length; } }
        public float Offset { get { return offset; } }

        int generation;
        List<TreeSegment> segments;
        List<TreeBranch> children;
        Vector3 from, to;
        float fromRadius, toRadius;
        float length;
        float offset;

        public TreeBranch(int generations, float length, float radius, TreeData data): this(generations, generations, Vector3.zero, Vector3.up, Vector3.right, Vector3.back, length, radius, 0f, data) {

        }
        protected TreeBranch(int generation, int generations, Vector3 from, Vector3 tangent, Vector3 normal, Vector3 binormal, float length, float radius, float offset, TreeData data) {
            this.generation = generation;

            this.fromRadius = radius;
            this.toRadius = generation == 0 ? 0 : radius * data.radiusAttenuation;

            this.length = length;
            this.offset = offset;

            this.from = from;
            var scale = Mathf.Lerp(1f, data.growthAngleScale, 1f - 1f * generation / generations);
            var qn = Quaternion.AngleAxis(scale * data.GetRandomGrowthAngle(), normal);   // rotate along normal and binormal axis
            var qb = Quaternion.AngleAxis(scale * data.GetRandomGrowthAngle(), binormal);
            this.to = from + (qn * qb) * tangent * length;

            segments = BuildSegments(data, from, to, fromRadius, toRadius, normal, binormal);

            children = new List<TreeBranch>();
            if (generation > 0) {
                int count = data.GetRandomBranches();
                for (var i = 0; i < count; i++) {
                    float ratio;
                    if (count == 1) {
                        ratio = 1;
                    } else {
                        ratio = Mathf.Lerp(0.5f, 1f, 1f * i / (count - 1));
                    }

                    var index = Mathf.FloorToInt(ratio * (segments.Count - 1));
                    var segment = segments[index];

                    var nt = segment.Frame.Tangent;
                    var nn = segment.Frame.Normal;
                    var nb = segment.Frame.Binormal;

                    var child = new TreeBranch(
                        this.generation - 1,
                        generations,
                        segment.Position,
                        nt,
                        nn,
                        nb,
                        length * Mathf.Lerp(1f, data.lengthAttenuation, ratio),
                        radius * Mathf.Lerp(1f, data.radiusAttenuation, ratio),
                        offset + length,
                        data
                    );
                    children.Add(child);
                }
            }
        }

        List<TreeSegment> BuildSegments(TreeData data, Vector3 from, Vector3 to, float fromDadius, float toRadius, Vector3 normal, Vector3 binormal) {
            var segments = new List<TreeSegment>();

            var curve = new CatmullRomCurve();
            curve.Points.Clear();

            var length = (to - from).magnitude;
            var bend = length * (normal * data.GetRandomBendDegree() + binormal * data.GetRandomBendDegree());
            curve.Points.Add(from);
            curve.Points.Add(Vector3.Lerp(from, to, 0.25f) + bend);
            curve.Points.Add(Vector3.Lerp(from, to, 0.75f) + bend);
            curve.Points.Add(to);

            var frame = curve.ComputeFrenetFrames(data.heightSegments, normal, binormal, false);
            for(int i=0, n = frame.Count; i < n; i++) {
                var u = 1f * i / (n - 1);
                var radius = Mathf.Lerp(fromDadius, toRadius, u);

                var position = curve.GetPointAt(u);
                var segment = new TreeSegment(frame[i], position, radius);
                segments.Add(segment);
            }
            return segments;
        }
    }

    public class TreeSegment {
        public FrenetFrame Frame { get { return frame; } }
        public Vector3 Position { get { return position; } }
        public float Radius { get { return radius; } }

        FrenetFrame frame;
        Vector3 position;
        float radius;

        public TreeSegment(FrenetFrame frame, Vector3 position, float radius) {
            this.frame = frame;
            this.position = position;
            this.radius = radius;
        }
    }

    public class Rand {
        System.Random rnd;

        public float value {
            get { 
                return (float)rnd.NextDouble(); 
            }
        }

        public Rand(int seed) {
            rnd = new System.Random(seed);
        }

        public float Range(float a, float b) {
            var v = value;
            return Mathf.Lerp(a, b, v);
        }

        public int Range(int a, int b) {
            var v = value;
            return Mathf.FloorToInt(Mathf.Lerp(a, b, v));
        }

    }
}