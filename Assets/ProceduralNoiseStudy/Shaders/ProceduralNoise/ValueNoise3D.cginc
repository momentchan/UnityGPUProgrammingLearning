#ifndef VALUE_NOISE_3D
#define VALUE_NOISE_3D

float3 pseudoRandom(float3 v)
{
    v = float3(dot(v, float3(127.1, 311.7, 542.3)), dot(v, float3(269.5, 183.3, 461.7)), dot(v, float3(732.1, 845.3, 231.7)));
    return -1.0 + 2.0 * frac(sin(v) * 43758.5453123);
}

float3 interpolate(float3 t) {
    return t * t * (3.0 - 2.0 * t);
}

float valueNoise(float3 x) {
    float3 i = floor(x);
    float3 f = frac(x);

    float3 i000 = i;
    float3 i100 = i + float3(1.0, 0.0, 0.0);
    float3 i010 = i + float3(0.0, 1.0, 0.0);
    float3 i110 = i + float3(1.0, 1.0, 0.0);
    float3 i001 = i + float3(0.0, 0.0, 1.0);
    float3 i101 = i + float3(1.0, 0.0, 1.0);
    float3 i011 = i + float3(0.0, 1.0, 1.0);
    float3 i111 = i + float3(1.0, 1.0, 1.0);

    float n000 = pseudoRandom(i000);
    float n100 = pseudoRandom(i100);
    float n010 = pseudoRandom(i010);
    float n110 = pseudoRandom(i110);
    float n001 = pseudoRandom(i001);
    float n101 = pseudoRandom(i101);
    float n011 = pseudoRandom(i011);
    float n111 = pseudoRandom(i111);

    float3 u = interpolate(f);
    
    return lerp(lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
                lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y),
                u.z);
}
#endif