Shader "Custom/Water" {
	Properties {
		_WaveTex1   ("Wave Tex 1" , 2D         ) = "bump" {}
		_WaveTex2   ("Wave Tex 2" , 2D         ) = "bump" {}
		_WaveTiling ("Wave Tiling", Vector     ) = (0, 0, 0, 0)
		_Color      ("Color"      , Color      ) = (1, 1, 1, 1)
		_Glossiness ("Smoothness" , Range(0, 1)) = 0.5
		_Refraction ("Refraction" , Vector     ) = (0, 0, 0, 0)
	}

	SubShader {
		Tags {
			"Queue"      = "Transparent"
			"RenderType" = "Transparent"
		}

		GrabPass {}
		
		CGPROGRAM
			#pragma target 3.0
			#pragma surface surf Standard

			sampler2D _GrabTexture;
			sampler2D _WaveTex1;
			sampler2D _WaveTex2;
			half4 _WaveTiling;
			fixed3 _Color;
			half _Glossiness;
			half4 _Refraction;

			struct Input {
				float2 uv_WaveTex1;
				float4 screenPos;
			};

			void surf (Input IN, inout SurfaceOutputStandard o) {
			    //法線マップの取得 with UVスクロール
				fixed4 waveTex1 = tex2D(_WaveTex1, IN.uv_WaveTex1 * _WaveTiling.x + float2(0, _Time.x * 4) );
				fixed4 waveTex2 = tex2D(_WaveTex2, IN.uv_WaveTex1 * _WaveTiling.y + float2(0, _Time.x * 4) );
				fixed3 normal1 = UnpackNormal(waveTex1);
				fixed3 normal2 = UnpackNormal(waveTex2);
				fixed3 normal = BlendNormals(normal1, normal2);

                //歪みを与える
				fixed3 distortion1 = UnpackScaleNormal(waveTex1, _Refraction.x);
				fixed3 distortion2 = UnpackScaleNormal(waveTex2, _Refraction.y);
				fixed2 distortion = BlendNormals(distortion1, distortion2).rg;

                //背景画像の取得
				half2 grabUV = (IN.screenPos.xy / IN.screenPos.w) * float2(1, -1) + float2(0, 1);
				half3 grab = tex2D(_GrabTexture, grabUV + distortion).rgb * _Color;

				o.Albedo     = fixed3(0, 0, 0);
				o.Emission   = grab;
				o.Metallic   = 0;
				o.Smoothness = _Glossiness;
				o.Normal     = normal;
				o.Alpha      = 1;
			}
		ENDCG
	}

	FallBack "Transparent/Diffuse"
}