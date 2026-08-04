// Voile signature d'invocation — balayage cellules pixel + doigts de noise + lisière chaude neutre.
// Fragment verbatim preview INV0 — ne pas réinterpréter les constantes.
Shader "ChezArthur/UI/PixelVeil"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Noise fBm (Point/Repeat)", 2D) = "gray" {}
        _Cells ("Cellules (cols, rows)", Vector) = (26, 46, 0, 0)
        _Progress ("Progression (0 = absent, 1 = couvert)", Range(0,1)) = 0
        _GlobalAlpha ("Alpha global", Range(0,1)) = 1
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
            "PreviewType"="Plane" "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _Cells;
            float _Progress;
            float _GlobalAlpha;
            fixed4 _Color;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 grid = _Cells.xy;
                float2 cell = floor(i.uv * grid);
                float2 quv  = (cell + 0.5) / grid;

                float n  = tex2D(_NoiseTex, quv * 1.6).r;
                float hv = lerp(n, quv.x, 0.62);              // balayage gauche -> droite + doigts de noise
                float th = _Progress * 1.5 - 0.25;
                float d  = th - hv;
                if (d <= 0.0) return fixed4(0, 0, 0, 0);      // cellule pas encore atteinte

                float shade = (14.0 + tex2D(_NoiseTex, quv * 5.0).r * 14.0) / 255.0;
                fixed3 charcoal = fixed3(shade, shade * 1.05, shade * 1.3);

                fixed4 col = fixed4(charcoal, 1.0);
                if (d < 0.09)                                  // lisiere chaude NEUTRE (pas de couleur de rarete)
                {
                    float e = 0.5 * (1.0 - d / 0.09);
                    col.rgb = lerp(col.rgb, fixed3(0.847, 0.788, 0.690), e);
                }
                col *= i.color * _GlobalAlpha;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
