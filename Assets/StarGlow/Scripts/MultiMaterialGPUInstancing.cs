using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarGlow {
    public class MultiMaterialGPUInstancing : MonoBehaviour {

        [SerializeField] protected Mesh mesh;
        [SerializeField] protected Material[] materials;
        public float range;

        public int population;
        private Matrix4x4[] matrices;

        void Start() {
            Debug.Log(mesh.subMeshCount);
            matrices = new Matrix4x4[population];
            Vector4[] colors = new Vector4[population];

            for (int i = 0; i < population; i++) {
                // Build matrix.
                Vector3 position = new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));
                Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
                Vector3 scale = transform.localScale;

                var mat = Matrix4x4.TRS(position, rotation, scale);

                matrices[i] = mat;

                colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
            }
        }

        void Update() {
            for (var i = 0; i < materials.Length; i++) {
                Graphics.DrawMeshInstanced(mesh, i, materials[i], matrices, population);
            }
        }
    }
}