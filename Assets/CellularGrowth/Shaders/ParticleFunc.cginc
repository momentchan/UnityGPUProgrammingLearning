bool is_dividable_particle(Particle p, uint idx) {
	float rate = p.radius / p.threshold;
	return rate > 0.95f;
}

Particle create() {
	Particle p;
	p.position = (float2)0;
	p.velocity = (float2)0;
	p.radius = p.threshold = 1;
	p.links = 0;
	p.alive = true;
	return p;
}

uint divide_particle(uint idx, float2 offset) {
	Particle parent = _Particles[idx];
	Particle child = create();

	// half parent and child's radius
	float hr = parent.radius * 0.5f;
	hr = max(hr, 0.1f);
	parent.radius = child.radius = hr;

	// displace parent and child's positions
	float2 center = parent.position;
	parent.position = center - offset;
	child.position = center + offset;

	// set child max radius
	float x = nrand(float2(_Time, idx));
	child.threshold = hr * lerp(1.25, 2.0, x);

	// set child index from particle pool
	uint cidx = _ParticlePoolConsume.Consume();
	_Particles[cidx] = child;

	// update parent
	_Particles[idx] = parent;

	return cidx;
}

uint divide_particle(uint idx) {
	Particle parent = _Particles[idx];
	float2 offset = random_point_on_circle(float2(idx, _Time)) * 0.1f;
	return divide_particle(idx, offset);
}

//groupshared Particle sharedParticles[SIMULATION_BLOCK_SIZE];
//
//[numthreads(SIMULATION_BLOCK_SIZE, 1, 1)]
//void Update(uint3 id : SV_DispatchThreadID, uint GI : SV_GroupIndex)
//{
//	uint idx = id.x;
//
//	uint count, stride;
//	_ParticlesRead.GetDimensions(count, stride);
//
//	Particle p = _ParticlesRead[idx];
//
//	// Grow
//	if(p.alive)
//		p.radius = min(p.threshold, p.radius + _DT * _Grow * p.radius);
//
//	// Repulsion
//	[loop]
//	for (uint N_block_ID = 0; N_block_ID < (uint)count; N_block_ID += SIMULATION_BLOCK_SIZE) {
//
//		sharedParticles[GI] = _ParticlesRead[N_block_ID + GI];
//
//		GroupMemoryBarrierWithGroupSync();
//
//		for (int N_tile_ID = 0; N_tile_ID < SIMULATION_BLOCK_SIZE; N_tile_ID++) {
//			Particle other = sharedParticles[N_tile_ID];
//
//			if (!p.alive || !other.alive || N_block_ID + N_tile_ID == idx)
//				continue;
//
//			float2 dir = p.position - other.position;
//			float l = length(dir);
//			float r = (p.radius + other.radius) * _Repulsion;
//			if (l < r) {
//				dir += random_point_on_circle(p.position + float2(N_tile_ID, idx)) * step(l, 0.0001);
//				p.velocity += normalize(dir) * (r - l);
//			}
//		}
//		GroupMemoryBarrierWithGroupSync();
//	}
//
//	float vl = length(p.velocity);
//	if (vl > 0) {
//		p.position += normalize(p.velocity) * min(vl, _Limit) * _DT;
//		p.velocity = normalize(p.velocity) * min(vl * _Drag, _Limit);
//	}
//	else {
//		p.velocity = (float2)0;
//	}
//
//	_Particles[idx] = p;
//}