using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoomProjection {
    public class Room : MonoBehaviour {

        private static Dictionary<Face, Quaternion> faceToRotDic = new Dictionary<Face, Quaternion>() {
            { Face.Front, Quaternion.Euler(0, 0, 0) },
            { Face.Right, Quaternion.Euler(0, 90, 0) },
            { Face.Back, Quaternion.Euler(0, 180, 0) },
            { Face.Left, Quaternion.Euler(0, 270, 0) },
            { Face.Bottom, Quaternion.Euler(90, 0, 0) },
        };
        public static Quaternion FaceToRot(Face face) {
            return faceToRotDic[face];
        }

        public Vector3 _size = new Vector3(20f, 5f, 10f);
        private List<FaceData> faceData;
        public List<FaceData> GetFaceData() { 

            if(faceData == null) {

                faceData = new List<FaceData>() {
                    new FaceData(){face = Face.Front, size = new Vector2(_size.x, _size.y), distance = _size.z*0.5f},
                    new FaceData(){face = Face.Right, size = new Vector2(_size.z, _size.y), distance = _size.x*0.5f},
                    new FaceData(){face = Face.Back, size = new Vector2(_size.x, _size.y), distance = _size.z*0.5f},
                    new FaceData(){face = Face.Left, size = new Vector2(_size.z, _size.y), distance = _size.x*0.5f},
                    new FaceData(){face = Face.Bottom, size = new Vector2(_size.x, _size.z), distance = _size.y*0.5f}
                };
            }
            return faceData;
        }
        public Dictionary<Face, GameObject> faceObjectDic = new Dictionary<Face, GameObject>();

        void Awake() {
            var center = Vector3.up * _size.y * 0.5f + transform.position;
            GetFaceData().ForEach(data => {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = data.face.ToString();
                go.layer = gameObject.layer;

                faceObjectDic[data.face] = go;

                var trans = go.transform;
                trans.SetParent(transform);
                var rot = FaceToRot(data.face);
                trans.localScale = new Vector3(data.size.x, data.size.y, 1);
                trans.SetPositionAndRotation(
                    center + rot * Vector3.forward * data.distance,
                    rot
                );
            });
        }

        public class FaceData {
            public Face face;
            public Vector2 size;
            public float distance;
        }

        public enum Face {
            Front,
            Back,
            Left,
            Right,
            Bottom
        }
    }
}