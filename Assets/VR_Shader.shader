// Author: Shraddha Agadi
// Email: shraddha.agadi@hs-weingarten.de

Shader "Custom/VR_Shader"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
		[HideInInspector]_FOV("FOV", Range(1, 2)) = 1.6
		[HideInInspector]_Disparity("Disparity", Range(0, 0.3)) = 0.1
		[HideInInspector]_Alpha("Alpha", Range(0, 2.0)) = 1.0
		[HideInInspector]_Magnifier("Magnifier", Range(0, 0.5)) = 0.0
		[HideInInspector]_Zoom("Zoom", Float) = 0.6
		[HideInInspector]_BulgeControl("BulgeControl", Float) = 0.1
		[HideInInspector]_UVCenterOffset("UVCenterOffset", Vector) = (0,0,0,1)
    }
    SubShader

    {
        // No culling or depth
       // Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			float4 _UVCenterOffset;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			float _FOV;
			float _BulgeControl;
			// Alpha is the ratio of pixel density: width to height
			float _Alpha;
			// Disparity is the portion to separate
			// larger disparity cause closer stereovision
			float _Disparity;

			float _Magnifier = 0.5;  ///
			float _Zoom;

            fixed4 frag (v2f i) : SV_Target
            {
				float2 uv1, uv2, uv3, uv4;
				float K1, K2, K3, K4, K5;
				float offset;
				float zoom = _Zoom * _FOV;

				// Split display to left and right eye 
				uv1 = i.uv - 0.5;		///
				uv1.x = uv1.x * 2 - 0.5 + sign(i.uv.x < 0.5);

				// Apply radial distortion to get magnifier effect
				K1 = pow((uv1.x * uv1.x + uv1.y * uv1.y), _Magnifier);

				//min val at 0.5 has no bulge, 0.0 full bulge. inbetween clamp
				//float roof = 0.2;               //
				K1 = clamp(K1, _BulgeControl, 1.0);     ///

				uv1 = (uv1 * K1);		///

				// t1  = k3,
				// mag = _Magnifier
				// k2  = magi
				// t2  = 1.0 / tan(magi)   
				//		= 1.0 / tan(K2)
				//		= 1.0 / tan(1.0 - _Magnifier)
				// uv   = uv* t2 + 0.5
				//		= uv * (1.0 / tan(K2)) + 0.5

				//	fragColor = texture(iChannel1, uv1);

				// Calculate factors for barrel distortion with magnification
				K2 = 1.0 - _Magnifier;  ///
				K3 = sqrt(1.0 - uv1.x * uv1.x - uv1.y * uv1.y);		////
				K4 = 1.0 / (K3 * tan(_FOV * 0.5 * K2));       
			
				// barrel distortion
				uv2 = (uv1 * K4) + 0.5;

				// zoom
				K5 = 1.0 / (zoom * K3 * tan(_FOV * 0.5 * K2));
				uv3 = (uv1 * K5) + 0.5;

				// black color for out-of-range pixels
				if (uv3.x >= 1 || uv3.y >= 1 || uv3.x <= 0 || uv3.y <= 0) {
					return fixed4(0, 0, 0, 1);
				}
				if (uv2.x >= 1 || uv2.y >= 1 || uv2.x <= 0 || uv2.y <= 0) {
					return fixed4(0, 0, 0, 1);
				}
				else {
					offset = 0.5 - _Alpha * 0.5 + _Disparity * 0.5 - _Disparity * sign(i.uv.x < 0.5);
					// uv3 is the remap of image texture
					uv4 = uv3;
					uv4.x = uv3.x * _Alpha + offset;
					return tex2D(_MainTex, uv4);
				}
            }
            ENDCG
        }
    }
	FallBack "Diffuse"
}