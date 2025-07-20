Shader "FFD/Test" 
{

	   CGINCLUDE
	   #include "UnityCG.cginc"

	   StructuredBuffer<float3> gNewPoints;
	   StructuredBuffer<float3> gNewNormals;
	   uniform int _VerticesCount;

       struct VS_OUTPUT
       {
            float4 Position : SV_POSITION;
		    half2 v2Texcoord : TEXCOORD0;
			half3 v3WNormal : TEXCOORD1;
			float3 v3WPos : TEXCOORD2;
       };
            
       VS_OUTPUT MainVS(appdata_full input)
       {
           VS_OUTPUT output = (VS_OUTPUT)0;
		   int vertexID = ceil(DecodeFloatRG(input.texcoord1.xy) * _VerticesCount);
		   float3 points = gNewPoints[vertexID];
		   float3 normal = gNewNormals[vertexID];
           output.Position = mul(UNITY_MATRIX_VP, float4(points,1.0));
           output.v2Texcoord = input.texcoord.xy;
		   output.v3WNormal = normal;
		   output.v3WPos = points;
           return output;
        }
	   
        float4 MainPS(VS_OUTPUT input) : COLOR 
        {   
		    return dot(input.v3WNormal,normalize(_WorldSpaceCameraPos.xyz - input.v3WPos));
        }

         ENDCG
	     SubShader 
	     {
		       Tags { "RenderType"="Opaque" }
		        LOD 200
		        Pass
		        {
			          Name "TestFFD"
			          CGPROGRAM

                      #pragma vertex MainVS
                      #pragma fragment MainPS
                      #pragma target 5.0
           
                      ENDCG
		        }
	    }
		Fallback Off
}
