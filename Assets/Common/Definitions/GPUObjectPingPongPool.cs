using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Common {
    public class GPUObjectPingPongPool : GPUPool {

        public PingPongBuffer ObjectPingPong => pingpong;

        protected PingPongBuffer pingpong;

        public GPUObjectPingPongPool(int count, Type type) : base(count, type) {
            pingpong = new PingPongBuffer(count, type);
        }

        public override void Dispose() {
            base.Dispose();
            pingpong.Dispose();
        }
    }
}