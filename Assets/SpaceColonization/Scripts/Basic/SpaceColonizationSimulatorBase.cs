using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceColonization {
    public abstract class SpaceColonizationSimulatorBase : MonoBehaviour {
        public bool IsReady { get; protected set; }
        public int BufferSize { get; protected set; }
        public abstract ComputeBuffer GetNodeBuffer();
        public abstract ComputeBuffer GetEdgeBuffer();
        public int EdgesCount { get; protected set; }
        public int NodesCount { get; protected set; }

    }
}