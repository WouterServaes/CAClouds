//https://catlikecoding.com/unity/tutorials/basics/compute-shaders/#2.2
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	StructuredBuffer<float3> _CloudPositions;
#endif


void ConfigureProcedural()
{
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		float3 position = _CloudPositions[unity_InstanceID];
		//unity_ObjectToWorld = 0.0;
		unity_ObjectToWorld._m03_m13_m23_m33 = float4(position, 1.0);
#endif
}

//shader graph has 2 precision modes: float and half
void ShaderGraphFunction_float(float3 In, out float3 Out)
{
    Out = In;
}

void ShaderGraphFunction_half(half3 In, out half3 Out)
{
    Out = In;
}