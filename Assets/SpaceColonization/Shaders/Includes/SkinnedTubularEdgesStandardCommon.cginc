#include "Includes/Edge.cginc"
#include "Includes/SkinnedNode.cginc"
#include "UnityCG.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardUtils.cginc"

#if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
#define PASS_CUBE_SHADOWCASTER
#endif

StructuredBuffer<SkinnedNode> _Nodes;
StructuredBuffer<Edge> _Edges;
uint _EdgesCount;

float4 _Color, _Emission;
half _Glossiness;
half _Metallic;
float4x4 _Local2World;
half _Thickness;

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    uint vid : SV_VertexID;
};

struct v2g
{
    float4 position : POSITION;
    float2 uv : TEXCOORD0;
    float2 uv2 : TEXCOORD1;
    float3 viewDir : TEXCOORD2;
    float thickness : TEXCOORD3;
    float emission : TEXCOORD4;
    float alpha : COLOR;
};

struct g2f {
    float4 position : SV_POSITION;
#if defined(PASS_CUBE_SHADOWCASTER)
    float3 shadow : TEXCOORD0;
#elif defined(UNITY_PASS_SHADOWCASTER)
#else
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
    half3 ambient : TEXCOORD1;
    float3 wpos : TEXCOORD2;
    float emission : TEXCOORD3;
#endif
};


v2g vert(appdata v, uint iid : SV_InstanceID)
{
    Edge e = _Edges[iid];
    SkinnedNode na = _Nodes[e.a];
    SkinnedNode nb = _Nodes[e.b];

    float3 ap = na.animated;
    float3 bp = nb.animated;
    float3 dir = bp - ap;
    bp = ap + dir * nb.t;
    float3 localPos = lerp(ap, bp, v.vid);
    float3 worldPos = mul(_Local2World, float4(localPos, 1));
    float t = lerp(na.t, nb.t, v.vid);

    v2g o;
    o.position = float4(worldPos, 1);
    o.uv = v.uv;
    o.uv2 = float2(lerp(na.offset, nb.offset, v.vid), 0);
    o.viewDir = WorldSpaceViewDir(float4(worldPos, 1));
    o.alpha = (na.active && nb.active) && (iid < _EdgesCount);
    o.thickness = o.alpha;
    o.emission = smoothstep(1.0, 0.0, t);
    return o;
}

g2f create(float3 wpos, float3 wnrm, float2 uv, float emission) {
    g2f o;
#if defined(PASS_CUBE_SHADOWCASTER)
    o.position = mul(UNITY_MATRIX_VP, float4(wpos, 1));
    o.shadow = wpos - _LightPositionRange.xyz;
#elif defined(UNITY_PASS_SHADOWCASTER)
    float scos = dot(wnrm, normalize(UnityWorldSpaceLightDir(wpos)));
    wpos -= wnrm * unity_LightShadowBias.z * sqrt(1 - scos * scos);
    o.position = UnityApplyLinearShadowBias(UnityWorldToClipPos(float4(wpos, 1)));
#else
    o.position = UnityWorldToClipPos(wpos);
    o.normal = wnrm;
    o.uv = uv;
    o.ambient = ShadeSHPerVertex(wnrm, 0);
    o.wpos = wpos;
    o.emission = emission;
#endif
    return o;
}

[maxvertexcount(64)]
void geom(line v2g IN[2], inout TriangleStream<g2f> outStream) {
    v2g p0 = IN[0];
    v2g p1 = IN[1];

    float3 t = normalize(p1.position - p0.position);
    float3 n = normalize(p0.viewDir);
    float3 bn = cross(t, n);
    n = cross(t, bn);

    float t0 = _Thickness * p0.thickness;
    float t1 = _Thickness * p1.thickness;

    float alpha = p0.alpha;

    float3 v0 = p0.position;
    float3 v1 = lerp(p0.position, p1.position, alpha);

    static const uint rows = 6, cols = 6;
    static const float rows_inv = 1.0 / rows, cols_inv = 1.0 / (cols - 1);

    for (uint col = 0; col < cols; col++) {
        float r_col = (col * cols_inv) * UNITY_TWO_PI;

        float s, c;
        sincos(r_col, s, c);

        float3 normal = normalize(n * c + bn * s);

        float3 w0 = p0.position + normal * t0;
        float3 w1 = p1.position + normal * t1;


        g2f o0 = create(w0, normal, p0.uv2, p1.emission);
        outStream.Append(o0);

        g2f o1 = create(w1, normal, p1.uv2, p1.emission);
        outStream.Append(o1);
    }
    outStream.RestartStrip();

    // half circle
    for (uint row = 0; row < rows; row++) {
        float r_row_0 = (row * rows_inv) * UNITY_HALF_PI;
        float r_row_1 = ((row + 1) * rows_inv) * UNITY_HALF_PI;

        for (uint col = 0; col < cols; col++) {
            float r_col = (col * cols_inv) * UNITY_TWO_PI;

            float s, c;
            sincos(r_col, s, c);

            float3 n0 = normalize(n * c * (1 - sin(r_row_0)) + bn * s * (1 - sin(r_row_0)) + t * sin(r_row_0));
            float3 n1 = normalize(n * c * (1 - sin(r_row_1)) + bn * s * (1 - sin(r_row_1)) + t * sin(r_row_1));

            float3 w0 = v1 + n0 * t1;
            float3 w1 = v1 + n1 * t1;

            g2f o0 = create(w0, n0, p1.uv2, p1.emission);
            outStream.Append(o0);

            g2f o1 = create(w1, n1, p1.uv2, p1.emission);
            outStream.Append(o1);
        }
        outStream.RestartStrip();
    }
}

#if defined(PASS_CUBE_SHADOWCASTER)

half4 frag(g2f input) : SV_Target
{
  float depth = length(input.shadow) + unity_LightShadowBias.x;
  return UnityEncodeCubeShadowDepth(depth * _LightPositionRange.w);
}

#elif defined(UNITY_PASS_SHADOWCASTER)

half4 frag() : SV_Target{ return 0; }

#else

void frag(g2f IN, 
    out half4 outGBuffer0 : SV_Target0,
    out half4 outGBuffer1 : SV_Target1,
    out half4 outGBuffer2 : SV_Target2,
    out half4 outEmission : SV_Target3)
{
    half3 albedo = _Color.rgb;
        
    half3 c_diff, c_spec;
    half refl10;
    c_diff = DiffuseAndSpecularFromMetallic(
        albedo, _Metallic, // input
        c_spec, refl10 // output
    );

    UnityStandardData data;
    data.diffuseColor = c_diff;
    data.occlusion = 1.0;
    data.specularColor = c_spec;
    data.smoothness = _Glossiness;
    data.normalWorld = IN.normal;
    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    half3 sh = ShadeSHPerPixel(data.normalWorld, IN.ambient, IN.wpos);
    outEmission = _Emission * IN.emission + half4(sh * c_diff, 1);
}
#endif



