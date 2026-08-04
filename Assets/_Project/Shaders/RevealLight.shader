// Lumière d'entrée en scène — maths preview INVR0 (Bayer + snap radial).
// Squelette UGUI (PixelVeil) ; fragment fidèle à la preview — ne pas réinterpréter.
Shader "ChezArthur/UI/RevealLight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _RectMin ("Rect Min (local px)", Vector) = (0, 0, 0, 0)
        _RectSize ("Rect Size (local px)", Vector) = (1, 1, 0, 0)
        _DitherCellPx ("Dither cell (px)", Float) = 3
        _FocalRect ("Focal UV rect", Vector) = (0.5, 0.5, 0, 0)
        _AspectY ("Aspect Y (h/w)", Float) = 1
        _LightR ("Light radius", Float) = 0
        _LightB ("Light brightness", Float) = 0
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Snap ("Snap progress", Range(0,1)) = 0
        _FrontSoft ("Front soft", Float) = 0.10
        _Flash ("Flash", Float) = 0
        _Vignette ("Vignette", Float) = 0
        _ShadowLevel ("Shadow level", Float) = 0.62
        _Dim ("Dim", Float) = 1

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
                float2 localPos : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _RectMin;
            float4 _RectSize;
            float _DitherCellPx;
            float4 _FocalRect;
            float _AspectY;
            float _LightR;
            float _LightB;
            fixed4 _Tint;
            float _Snap;
            float _FrontSoft;
            float _Flash;
            float _Vignette;
            float _ShadowLevel;
            float _Dim;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // uvRect flipbook déjà appliqué aux UV des sommets — pas de TRANSFORM_TEX
                o.uv = v.texcoord;
                o.localPos = v.vertex.xy;
                o.color = v.color;
                return o;
            }

            float bayer2(float2 a)
            {
                a = floor(a);
                return frac(a.x / 2.0 + a.y * a.y * 0.75);
            }

            float bayer4(float2 a)
            {
                return bayer2(0.5 * a) * 0.25 + bayer2(a);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 rectUv = (i.localPos - _RectMin.xy) / max(_RectSize.xy, float2(1e-4, 1e-4));
                float  b    = bayer4(rectUv * _RectSize.xy / max(_DitherCellPx, 0.001)) + 0.001;
                float  d    = length((rectUv - _FocalRect.xy) * float2(1.0, _AspectY));
                float4 art  = tex2D(_MainTex, i.uv);
                float  luma = dot(art.rgb, float3(0.299, 0.587, 0.114));
                // entrée : pénombre ditherée 3 bandes
                float tl = saturate((_LightR - d) / 0.34);
                float q  = saturate(floor(tl * 3.0 + b) / 3.0);
                float3 col = _Tint.rgb * luma * (0.30 + 0.70 * q) * _LightB
                           + art.rgb * q * _LightB * _ShadowLevel * 0.55;
                // snap : front radial dithered
                float F = _Snap * 1.65;
                float t2 = saturate((F - d) / max(_FrontSoft, 0.0001));
                col = lerp(col, art.rgb, step(b, t2));
                // flash additif au front + global
                col += _Tint.rgb * exp(-abs(d - F) * 9.0) * (_Snap > 0.001 ? 1.0 : 0.0) * _Flash * 0.85;
                col += _Tint.rgb * _Flash * 0.10;
                // vignette + dim ; alpha = vertex color (compat CanvasGroup)
                float v = smoothstep(1.25, 0.42, length((rectUv - 0.5) * float2(1.0, _AspectY)) * 1.35);
                col *= lerp(1.0, v, _Vignette);
                fixed4 outCol = float4(col * _Dim, i.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                outCol.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(outCol.a - 0.001);
                #endif
                return outCol;
            }
            ENDCG
        }
    }
}
