#ifndef __ATTRACTOR_PARTICLE__
#define __ATTRACTOR_PARTICLE__

struct Particle {
    float3 emitPos;
    float3 position;
    float3 velocity;
    float  life;
    float2 size;       // x = current size, y = target size
    float4 color;
};

#endif