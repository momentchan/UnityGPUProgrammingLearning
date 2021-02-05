#ifndef __NODE_INCLUDED__
#define __NODE_INCLUDED__
struct Node {
    float3 position;
    float t; // (0.0, 1.0)
    float offset; // distance from root
    float mass;
    int from; // branch root index
    bool active;
};
#endif