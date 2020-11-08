using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenSpaceFluidRendering {

    public class SPHFluidSimulationGravity : MonoBehaviour {
        [SerializeField] Camera cam;
        [SerializeField] SPHFluidSimulation simulation;
        [SerializeField] float noiseSpeed = 0.1f;

        void Update() {
            var gravity = new Vector3(-1.0f + 2.0f * Mathf.PerlinNoise(0.0f, Time.time * noiseSpeed),
                                      -1.0f + 2.0f * Mathf.PerlinNoise(1.0f, Time.time * noiseSpeed),
                                      -1.0f + 2.0f * Mathf.PerlinNoise(2.0f, Time.time * noiseSpeed));

            simulation.Gravity = gravity;
            simulation.GravityToCenter = 3.0f * (-1.0f + 2.0f * Mathf.PerlinNoise(3.0f, Time.time * noiseSpeed));

            if (Input.GetMouseButtonDown(0)) {
                simulation.MouseDown = true;
            }

            if (Input.GetMouseButtonUp(0)) {
                simulation.MouseDown = false;
            }

            if (simulation.MouseDown) {
                var mousePosition = Input.mousePosition;
                mousePosition.z = transform.position.z - cam.transform.position.z;
                var worldMouse = cam.ScreenToWorldPoint(mousePosition);
                simulation.MousePosition = worldMouse;
            }
        }
    }
}