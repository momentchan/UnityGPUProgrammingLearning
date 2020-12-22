#ifndef __PARTICLE_INCLUDED_
#define __PARTICLE_INCLUDED_
struct Particle {
    float2 position;
    float2 velocity;
    float radius;
    float threshold;
    int links;
    bool alive;
};
#endif