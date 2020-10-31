using UnityEngine;

namespace ProceduralModeling {
    public class LSystem : MonoBehaviour {
        [SerializeField] protected Color color = Color.black;
        [SerializeField, Range(1, 8)] protected int depth;
        [SerializeField, Range(1f, 5f)] protected float length;
        [SerializeField, Range(0.5f, 0.9f)] protected float attenuation;
        [SerializeField, Range(0f, 90f)] protected float angle;

        Material lineMat;

        private void OnEnable() {
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) {
                Debug.LogError("Shader Hidden/Internal-Colored not founded.");
                return;
            }
            lineMat = new Material(shader);
        }

        private void OnRenderObject() {
            DrawSystem(depth, length);
        }

        private void DrawSystem(int depth, float length) {
            lineMat.SetColor("_Color", color);
            lineMat.SetPass(0);

            DrawFractal(transform.localToWorldMatrix, depth, length);
        }

        private void DrawFractal(Matrix4x4 current, int depth, float length) {
            if (depth <= 0) return;

            GL.MultMatrix(current);
            GL.Begin(GL.LINES);
            GL.Vertex(Vector3.zero);
            GL.Vertex(new Vector3(0f, length, 0f));
            GL.End();

            GL.PushMatrix();
            var ml = current * Matrix4x4.TRS(new Vector3(0f, length, 0f), Quaternion.AngleAxis(angle, Vector3.forward), Vector3.one);
            DrawFractal(ml, depth - 1, length * attenuation);
            GL.PopMatrix();

            GL.PushMatrix();
            var mr = current * Matrix4x4.TRS(new Vector3(0f, length, 0f), Quaternion.AngleAxis(-angle, Vector3.forward), Vector3.one);
            DrawFractal(mr, depth - 1, length * attenuation);
            GL.PopMatrix();
        }
    }
}