#ifndef CLASSIC_PERLIN_NOISE_2D
#define CLASSIC_PERLIN_NOISE_2D

float4 mod289(float4 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 permute(float4 x)
{
    return mod289(((x * 34.0) + 1.0) * x);
}

float4 taylorInvSqrt(float4 r)
{
    return 1.79284291400159 - 0.85373472095314 * r;
}

float2 fade(float2 t)
{
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}
float perlinNoise(float2 P)
{
	float4 Pi = floor(P.xyxy) + float4(0.0, 0.0, 1.0, 1.0);
	float4 Pf = frac(P.xyxy) - float4(0.0, 0.0, 1.0, 1.0);
	Pi = mod289(Pi);	
	float4 ix = Pi.xzxz;// 整数部 x i0.x i1.x i2.x i3.x
	float4 iy = Pi.yyww;// 整数部 y i0.y i1.y i2.y i3.y
	float4 fx = Pf.xzxz;// 小数部 x f0.x f1.x f2.x f3.x
	float4 fy = Pf.yyww;// 小数部 y f0.y f1.y f2.y f3.y

	// シャッフルされた勾配のためのインデックスを計算
	float4 i = permute(permute(ix) + iy);

	// 勾配を計算
	// 2次元正軸体（45°回転した四角形）の境界に均一に分散した41個の点
	// 41という数字は、ほどよく分散しかつ、41×7=287と289 に近い数値であるから
	float4 gx = frac(i * (1.0 / 41.0)) * 2.0 - 1.0;
	float4 gy = abs(gx) - 0.5;
	float4 tx = floor(gx + 0.5);
	gx = gx - tx;

	// 勾配ベクトル
	float2 g00 = float2(gx.x, gy.x);
	float2 g10 = float2(gx.y, gy.y);
	float2 g01 = float2(gx.z, gy.z);
	float2 g11 = float2(gx.w, gy.w);

	// 正規化
	float4 norm = taylorInvSqrt(float4(dot(g00, g00), dot(g01, g01), dot(g10, g10), dot(g11, g11)));
	g00 *= norm.x;
	g01 *= norm.y;
	g10 *= norm.z;
	g11 *= norm.w;

	// 勾配ベクトルと各格子点から点Pへのベクトルとの内積
	float n00 = dot(g00, float2(fx.x, fy.x));
	float n10 = dot(g10, float2(fx.y, fy.y));
	float n01 = dot(g01, float2(fx.z, fy.z));
	float n11 = dot(g11, float2(fx.w, fy.w));

	// 補間
	float2 fade_xy = fade(Pf.xy);
	float2 n_x = lerp(float2(n00, n01), float2(n10, n11), fade_xy.x);
	float  n_xy = lerp(n_x.x, n_x.y, fade_xy.y);
	// [-1.0～1.0]の範囲で値を返すように調整
	return 2.3 * n_xy;
}
#endif