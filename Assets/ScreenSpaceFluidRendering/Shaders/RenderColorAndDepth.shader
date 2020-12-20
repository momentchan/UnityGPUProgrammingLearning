Shader "Hidden/ScreenSpaceFluidRendering/RenderColorAndDepth"
{
	CGINCLUDE
	#include "UnityCG.cginc"
	#include "../../Common/Libs/Definition.cginc"


	struct Particle {
		float3 position;
		float3 velocity;
	};

	struct v2g
	{
		float4 position : SV_POSITION;
	};

	struct g2f
	{
		float4 position : POSITION;
		float  size		: TEXCOORD0;
		float2 uv		: TEXCOORD1;
		float3 vpos		: TEXCOORD2;
	};
	struct fragmentOut
	{
		float depthBuffer  : SV_Target0;
		float depthStencil : SV_Depth;
	};

	StructuredBuffer<Particle> _ParticleDataBuffer;
	float _ParticleSize;
	
	v2g vert(uint id : SV_VertexID)
	{
		v2g o = (v2g)0;
		Particle p = _ParticleDataBuffer[id];
		o.position = float4(p.position, 1.0);
		return o;
	}

	[maxvertexcount(4)]
	void geom(point v2g IN[1], inout TriangleStream<g2f> stream) 
	{
		g2f o = (g2f)0;
		float3 vertPos = IN[0].position.xyz;
		
		[unroll]
		for (int i = 0; i < 4; i++) {
			float3 displace = g_positions[i] * _ParticleSize;
			float3 pos = vertPos + mul(unity_CameraToWorld, displace);
			o.position = UnityObjectToClipPos(float4(pos, 1.0));
			o.uv = g_texcoords[i];
			o.vpos = UnityObjectToViewPos(pos);
			o.size = _ParticleSize;

			stream.Append(o);
		}
		stream.RestartStrip();
	}

	fragmentOut frag(g2f i)
	{

		// Normal
		float3 N = (float3)0;
		N.xy = i.uv.xy * 2.0 - 1.0;
		float radius_sq = dot(N.xy, N.xy);
		if (radius_sq > 1.0) discard;
		N.z = sqrt(1 - radius_sq);

		// Pixel position in clip space
		float4 pixelPos = float4(i.vpos.xyz + N * i.size, 1.0);
		float4 clipPos = mul(UNITY_MATRIX_P, pixelPos);

		// Depth
		float depth = clipPos.z / clipPos.w; // normalize

		fragmentOut o = (fragmentOut)0;

		o.depthBuffer  = depth;
		o.depthStencil = depth;

		return o;
	}
	ENDCG

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "DisableBatching" = "True" "RenderQueue" = "Geometry" }
		Cull Off
		ZWrite On

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			ENDCG
		}
	}
}
