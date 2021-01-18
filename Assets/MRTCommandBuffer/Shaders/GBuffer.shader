Shader "Unlit/GBuffer"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100
		Cull Back

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 normal : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.normal = float4(v.normal,0);
				return o;
			}

			void frag(v2f i,
			out half4 GRT0 :SV_Target0,
			out half4 GRT1 : SV_Target1,
			out half4 GRT2 : SV_Target2,
			out float GRTDepth : SV_Depth
			)
			{
				// sample the texture
				float4 col = tex2D(_MainTex, i.uv);
				GRT0 = col;
				GRT1 = i.normal;
				GRT2 = float4(0,0,1,0);
				GRTDepth = 50;
			}
			ENDCG
		}
	}
}