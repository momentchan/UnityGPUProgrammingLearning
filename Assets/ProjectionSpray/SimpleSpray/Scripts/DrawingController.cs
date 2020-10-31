using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectionSpray {
    public class DrawingController : MonoBehaviour {

        [SerializeField] protected ProjectionSpray spot;
        [SerializeField] protected ProjectionSpray pinSpot;
        [SerializeField] protected Drawable[] drawables;

        void Update() {
            spot.Color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.05f, 1f), 1f, 1f);

            spot.UpdateDrawingMaterial();
            foreach (var drawable in drawables)
                spot.Draw(drawable);

            if (Input.GetMouseButton(0)) {
                var cam = Camera.main;
                var pos = Input.mousePosition;
                pos.z = 5f;
                pos = cam.ScreenToWorldPoint(pos);
                pinSpot.transform.position = cam.transform.position;
                pinSpot.transform.LookAt(pos);

                pinSpot.Color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.05f, 1f), 1f, 1f);
                pinSpot.UpdateDrawingMaterial();
                foreach (var drawable in drawables)
                    pinSpot.Draw(drawable);
            }
        }
    }
}