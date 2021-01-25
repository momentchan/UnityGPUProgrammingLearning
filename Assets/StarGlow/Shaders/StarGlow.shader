Shader "StarGlow/StarGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
        
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_ST;
        float4 _MainTex_TexelSize;
        ENDCG
            
        // Debug
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_debug

            fixed4 frag_debug(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }

        // Brightness
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_brightness

            float _GlowThreshold;
            float _GlowIntensity;

            fixed4 frag_brightness(v2f_img i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                return max(color - _GlowThreshold, 0) * _GlowIntensity;
            }
            ENDCG
        }
        
        // Blur 
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_blur
            #pragma fragment frag_blur

            float2 _BlurOffset;
            int    _BlurIteration;
            float  _BlurAttenuation;


            struct v2f_blur {
                float4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
                half power : TEXCOORD1;
                half2 offset : TEXCOORD2;
            };

            v2f_blur vert_blur(appdata_img v) {
                v2f_blur o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.texcoord;
                o.power = pow(4, _BlurIteration - 1);
                o.offset = _MainTex_TexelSize.xy * _BlurOffset * o.power;
                return o;
            }

            fixed4 frag_blur(v2f_blur i) : SV_Target
            {
                half4 color = half4(0, 0, 0, 0);
                half2 uv = i.uv;

                for (int j = 0; j < 4; j++) {
                    color += saturate(tex2D(_MainTex, uv) * pow(_BlurAttenuation, i.power * j));
                    uv += i.offset;
                }
                return color;
            }
            ENDCG
        }
        
        // Compose Blurs
        Pass
        {
            Blend OneMinusDstColor One

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_compose_blur

            fixed4 frag_compose_blur(v2f_img i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);;
            }
            ENDCG
        }

        // Compose Origin
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag_compose_origin
            #pragma multi_compile _COMPOSITE_TYPE_ADDITIVE _COMPOSITE_TYPE_SCREEN _COMPOSITE_TYPE_COLORED_ADDITIVE _COMPOSITE_TYPE_COLORED_SCREEN _COMPOSITE_TYPE_DEBUG

            sampler2D _CompositeTex;
            float4 _CompositeColor;

            fixed4 frag_compose_origin(v2f_img i) : SV_Target
            {
                float4 main      = tex2D(_MainTex, i.uv);
                float4 composite = tex2D(_CompositeTex, i.uv);

#if defined(_COMPOSITE_TYPE_COLORED_ADDITIVE) || defined(_COMPOSITE_TYPE_COLORED_SCREEN)
                composite.rgb = (composite.r + composite.g + composite.b) * 0.3333 * _CompositeColor;
#endif

#if   defined(_COMPOSITE_TYPE_SCREEN) || defined(_COMPOSITE_TYPE_COLORED_SCREEN)
                return saturate(main + composite - saturate(main * composite));
#elif defined(_COMPOSITE_TYPE_ADDITIVE) || defined(_COMPOSITE_TYPE_COLORED_ADDITIVE)
                return saturate(main + composite);
#else
                return composite;
#endif
            }
            ENDCG
        }
    }
}
