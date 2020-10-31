#ifndef __GPUTRAILS_COMMON_INCLUDED__
#define __GPUTRAILS_COMMON_INCLUDED__

struct Trail {
	int currentNodeIdx;
};

struct Node {
	float time;
	float3 pos;
};

struct Input {
	float3 pos;
};

struct Particle
{
	float3 pos;
};

uint _NodeNumPerTrail;

int ToNodeBufferIndex(int trailIdx, int nodeIdx) {
	nodeIdx %= _NodeNumPerTrail;
	return trailIdx * _NodeNumPerTrail + nodeIdx;
}

bool IsValid(Node node) {
	return node.time > 0;
}

#endif