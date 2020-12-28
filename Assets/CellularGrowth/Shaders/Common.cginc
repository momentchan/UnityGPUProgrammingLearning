#ifndef __COMMON_INCLUDED_
#define __COMMON_INCLUDED_
StructuredBuffer<Particle> _ParticlesRead;
RWStructuredBuffer<Particle> _Particles;
AppendStructuredBuffer<uint> _ParticlePoolAppend;
ConsumeStructuredBuffer<uint> _ParticlePoolConsume;

AppendStructuredBuffer<uint> _DividablePoolAppend;
ConsumeStructuredBuffer<uint> _DividablePoolConsume;

RWStructuredBuffer<Edge> _Edges;
AppendStructuredBuffer<uint> _EdgePoolAppend;
ConsumeStructuredBuffer<uint> _EdgePoolConsume;

float _Time;
float _DT;

float _Grow;
float _Drag;
float _Limit;
float _Repulsion;

float2 _EmitPoint;
int _EmitCount;

uint _DivideCount;
int _MaxLink;
float _Spring;
#endif