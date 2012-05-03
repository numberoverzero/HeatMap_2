sampler IntensitySampler : register(s0);
float2 pos;
float radius;
float minPressure;
float maxPressure;

float GetPressureAt(float2 texCoord)
{
	float d = distance(texCoord, pos);
	float invPct = 1 - clamp(d/radius, 0, 1);
	return lerp(minPressure, maxPressure, invPct);
}

float4 PixelShaderF(float2 texCoord : TEXCOORD0) : COLOR0
{
	float baseIntensity = tex2D(IntensitySampler, texCoord).a;
	float pressureOffset = GetPressureAt(texCoord);
	float value = clamp(baseIntensity+pressureOffset, 0, 1);
	return float4(0,0,0,value);
}




technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderF();
    }
}
