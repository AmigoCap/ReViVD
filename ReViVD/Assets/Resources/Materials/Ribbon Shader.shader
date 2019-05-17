Shader "Custom/Ribbon Shader" {
	Category{
	Tags { "RenderType" = "Opaque" }
	Lighting Off

	SubShader {
		Pass {

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float4 tex0 : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
			};

			v2f vert(appdata_t v)
			{
				v2f o;

				//tex0 carries data necessary for the reorientation of the vertices:
				//x, y, z are the coordinates of the (non-unit) vector from point p to point p+1
				//w is the radius information : it is equal to either -r, 0 or r depending on the vertex (bottom, middle or top vertex of the ribbon)
				if (v.tex0.w != 0)
					o.vertex = UnityObjectToClipPos(v.vertex + normalize(cross(WorldSpaceViewDir(v.vertex), v.tex0.xyz)) * v.tex0.w);
				else
					o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
	}
	}
}
