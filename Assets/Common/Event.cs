using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Common {
    [Serializable]
    public class TextureEvent : UnityEvent<RenderTexture> { }
}