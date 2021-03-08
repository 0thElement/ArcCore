	float alpha_from_pos(float4 col, float z) 
	{
		return clamp(5 * col.a + 0.04024144869 * col.a * z, 0, col.a);
	}