#ifndef __SKINNED_ATTRACTION_INCLUDED__
#define __SKINNED_ATTRACTION_INCLUDED__
struct SkinnedAttraction {
    float3 position;
    int bone;
    int nearestIndex;
    bool found;
    bool active;
};
#endif