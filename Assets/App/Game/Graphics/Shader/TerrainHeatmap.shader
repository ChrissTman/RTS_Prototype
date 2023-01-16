// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "*__LNB/TerrainHeatmap" 
{
    Properties 
	{
        _MainTex ("Texture", 2D) = "white" { }
        _Color ("Background color", Color) = (0.5, 0.5, 0.5, 1)
    }
    SubShader 
	{
        Pass 
		{
			Tags { "LightMode" = "ForwardBase" }
			Lighting On


			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			sampler2D _MainTex;
			fixed4 _Color;

			uniform float4 _LightColor0;

			int _DataSize;
			vector _Lines[1000];
			vector _Properties[1000];
			int _EnableOverlay;

			struct v2f 
			{
			    float4 pos : SV_POSITION;
			    float2 uv : TEXCOORD0;
				float4 posWorld : TEXCOORD1;
				float3 normalDir : TEXCOORD2;
			};

			float4 _MainTex_ST;

			v2f vert (appdata_base v)
			{
			    v2f o;
			    o.pos = UnityObjectToClipPos(v.vertex);
			    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.normalDir = normalize( mul( float4( v.normal, 0.0 ), unity_WorldToObject ).xyz );
			    return o;
			}
			/*
			fixed4 calcColor(v2f i)
			{
				fixed4 defaultColor = _Color;//fixed4(0.2,0.2,0.2, 1);

				float minDistance;
				float minInvolvement;
				fixed4 col;

				[loop]
				for (int index = 0; index < _DataSize; index++)
				{
					vector data = _Data[index];					
					//half dist = distance(half2(data.x, data.y), half2(i.uv.x, i.uv.y));
					float dist = distance(data.xy, i.uv.xy);

					data.z = data.z * 50;

					if(index == 0)
					{
						minDistance = dist;
						//minInvolvement = 0;
					}

					if(dist < data.z)
					{
						if(dist > minDistance && index != 0)
						{
							//suck my willy
						}
						else
						{
							minDistance = dist;
							//portion / max
							//15 / 30
							
							minInvolvement = (dist / data.z);
							minInvolvement = saturate(minInvolvement) * 5;
							//col = fixed4(0,0,0,1);

							fixed cTag = data.w;
							if(cTag == 0)
							{
								col = fixed4(1,1,1,1);
							}
							else if(cTag == 1)
							{
								col = fixed4(1,0,0,1);
							}
							else if(cTag == 2)
							{
								col = fixed4(0,1,0,1);
							}
							else if(cTag == 3)
							{
								col = fixed4(0,0,1,1);
							}
							else
							{
								col = fixed4(1,0,1,1);
							}
						}
					}
				}

				if(minInvolvement != 0)
				{
					float t = clamp(minInvolvement, 0, 1);
					fixed4 finalColor = lerp(col, defaultColor, t);
					return finalColor;
					//return col;
				}
				else
				{
					return defaultColor;
				}
			}
			*/

			
			fixed4 calcColor2(v2f i)
			{
				fixed4 defaultColor = fixed4(0,0,0,1);

				fixed4 medianColor;
				int colorEntries;

				[loop]
				for (int index = 0; index < _DataSize; index++)
				{
					vector lineData = _Lines[index];					
					vector props = _Properties[index];

					float distToStart = distance(lineData.xy, i.uv.xy);
					float distToEnd = distance(lineData.zw, i.uv.xy);
					float distStartEnd = distance(lineData.xy, lineData.zw);

					float bias = props.x;
					if(distToStart + distToEnd < distStartEnd + 0.0025)
					{
						fixed4 col;
						float team = props.y;

						if(team == 2)
							col = fixed4(0, 1, 0, 1);
						else if(team == 1)
							col = fixed4(1, 0, 0, 1);
						else
							col = fixed4(1, 0, 1, 1);

						medianColor += col;
						colorEntries++;
					}
				}
				if(colorEntries != 0)
				{
					return medianColor / colorEntries;
				}
				else
				{
					return defaultColor;
				}
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 normalDirection = i.normalDir;
				float3 viewDirection = normalize( _WorldSpaceCameraPos.xyz - i.posWorld.xyz );
				float3 lightDirection;
				float atten;
 
				if(_WorldSpaceLightPos0.w == 0.0){ //directional light
				  atten = 1.0;
				  lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				}
				else{
				  float3 fragmentToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
				  float distance = length(fragmentToLightSource);
				  atten = 1.0/distance;
				  lightDirection = normalize(fragmentToLightSource);
				}
 
				//Lighting
				float3 diffuseReflection = atten * _LightColor0.xyz * saturate(dot(normalDirection, lightDirection));
				//float3 specularReflection = diffuseReflection * _SpecColor.xyz * pow(dot(reflect(-lightDirection, normalDirection), viewDirection));
				  
				float3 lightFinal = (UNITY_LIGHTMODEL_AMBIENT.xyz + diffuseReflection) / 1.7;// + specularReflection;// + rimLighting;

				if(_EnableOverlay == 1)
				{
					float4 overlay = calcColor2(i);
				
					if(overlay.x + overlay.y + overlay.z < 0.01)
						return fixed4(lightFinal, 1);
					else
						return overlay;
				}
				else
					return fixed4(lightFinal, 1);


				
				//return fixed4(d.x,d.y,d.z, 1);//LIGHT_ATTENUATION(i); //calcColor2(i)  
			}
			ENDCG
        }
    }
}