Shader "MineTetris/BlockCrackOverlay"
{
    // Crack overlay shader — replicates the Minecraft block-damage crack look.
    // Crack texture: dark crack lines on a light background (no alpha channel needed).
    // Alpha is derived from inverted luminance: dark crack pixels → high alpha (visible),
    // light background pixels → low alpha (transparent). Color is tex.rgb directly,
    // so crack lines appear as dark marks drawn on top of the block.
    // _CrackColor.rgb tints the crack color; _CrackColor.a controls overall intensity.
    //
    // Usage: assign this shader to a runtime Material on any block's crack overlay
    // SpriteRenderer (see BlockRenderer subclasses such as BedrockBlockRenderer).
    Properties
    {
        _MainTex         ("Crack Texture", 2D)              = "white" {}
        _CrackAlpha      ("Crack Opacity", Range(0, 1))     = 1.0
        _BrightnessBoost ("Brightness Boost", Range(1, 10)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent+1"
            "RenderType"     = "Transparent"
            "IgnoreProjector"= "True"
        }

        // Standard alpha blend. Background of the crack texture has near-zero alpha
        // (derived from luminance), so only crack lines are composited onto the block.
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed     _CrackAlpha;
            fixed     _BrightnessBoost;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.color  = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Derive alpha from inverted luminance so the crack texture does not
                // need its own alpha channel (cracks encoded as dark RGB on light RGB).
                //   background (lum ≈ 0.9) → alpha ≈ 0.1 → nearly transparent
                //   crack line  (lum ≈ 0.1) → alpha ≈ 0.9 → visible dark mark
                fixed lum   = dot(tex.rgb, fixed3(0.299, 0.587, 0.114));
                fixed alpha = (1.0 - lum) * tex.a * _CrackAlpha * i.color.a;

                // Brighten the crack lines without changing alpha.
                fixed3 color = saturate(tex.rgb * _BrightnessBoost);
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
