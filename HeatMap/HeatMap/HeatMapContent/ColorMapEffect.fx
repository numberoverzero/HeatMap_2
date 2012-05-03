sampler IntensitySampler : register(s0);
sampler MapSampler : register(s1);

float4 PixelShaderF(float2 texCoord : TEXCOORD0) : COLOR0
{
	float intensityPct = tex2D(IntensitySampler, texCoord).a;
	intensityPct = clamp(intensityPct, 0.001, 0.999);
	float2 colorMapCoord = float2(intensityPct, 0);
	return tex2D(MapSampler, colorMapCoord);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderF();
    }
}
