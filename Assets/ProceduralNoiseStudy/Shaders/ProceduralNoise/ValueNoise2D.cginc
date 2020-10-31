#ifndef VALUE_NOISE_2D
#define VALUE_NOISE_2D

float2 pseudoRandom(float2 v)
{
    v = float2(dot(v, float2(127.1, 311.7)), dot(v, float2(269.5, 183.3)));
    return -1.0 + 2.0 * frac(sin(v) * 43758.5453123);
}

float2 interpolate(float2 t) {
    return t * t * (3.0 - 2.0 * t);
}

float valueNoise(float2 x) {
    float2 i = floor(x);
    float2 f = frac(x);

    float2 i00 = i;
    float2 i01 = i + float2(0.0, 1.0);
    float2 i10 = i + float2(1.0, 0.0);
    float2 i11 = i + float2(1.0, 1.0);

    float n00 = pseudoRandom(i00);
    float n01 = pseudoRandom(i01);
    float n10 = pseudoRandom(i10);
    float n11 = pseudoRandom(i11);

    float2 u = interpolate(f);

    return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y);
}
#endif