Shader "Unlit/GPUClothDebugRenderer"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
	CGINCLUDE

	#include "UnityCG.cginc"

	struct Spring {
		int2 a;
		int2 b;
	};

	struct v2g_mass {
		float4 vertex : POSITION;
	};

	struct g2f_mass {
		float4 position : SV_POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2g_spring {
		float4 vertex0 : POSITION;
		float4 vertex1 : TEXCOORD0;
	};

	struct g2f_spring {
		float4 position : SV_POSITION;
	};

	sampler2D _PositionTex;
	float4 _PositionTex_TexelSize;
	StructuredBuffer<Spring> _SpringBuffer;
	float4 _Color;
	float _ParticleSize;

	static const float3 g_positions[4] =
	{
		float3(-1, 1, 0),
		float3(1, 1, 0),
		float3(-1,-1, 0),
		float3(1,-1, 0),
	};

	static const float2 g_texcoords[4] =
	{
		float2(0, 1),
		float2(1, 1),
		float2(0, 0),
		float2(1, 0),
	};

	v2g_mass vert_mass(uint id : SV_VertexID) {
		v2g_mass o = (v2g_mass)0;
		float2 tr = _PositionTex_TexelSize.zw;
		float2 ts = _PositionTex_TexelSize.xy;

		float2 uv = float2(
			fmod(id, tr.x) * ts.x,
			floor(int(id / tr.x)) * ts.y);
		float3 v = tex2Dlod(_PositionTex, float4(uv.xy, 0, 0)).xyz;
		o.vertex = float4(v.xyz, 1.0);
		return o;
	}

	[maxvertexcount(4)]
	void geom_mass(point v2g_mass IN[1], inout TriangleStream<g2f_mass> outStream) {

		g2f_mass o = (g2f_mass)0;
		float3 vertPos = IN[0].vertex.xyz;
		[unroll]
		for (int i = 0; i < 4; i++) {
			float3 pos = g_positions[i] * _ParticleSize;
			pos = mul(unity_CameraToWorld, pos) + vertPos;
			o.position = UnityObjectToClipPos(float4(pos, 1.0));
			o.uv = g_texcoords[i];
			outStream.Append(o);
		}
		outStream.RestartStrip();
	}

	fixed4 frag_mass(g2f_mass i) : SV_Target
	{
		float2 diff = i.uv.xy - float2(0.5,0.5);
		if (dot(diff, diff) > 0.25)
			discard;
		fixed4 col = _Color;
		return col;
	}

	v2g_spring vert_spring(uint id : SV_VertexID) {
		v2g_spring o = (v2g_spring)0;

		Spring sp = _SpringBuffer[id];
		int2 a = sp.a;
		int2 b = sp.b;

		float2 ts = _PositionTex_TexelSize.xy;

		float2 uv0 = a * ts;
		float2 uv1 = b * ts;

		float3 v0 = tex2Dlod(_PositionTex, float4(uv0.xy, 0, 0)).xyz;
		float3 v1 = tex2Dlod(_PositionTex, float4(uv1.xy, 0, 0)).xyz;

		o.vertex0 = UnityObjectToClipPos(v0);
		o.vertex1 = UnityObjectToClipPos(v1);
		return o;
	}

	[maxvertexcount(2)]
	void geom_spring(point v2g_spring points[1], inout LineStream<g2f_spring> outStream) {

		g2f_spring o = (g2f_spring)0;

		float4 pos0 = points[0].vertex0;
		float4 pos1 = points[0].vertex1;

		o.position = pos0;
		outStream.Append(o);

		o.position = pos1;
		outStream.Append(o);

		outStream.RestartStrip();
	}

	fixed4 frag_spring(g2f_spring i) : SV_Target
	{
		fixed4 col = _Color;
		return col;
	}

	ENDCG
	SubShader
	{
		Tags{ "RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100

		// mass
		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert_mass
			#pragma geometry geom_mass
			#pragma fragment frag_mass
			ENDCG
		}

		// mass
		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert_spring
			#pragma geometry geom_spring
			#pragma fragment frag_spring
			ENDCG
		}
	}
}
