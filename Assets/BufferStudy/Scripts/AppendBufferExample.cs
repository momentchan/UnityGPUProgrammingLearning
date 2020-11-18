using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BufferStudy {
    public class AppendBufferExample : MonoBehaviour {
        public enum Mode { CS, CG }
        public Mode mode;
        public GameObject cs;
        public GameObject cg;

        void Start() {
            Instantiate(mode == Mode.CS ? cs : cg, transform);
        }
    }
}