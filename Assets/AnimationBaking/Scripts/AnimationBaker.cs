using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using mj.gist;
using System.IO;
using UnityEditor;

namespace AnimationBaking {
    public class AnimationBaker : MonoBehaviour {

        [SerializeField] ComputeShader cs;
        [SerializeField] Shader playShader;
        [SerializeField] AnimationClip[] clips;
        [SerializeField] float fps = 0.05f;

        struct VertexInfo {
            public Vector3 postion;
            public Vector3 normal;
        }

        private void LoadAnimations() {
            var animation = GetComponent<Animation>();
            var animator = GetComponent<Animator>();

            if (animation != null) {
                clips = new AnimationClip[animation.GetClipCount()];
                var i = 0;
                foreach (Animation state in animation)
                    clips[i++] = state.clip;
            } else if (animator != null) {
                clips = animator.runtimeAnimatorController.animationClips;
            }
        }

        [ContextMenu("Bake")]
        void Bake() {
            LoadAnimations();
            var skin = GetComponentInChildren<SkinnedMeshRenderer>();
            var vertexNum = skin.sharedMesh.vertexCount;
            var texWidth = Mathf.NextPowerOfTwo(vertexNum);
            var mesh = new Mesh();

            foreach (var clip in clips) {
                var frames = Mathf.NextPowerOfTwo((int)(clip.length / fps));
                var dt = clip.length / frames;
                var infoList = new List<VertexInfo>();

                var pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
                pRt.name = $"{name}.{clip.name}.posTex";
                var nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
                nRt.name = $"{name}.{clip.name}.normTex";

                foreach (var rt in new[] { pRt, nRt }) {
                    rt.enableRandomWrite = true;
                    rt.Create();
                    RenderTexture.active = rt;
                    GL.Clear(true, true, Color.clear);
                }

                for (var i = 0; i < frames; i++) {
                    clip.SampleAnimation(gameObject, dt * i);
                    skin.BakeMesh(mesh);

                    infoList.AddRange(Enumerable.Range(0, vertexNum).
                        Select(idx => new VertexInfo() {
                            postion = mesh.vertices[idx],
                            normal = mesh.normals[idx]
                        })
                    );
                }

                var buffer = new ComputeBuffer(infoList.Count, Marshal.SizeOf(typeof(VertexInfo)));
                buffer.SetData(infoList.ToArray());

                var kernel = new Kernel(cs, "WriteToTexture");

                cs.SetInt("_VertexNum", vertexNum);
                cs.SetBuffer(kernel.Index, "_VertexInfoBuffer", buffer);
                cs.SetTexture(kernel.Index, "_PositionTexture", pRt);
                cs.SetTexture(kernel.Index, "_NormalTexture", nRt);
                cs.Dispatch(kernel.Index, Mathf.CeilToInt(1.0f * vertexNum / kernel.ThreadX),
                                          Mathf.CeilToInt(1.0f * frames / kernel.ThreadY),
                                          1);
                ComputeShaderUtil.ReleaseBuffer(buffer);
#if UNITY_EDITOR
                var folder = "AnimationBaking/BakedAnimationTex";
                var folderPath = CreateFolder("Assets", folder);

                var subFolder = name;
                var subFolderPath = CreateFolder(folderPath, subFolder);

                var posTex = RenderTextureUtil.RenderTextureToTexture2D(pRt);
                var normTex = RenderTextureUtil.RenderTextureToTexture2D(nRt);

                var mat = new Material(playShader);
                mat.SetTexture("_MainTex", skin.sharedMaterial.mainTexture);
                mat.SetTexture("_PosTex", posTex);
                mat.SetTexture("_NormTex", normTex);
                mat.SetFloat("_Length", clip.length);
                if (clip.wrapMode == WrapMode.Loop) {
                    mat.SetFloat("_Loop", 1f);
                    mat.EnableKeyword("ANIM_LOOP");
                }

                var go = new GameObject($"{name}.{clip.name}");
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                go.AddComponent<MeshFilter>().sharedMesh = skin.sharedMesh;

                AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, $"{pRt.name}.asset"));
                AssetDatabase.CreateAsset(normTex, Path.Combine(subFolderPath, $"{nRt.name}.asset"));
                AssetDatabase.CreateAsset(mat, Path.Combine(subFolderPath, $"{name}.{clip.name}.animTex.asset"));
                PrefabUtility.CreatePrefab(Path.Combine(folderPath, go.name + ".prefab").Replace("\\", "/"), go);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
#endif
            }

        }

        string CreateFolder(string folder, string subFolder) {
            var path = Path.Combine(folder, subFolder);
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(folder, subFolder);
            return path;
        }
    }
}