/*
// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color
Shader "Joint Editor" { 
 
	Properties {
		_Color ("Tint (A = Opacity)", Color) = (1,1,1,1) 
		_MainTex ("Texture (A = Transparency)", 2D) = "white" 
		_HotTexture ("Hot Texture (A = Transparency)", 2D) = "white"
		_hot ("Hot", Float) = 0.0
	} 
 
	SubShader {
		Tags {Queue = Transparent}
		Cull Off
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
 
		Pass { 
			SetTexture[_HotTexture] {
				ConstantColor [_Color]
				Combine texture * constant
			} 
			SetTexture[_MainTex] {
				ConstantColor [_Color]
				Combine texture * constant
			} 
		} 
	}
 
}
*/



Shader "Joint Editor" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_HotTex ("Hot Texture (RGB) Trans (A)", 2D) = "white" {}
	_Color ("Tint (A = Opacity)", Color) = (1,1,1,1) 
	_Hot ("Hot", Float) = 0 
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	Cull Off
	ZWrite Off
	ZTest Always
	Blend SrcAlpha OneMinusSrcAlpha 
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			sampler2D _HotTex;
			float4 _MainTex_ST;
			float4 _HotTex_ST;
			float4 _Color;
			float _Hot;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : COLOR
			{
				fixed4 col;
				if(_Hot < 0.5f) {
					col =  tex2D(_MainTex, i.texcoord);
				}
				else {
					col =  tex2D(_HotTex, i.texcoord);
				}
				return col * _Color;
			}
		ENDCG
	}
}

}