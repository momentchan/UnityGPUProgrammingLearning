bool is_dividable_edge(Edge e, uint idx) {
	Particle pa = _Particles[e.a];
	Particle pb = _Particles[e.b];
	return !(pa.links >= _MaxLink && pb.links >= _MaxLink) && is_dividable_particle(pa, e.a) && is_dividable_particle(pb, e.b);
}

void connect(int a, int b) {

	uint eidx = _EdgePoolConsume.Consume();

	InterlockedAdd(_Particles[a].links, 1);
	InterlockedAdd(_Particles[b].links, 1);

	Edge e;
	e.a = a;
	e.b = b;
	e.force = float2(0, 0);
	e.alive = true;
	_Edges[eidx] = e;
}

void divide_edge_closed(uint idx) {

	Edge e = _Edges[idx];
	Particle pa = _Particles[e.a];
	Particle pb = _Particles[e.b];

	if (pa.links == 1 || pb.links == 1) {
		//build triangle
		uint cidx = divide_particle(e.a);
		connect(e.a, cidx);
		connect(cidx, e.b);
	}
	else {
		float2 dir = pb.position - pa.position;
		float2 offset = normalize(dir) * pa.radius * 0.25;
		uint cidx = divide_particle(e.a, offset);

		// connect parent and child
		connect(e.a, cidx);

		// break edge between parent and oppsite 
		// and connect oppsite and child
		InterlockedAdd(_Particles[e.a].links, -1);
		InterlockedAdd(_Particles[cidx].links, 1);
		e.a = cidx;
	}
	_Edges[idx] = e;
}

void divide_edge_branch(uint idx) {

	Edge e = _Edges[idx];
	Particle pa = _Particles[e.a];
	Particle pb = _Particles[e.b];

	// choose particle with fewer links
	uint i = lerp(e.b, e.a, step(pa.links, pb.links));

	uint cidx = divide_particle(i);
	connect(i, cidx);
}