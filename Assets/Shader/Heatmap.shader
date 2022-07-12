 // Base Shader by Alan Zucconi : www.alanzucconi.com
Shader "Hidden/Heatmap" {
		Properties{
			_HeatTex("Texture", 2D) = "white" {}
			_Bending("Value", Range(0.0,1.0)) = 0.0
			_Amplitude("Amplitude", Float) = 0
			_BoundZ("BoundZ", Float) = 1
		}

SubShader{
	Tags{ 
		  "LightMode" = "ForwardBase"
		}

	Pass{
		Cull Off
		 CGPROGRAM

#pragma vertex vert             
#pragma fragment frag
#include "UnityCG.cginc" // for UnityObjectToWorldNormal
#include "UnityLightingCommon.cginc" // for _LightColor0

		
		struct vertOutput {
			float4 pos		: POSITION;
			
			fixed4 diff		: COLOR0; 

			fixed3 worldPos : TEXCOORD1;
			fixed2 uv		: TEXCOORD0;
		};

		uniform int	   _Points_Length = 0;
		uniform float3 _Points[100];		// (x, y, z) = position
		uniform float2 _Properties[100];	// x = radius, y = intensity

		uniform float  _Bending;
		uniform float  _BoundZ;
		uniform float _Amplitude;
		sampler2D _HeatTex;


		vertOutput vert(appdata_base input){
			vertOutput o;
			
			o.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
			o.pos = UnityObjectToClipPos(input.vertex);

			o.uv	   = input.texcoord;
			half3 worldNormal = UnityObjectToWorldNormal(input.normal);
			half  nl		  = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
			o.diff			  = nl * _LightColor0;
			o.diff.rgb		 += ShadeSH9(half4(worldNormal, 1));

			return o;
		}


		half4 frag(vertOutput output) : SV_TARGET{
			// Loops over all the points
			half h = 0;
			for (int i = 0; i < _Points_Length; i++)
			{
				// Calculates the contribution of each point
				half di = distance(output.worldPos, _Points[i].xyz);
				half ri = _Properties[i].x;
				half hi = 1- saturate(di / ri);

				h += hi * _Properties[i].y;
			}

			// Converts (0-1) according to the heat texture
			h = saturate(h);
			half4 color = tex2D(_HeatTex, fixed2(h, 0.5f));
			color *= output.diff;
			return color;
		}
			ENDCG
	}
}
Fallback "Diffuse"

				
	}