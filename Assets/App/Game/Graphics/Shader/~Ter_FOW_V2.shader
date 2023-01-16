Shader "*__LNB/FOW_V2" 
{
    Properties 
	{
        _MainTex ("Texture", 2D) = "white" { }
		_CellIntensity ("Intesity", float) = 0
    }
    SubShader 
	{
        Pass 
		{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _CellSize;
			float _CellIntensity;
			int _DataSize;
			vector _Data[1000];


			struct v2f 
			{
			    float4 pos : SV_POSITION;
			    float2 uv : TEXCOORD0;
			};

			float4 _MainTex_ST;

			v2f vert (appdata_base v)
			{
			    v2f o;
			    o.pos = UnityObjectToClipPos(v.vertex);
			    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
			    return o;
			}

			fixed4 calculateFOW(v2f i)
			{
				fixed4 fullColor = fixed4(1, 1, 1, 1);
				fixed4 noColor = fixed4(.2, .2, .2, 1);

				half shortestDistance = 9999;
				fixed intensity = 0;
				for (int index = 0; index < _DataSize; index++)
				{
					vector data = _Data[index];
					
					half dist = distance(data.xy, i.uv.xy);

					//0 < x < 10
					//if(i.uv.x - _CellSize < data.x && i.uv.x + _CellSize > data.x &&
					//   i.uv.y - _CellSize < data.y && i.uv.y + _CellSize > data.y)

					if(dist < shortestDistance)
					{
						shortestDistance = dist;
						if(dist <= _CellSize)
						{
							intensity = abs(dist - _CellSize);	
							intensity = clamp(intensity * _CellIntensity, 0, 1);
						}
						else
						{
							intensity = 0;
						}
					}
				}
				
				return lerp(noColor, fullColor, intensity);

			    //fixed4 texcol = tex2D (_MainTex, i.uv);
				//if(i.uv.x > 0.5 && i.uv.x < 0.6)
				//{
				//	return fixed4(0,0,0,1);
				//}
				//else	
				//{
				//	return fixed4(i.uv.x, i.uv.y, 0, 1);
				//}
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return calculateFOW(i);
			}
			ENDCG
        }
    }
}