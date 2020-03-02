Shader "Custom/Ice" {
	Properties {
		_NormalTex  ("Normal Tex", 2D   ) = "bump" {}
		_Distortion ("Distortion", Float) = 1
	}

	SubShader {
		Tags {
			"Queue"      = "Transparent"
			"RenderType" = "Transparent"
		}

		GrabPass {}
		
		CGPROGRAM
			#pragma target 3.0
			#pragma surface surf Standard fullforwardshadows alpha

			sampler2D _GrabTexture;

			sampler2D _NormalTex;
			float _Distortion;

			struct Input {
				float2 uv_NormalTex;
				float4 screenPos;
				float3 worldNormal;
      			float3 viewDir;
			};

			void surf (Input IN, inout SurfaceOutputStandard o) {
				float2 grabUV = (IN.screenPos.xy / IN.screenPos.w);
				
				//float2 offset = float2(0, _Time.x * 4);
				float2 offset = float2(0, 0);

                // 法線マップで凹凸をつける
                // ディストー＝ションが歪みの強さ
				fixed2 normalTex = UnpackNormal(tex2D(_NormalTex, IN.uv_NormalTex + offset)).rg;
				grabUV += normalTex * _Distortion;

				fixed3 grab = tex2D(_GrabTexture, grabUV).rgb;

				o.Emission = grab;
				o.Albedo   = fixed3(1, 1, 1);
			
			    //輪郭ほど透明度を下げる
		//		float alpha = 1 - (abs(dot(IN.viewDir, IN.worldNormal)));
     	//	    o.Alpha =  alpha*0.9f;
			}
		ENDCG
	}

	FallBack "Transparent/Diffuse"
}