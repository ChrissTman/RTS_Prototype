Shader "*__LNB/Minimap" 
{
    Properties 
	{
        _MainTex ("Texture", 2D) = "white" { }
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

			fixed4 frag (v2f i) : SV_Target
			{
				for (int index = 0; index < _DataSize; index++)
				{
					vector data = _Data[index];
					
					half dist = distance(half2(data.x, data.y), half2(i.uv.x, i.uv.y));

					//0 < x < 10
					//if(i.uv.x - data.z < data.x && i.uv.x + data.z > data.x &&
					//   i.uv.y - data.z < data.y && i.uv.y + data.z > data.y)
					if(dist < data.z)
					{
						fixed4 col;
						switch(data.w)
						{
							case 0: 
							{ return fixed4(1,1,1,1);}
							case 1: 
							{ return fixed4(1,0,0,1);}
							case 2: 
							{ return fixed4(0,1,0,1);}
							case 3: 
							{ return fixed4(0,0,1,1);}
						}

						return fixed4(1,0,1,1);
					}
				}
				return fixed4(0.2 ,0.2 , 0.2, 1);
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
			ENDCG
        }
    }
}