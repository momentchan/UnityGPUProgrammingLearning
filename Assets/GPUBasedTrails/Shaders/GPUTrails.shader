Shader "GPUBasedTrails/GPUTrails"
{
    Properties
    {
        _Width("Width", Float) = 0.5
        _StartColor("Start Color", Color) = (1,1,1,1)
        _EndColor("End Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "GPUTrails.cginc"

        float _Width;
        float _Life;
        float4 _StartColor;
        float4 _EndColor;
        StructuredBuffer<Trail> _TrailBuffer;
        StructuredBuffer<Node> _NodeBuffer;


        Node GetNode(int trailIdx, int nodeIdx)
        {
            return _NodeBuffer[ToNodeBufferIndex(trailIdx, nodeIdx)];
        }

        struct v2g {
            float4 pos : POSITION0;
            float3 dir : TANGENT0;
            float4 col : COLOR0;
            float4 posNext : POSITION1;
            float3 dirNext : TANGENT1;
            float4 colNext : COLOR1;
        };

        struct g2f {
            float4 pos : SV_POSITION;
            float4 col : COLOR;
        };


        v2g vert(uint id : SV_VertexID, uint instanceId : SV_InstanceID)
        {
            v2g o;
            Trail trail = _TrailBuffer[instanceId];

            int currentNodeIdx = trail.currentNodeIdx;

            Node node0 = GetNode(instanceId, id - 1);
            Node node1 = GetNode(instanceId, id);   //current
            Node node2 = GetNode(instanceId, id + 1);
            Node node3 = GetNode(instanceId, id + 2);

            bool isLastNode = currentNodeIdx == (int)id;

            if (isLastNode || !IsValid(node1)) {
                node0 = node1 = node2 = node3 = GetNode(instanceId, currentNodeIdx);
            }

            float3 pos1 = node1.pos;
            float3 pos0 = IsValid(node0) ? node0.pos : pos1;
            float3 pos2 = IsValid(node2) ? node2.pos : pos1;
            float3 pos3 = IsValid(node3) ? node3.pos : pos2;

            o.pos = float4(pos1, 1);
            o.posNext = float4(pos2, 1);

            o.dir = normalize(pos2 - pos0);
            o.dirNext = normalize(pos3 - pos1);

            float ageRate     = saturate((_Time.y - node1.time) / _Life);
            float ageRateNext = saturate((_Time.y - node2.time) / _Life);
            o.col     = lerp(_StartColor, _EndColor, ageRate);
            o.colNext = lerp(_StartColor, _EndColor, ageRateNext);

            return o;
        }

        [maxvertexcount(4)]
        void geom(point v2g input[1], inout TriangleStream<g2f> outStream) {
            g2f o1, o2, o3, o4;

            float3 pos = input[0].pos;
            float3 dir = input[0].dir;
            float3 posNext = input[0].posNext;
            float3 dirNext = input[0].dirNext;

            float3 camPos = _WorldSpaceCameraPos;
            float3 toCamDir = normalize(camPos - pos);
            float3 sideDir = normalize(cross(toCamDir, dir));
            float3 toCamDirNext = normalize(camPos - posNext);
            float3 sideDirNext = normalize(cross(toCamDirNext, dir));
            
            float width = _Width * 0.5f;

            o1.pos = UnityWorldToClipPos(pos + sideDir * width);
            o2.pos = UnityWorldToClipPos(pos - sideDir * width);
            o3.pos = UnityWorldToClipPos(posNext + sideDirNext * width);
            o4.pos = UnityWorldToClipPos(posNext - sideDirNext * width);


            o1.col = o2.col = input[0].col;
            o3.col = o4.col = input[0].colNext;

            outStream.Append(o1);
            outStream.Append(o2);
            outStream.Append(o3);
            outStream.Append(o4);

            outStream.RestartStrip();
        }

        fixed4 frag(g2f i) : SV_Target
        {
            return i.col;
        }
        ENDCG
        
        Pass
        {
            Cull Off Fog{ Mode Off } ZWrite Off
            Blend One One

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            ENDCG
        }
    }
}
