	float4 alpha_from_pos(float4 col, float z, float zMax) 
	{
		float p = -z / zMax;
		return float4(col.x, col.y, col.z, min(col.a, col.a - col.a * p*p*p*p*p)); // 256 - 256 * (-p.z / 124.25) ^ 5
	}