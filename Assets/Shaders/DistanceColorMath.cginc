	float4 alpha_from_pos(float4 col, float z) 
	{
		return float4(col.r, col.g, col.b, min(col.a, 5 * col.a - 0.04024144869 * col.a * z));
	}