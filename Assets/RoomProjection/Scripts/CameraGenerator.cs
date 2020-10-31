using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoomProjection {
    public class CameraGenerator : MonoBehaviour {
        
        public Transform eyePoint;

        [SerializeField] protected float debugPositionOffset = 0;
        [SerializeField] protected float cameraDepth = -10;
        [SerializeField] protected float eyePointToWorldScale = 100f;
        [SerializeField] protected Vector2Int texSize = new Vector2Int(1024, 512);

        private Room room;
        private Dictionary<Room.Face, Camera> faceToCameraDic = new Dictionary<Room.Face, Camera>();

        void Start() {
            room = FindObjectOfType<Room>();
            room.GetFaceData().ForEach(data => {
                var go = new GameObject(data.face.ToString(), typeof(Camera));
                var trans = go.transform;
                trans.SetParent(transform);
                trans.localRotation = Room.FaceToRot(data.face);

                var cam = go.GetComponent<Camera>();
                faceToCameraDic[data.face] = cam;

                var tex = new RenderTexture(texSize.x, texSize.y, 24);
                cam.targetTexture = tex;

                room.faceObjectDic[data.face].GetComponent<Renderer>().material.mainTexture = tex;
            });
        }

        void Update() {
            var eyePointRoomLocal = room.transform.InverseTransformPoint(eyePoint.position);

            room.GetFaceData().ForEach(data => {
                UpdataCamera(faceToCameraDic[data.face], data, eyePointRoomLocal, debugPositionOffset);
            });

            UpdateWorldCameraPos(eyePointRoomLocal);
        }

        private void UpdataCamera(Camera cam, Room.FaceData data, Vector3 eyePointRoomLocal, float debugPositionOffset = 0f) {
            cam.depth = cameraDepth;
            cam.ResetProjectionMatrix();

            var faceSize = data.size;
            cam.aspect = faceSize.x/faceSize.y;

            var positionOffset = Quaternion.Inverse(Room.FaceToRot(data.face)) * -eyePointRoomLocal;
            var distance = data.distance + positionOffset.z;
            cam.fieldOfView = 2f * Mathf.Atan2(faceSize.y * 0.5f, distance) * Mathf.Rad2Deg;

            cam.farClipPlane = distance * 1000f;

            var shift = new Vector2(positionOffset.x / faceSize.x, positionOffset.y / faceSize.y) * 2f;
            var projectionMatrix = cam.projectionMatrix;
            projectionMatrix[0, 2] = shift.x;
            projectionMatrix[1, 2] = shift.y;
            cam.projectionMatrix = projectionMatrix;

            var trans = cam.transform;
            trans.position = transform.position + trans.forward * debugPositionOffset;
        }
        void UpdateWorldCameraPos(Vector3 eyePointRoomLocal) {
            transform.localPosition = eyePointRoomLocal * eyePointToWorldScale;
        }
    }
}