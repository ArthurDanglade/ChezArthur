// Shader UI des transitions d'artwork SSR (Déchéance / Ascension).
// Portage 1:1 du fragment de la preview AW0 — ne pas modifier les constantes.
Shader "ChezArthur/UI/ArtworkTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Frame avant (prime ou dechu)", 2D) = "white" {}
        _BackTex ("Frame arriere", 2D) = "white" {}
        _NoiseTex ("Noise fBm 256 (Point/Repeat)", 2D) = "gray" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _FrontRect ("UV rect frame avant (x,y,w,h)", Vector) = (0,0,1,1)
        _BackRect ("UV rect frame arriere", Vector) = (0,0,1,1)
        _ArtSize ("Taille art en pixels (cellules)", Vector) = (96,128,0,0)
        _PixelSize ("Quantification supplementaire (>=1)", Float) = 1
        _NoiseScale ("Echelle UV du noise", Float) = 1.4

        _Progress ("Progression (0 avant intact, 1 arriere revele)", Range(0,1)) = 0
        _DirMode ("1 = consume depuis le haut", Float) = 1
        _DirWeight ("Poids du gradient directionnel", Range(0,1)) = 0.8
        _Band ("Demi-epaisseur du front (unites h)", Float) = 0.05
        _Hybrid ("0 = or -> 1 = cendre/violet", Range(0,1)) = 0
        _WhiteFront ("Whiteout de la frame avant", Range(0,1)) = 0
        _Bright ("Surbrillance additive", Float) = 0
        _Rim ("Intensite du lisere de bord", Float) = 0
        _RimColor ("Couleur du lisere", Color) = (1,0.83,0.42,1)
        _EmberCool ("Remanence de braise cote revele", Range(0,1)) = 0
        _EdgeGain ("Intensite des bandes du front", Range(0,1)) = 1
        _Jitter ("Tremblement de chaleur (0..1)", Range(0,1)) = 0
        _TimeSeq ("Temps de sequence (pousse par le driver)", Float) = 0

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
            sampler2D _BackTex;
            sampler2D _NoiseTex;
            fixed4 _Color;
            float4 _FrontRect, _BackRect, _ArtSize;
            float _PixelSize, _NoiseScale, _Progress, _DirMode, _DirWeight, _Band;
            float _Hybrid, _WhiteFront, _Bright, _Rim, _EmberCool, _EdgeGain, _Jitter, _TimeSeq;
            fixed4 _RimColor;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Quantification sur la grille de l'art (pixel art : cellules dures)
                float2 grid = _ArtSize.xy / max(_PixelSize, 1.0);
                float2 cell = floor(i.uv * grid);
                float2 quv  = (cell + 0.5) / grid;

                float n    = tex2D(_NoiseTex, quv * _NoiseScale).r;
                float grad = (_DirMode > 0.5) ? (1.0 - quv.y) : quv.y; // zone consumee en premier = h bas
                float hv   = lerp(n, grad, _DirWeight);
                float p    = _Progress * (1.0 + 4.0 * _Band) - 2.0 * _Band;
                float d    = hv - p; // < 0 : consume (arriere) ; > 0 : avant

                // Tremblement de chaleur pixelise (rangees proches du front)
                float2 suv = quv;
                if (_Jitter > 0.001 && abs(d) < _Band * 3.0)
                {
                    float j = hash21(float2(cell.y, floor(_TimeSeq * 24.0))) - 0.5;
                    if (abs(j) > 0.34) suv.x += sign(j) * (_Jitter / grid.x);
                }

                fixed4 front = tex2D(_MainTex, _FrontRect.xy + suv * _FrontRect.zw);
                fixed4 back  = tex2D(_BackTex,  _BackRect.xy  + suv * _BackRect.zw);

                // Palette du front : or SSR -> cendre violette selon _Hybrid
                float3 e0 = lerp(float3(1.00, 0.96, 0.78), float3(0.93, 0.78, 0.95), _Hybrid);
                float3 e1 = lerp(float3(1.00, 0.72, 0.25), float3(0.58, 0.32, 0.80), _Hybrid);
                float3 e2 = lerp(float3(0.85, 0.30, 0.10), float3(0.22, 0.13, 0.32), _Hybrid);

                float3 col = (d < 0.0) ? back.rgb : front.rgb;
                float  a   = (d < 0.0) ? back.a   : front.a;

                // Silhouette blanche (pulses / dechirure du white-out) : frame AVANT uniquement
                if (d >= 0.0) col = lerp(col, float3(1.0, 1.0, 1.0), _WhiteFront);

                // Remanence de braise sur la zone fraichement revelee
                if (d < 0.0)
                {
                    float cool = saturate(-d / (_Band * 5.0));
                    col += e1 * 0.38 * (1.0 - cool) * _EmberCool;
                }

                // Bandes du front (pixel art : bandes DURES, pas de smoothstep)
                float gate = _EdgeGain * step(0.0005, _Progress) * (1.0 - step(0.9995, _Progress));
                if (gate > 0.001)
                {
                    float3 bandCol = col;
                    if      (d > -_Band * 1.8 && d < -_Band * 0.6) bandCol = lerp(col, e2 * 0.55, 0.85); // charbon
                    else if (d >= -_Band * 0.6 && d <  _Band * 0.5) bandCol = e0 * 1.75;                 // coeur chauffe a blanc
                    else if (d >=  _Band * 0.5 && d <  _Band * 1.6) bandCol = lerp(e1 * 1.35, col, 0.22); // braise
                    else if (d >=  _Band * 1.6 && d <  _Band * 2.6) bandCol = lerp(col, e1, 0.38);        // prechauffe
                    col = lerp(col, bandCol, gate);
                }

                // Lisere de carte (2 cellules de bord)
                float2 bpx = min(cell, grid - 1.0 - cell);
                float bd = min(bpx.x, bpx.y);
                if (bd < 2.0) col += _RimColor.rgb * _Rim * (bd < 1.0 ? 1.0 : 0.5);

                col += _Bright * float3(1.0, 0.93, 0.75);

                fixed4 outCol = fixed4(col, a) * i.color;

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
