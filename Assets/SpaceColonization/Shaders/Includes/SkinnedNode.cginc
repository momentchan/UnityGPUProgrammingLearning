#ifndef __SKINNED_NODE_INCLUDED__
#define __SKINNED_NODE_INCLUDED__
struct SkinnedNode {
    float3 position;
    float3 animated;
    int index0;
    float t; // (0.0, 1.0)
    float offset; // distance from root
    float mass;
    int from; // branch root index
    bool active;
};
#endif