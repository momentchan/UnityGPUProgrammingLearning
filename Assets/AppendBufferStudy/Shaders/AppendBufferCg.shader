// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/AppendBufferCg"
{
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }

            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag

            uniform AppendStructuredBuffer<float3> appendBuffer;
            uniform float size;
            uniform float width;

            struct v2f
            {
                float4  pos : SV_POSITION;
                float2  uv : TEXCOORD0;
             };


             v2f vert(appdata_base v)
             {
                 v2f OUT;
                 OUT.pos = UnityObjectToClipPos(v.vertex);
                 OUT.uv = v.texcoord.xy;
                 return OUT;
             }

             float4 frag(v2f IN) : COLOR
             {
                 float3 pos = float3(IN.uv.xy,0);

                 //make pos range from -size to +size
                 pos = (pos - 0.5) * 2.0 * size;

                 //keep z pos at 0
                 pos.z = 0.0;

                 int2 id = IN.uv.xy * width;

                 if (id.x % 2 == 0 && id.y % 2 == 0)
                     appendBuffer.Append(pos);

                 return float4(1,0,0,1);
             }
             ENDCG
        }
    }
}