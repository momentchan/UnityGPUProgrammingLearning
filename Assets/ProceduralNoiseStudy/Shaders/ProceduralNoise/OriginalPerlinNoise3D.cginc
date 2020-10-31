#ifndef ORIGINAL_PERLIN_NOISE_3D
#define ORIGINAL_PERLIN_NOISE_3D

float3 pseudoRandom(float3 v)
{
    v = float3(dot(v, float3(127.1, 311.7, 542.3)), dot(v, float3(269.5, 183.3, 461.7)), dot(v, float3(732.1, 845.3, 231.7)));
    return -1.0 + 2.0 * frac(sin(v) * 43758.5453123);
}

float3 interpolate(float3 t) {
    return t * t * (3.0 - 2.0 * t);
}

float originalPerlinNoise(float3 x) {
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

    float3 p000 = f;
    float3 p100 = f - float3(1.0, 0.0, 0.0);
    float3 p010 = f - float3(0.0, 1.0, 0.0);
    float3 p110 = f - float3(1.0, 1.0, 0.0);
    float3 p001 = f - float3(0.0, 0.0, 1.0);
    float3 p101 = f - float3(1.0, 0.0, 1.0);
    float3 p011 = f - float3(0.0, 1.0, 1.0);
    float3 p111 = f - float3(1.0, 1.0, 1.0);

    float3 g000 = normalize(pseudoRandom(i000));
    float3 g100 = normalize(pseudoRandom(i100));
    float3 g010 = normalize(pseudoRandom(i010));
    float3 g110 = normalize(pseudoRandom(i110));
    float3 g001 = normalize(pseudoRandom(i001));
    float3 g101 = normalize(pseudoRandom(i101));
    float3 g011 = normalize(pseudoRandom(i011));
    float3 g111 = normalize(pseudoRandom(i111));


    float n000 = dot(g000, p000);
    float n100 = dot(g100, p100);
    float n010 = dot(g010, p010);
    float n110 = dot(g110, p110);
    float n001 = dot(g001, p001);
    float n101 = dot(g101, p101);
    float n011 = dot(g011, p011);
    float n111 = dot(g111, p111);

    float3 u = interpolate(f);
    return lerp(lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
           lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y),
           u.z);
}
#endif