Shader "UI/CurvedBarrelUV"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BarrelStrength ("桶形强度", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _MainTex_ST;
            float _BarrelStrength;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                float2 uvCentered = (v.uv - 0.5) * 2.0; // [-1, 1]
                float dist = dot(uvCentered, uvCentered); // r^2

                // 桶形扰动方向：向外放射的反方向（中心鼓起）
                float2 offset = uvCentered * dist * _BarrelStrength;

                // 使用这个 offset 改变顶点位置
                float3 pos = v.vertex.xyz;
                pos.xy -= offset * 100.0; // 放大因 UI 单位较大

                o.vertex = UnityObjectToClipPos(float4(pos, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
