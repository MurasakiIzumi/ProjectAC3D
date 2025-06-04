Shader "Custom/MonitorShader_Basic_UICompatible_NoShake"
{
    Properties
    {
        _MainTex ("RenderTexture", 2D) = "white" {}
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.2
        _ScanlineDensity ("Scanline Density", Range(100, 1000)) = 400

        // UI 所需隐藏属性
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        LOD 100

        Pass
        {
            Name "FOR_UI"
            Tags { "LightMode"="UniversalForward" }

            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask [_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _ScanlineIntensity;
            float _ScanlineDensity;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                fixed4 col = tex2D(_MainTex, uv);

                float scan = sin(i.uv.y * _ScanlineDensity * 3.1415);
                col.rgb *= 1.0 - _ScanlineIntensity * (0.5 + 0.5 * scan);

                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
