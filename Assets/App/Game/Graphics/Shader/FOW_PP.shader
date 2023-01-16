Shader "Post processing/FOW"
{
	Properties
    {
		_NormalTint("Normal color", Color) = (1,1,1,1)
		_FogTint ("Fog tint", Color) = (1,1,1,1)
		_AttackTint ("Attack range tint", Color) = (1,1,1,1)
        _Intensity ("Intensity", float) = 1
        _Offset ("Offset", float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			float _Intensity;
			float _Offset;
			float4 _FogTint;
			float4 _NormalTint;
			float4 _AttackTint;

			int _DataSize;
			vector _Data[1000];


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldDirection : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float4x4 clipToWorld;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float4 clip = float4(o.vertex.xy, 0.0, 1.0);
                o.worldDirection = mul(clipToWorld, clip) - _WorldSpaceCameraPos;

                return o;
            }

            sampler2D_float _CameraDepthTexture;
            float4 _CameraDepthTexture_ST;

			float3 CalculateWorldPos(v2f i)
			{
			    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);
                depth = LinearEyeDepth(depth);
                return i.worldDirection * depth + _WorldSpaceCameraPos;
			}

            float4 frag (v2f i) : SV_Target
            {
				float3 pixelPos = CalculateWorldPos(i);

				float4 originalPixel = tex2D(_MainTex, i.uv);

				float4 attackColor = originalPixel * _AttackTint;
				float4 normalColor = originalPixel;
				float4 dimmedColor = originalPixel * _FogTint;

				float smallestT = 999999;
				float4 finalColor = dimmedColor;
				bool wasAttack = false;

				for(int index = 0; index < _DataSize; index++)
				{
					vector v = _Data[index];
					float2 pos = v.xy;
					float rangeNormal = v.z;
					float rangeAttack = v.w;
					float dist = distance(pixelPos.xz, pos);
				
					if(dist <= rangeNormal)
					{
						bool isAttackRange = dist <= rangeAttack;

						if(isAttackRange)
						{
							finalColor = attackColor;
							wasAttack = true;
						}
						else if(!wasAttack)
							finalColor = normalColor;

						float t = clamp(_Offset + (dist * _Intensity) / (rangeNormal), 0, 1);
						if(t < smallestT)
						{
							smallestT = t;
						}
					}
				}


				float4 col = lerp(finalColor, dimmedColor, smallestT);

				return col;
			
            }
            ENDCG
        }
    }
}