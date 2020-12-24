#ifndef __EDGE_INCLUDED_
#define __EDGE_INCLUDED_
struct Edge {
    int a, b;        // particle index connecting together
    float2 force;
    bool alive;
};
#endif