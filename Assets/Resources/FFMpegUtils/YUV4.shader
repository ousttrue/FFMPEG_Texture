// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/YUV4"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _UTex("UTexture", 2D) = "white" {}
        _VTex("VTexture", 2D) = "white" {}
    }
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
            };

			struct v2f
			{
				float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;			
            sampler2D _UTex;
            float4 _UTex_ST;
            sampler2D _VTex;
            float4 _VTex_ST;


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = TRANSFORM_TEX(v.uv2, _UTex);
                o.uv3 = TRANSFORM_TEX(v.uv3, _VTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                // for testing:
                // show the y image:
                //float4 col = tex2D(_MainTex, i.uv);
                //return float4(col.a, col.a, col.a, 1.0);
                // show the u image:
                //float4 col = tex2D(_UTex, i.uv2);
                //return float4(col.a, col.a, col.a, 1.0);
                // show the v image:
                //float4 col = tex2D(_VTex, i.uv3);
                //return float4(col.a, col.a, col.a, 1.0);

				// sample the textures
                float2 coord;
                coord.x = i.uv.x;
                coord.y = 1 - i.uv.y;
                float4 y = tex2D(_MainTex, coord);
                float4 u = tex2D(_UTex, coord);
                float4 v = tex2D(_VTex, coord);

                 // apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);            
                
                float y_value = y.a;
                float u_value = u.a;
                float v_value = v.a;
                
                // based on https://en.wikipedia.org/wiki/YUV#Y.E2.80.B2UV420sp_.28NV21.29_to_RGB_conversion_.28Android.29
                float r = y_value + (1.370705 * (v_value - 0.5));
                float g = y_value - (0.698001 * (v_value - 0.5)) - (0.337633 * (u_value - 0.5));
                float b = y_value + (1.732446 * (u_value - 0.5));
                
                // clamp the RGB values 0..1
                if (r < 0)
                    r = 0;
                else if (r > 1.0)
                    r = 1.0;
                if (g < 0)
                    g = 0;
                else if (g > 1.0)
                    g = 1.0;
                if (b < 0)
                    b = 0;
                else if (b > 1.0)
                    b = 1.0;

                return float4(r, g, b, 1.0);               
			}
			ENDCG
		}
	}
}
