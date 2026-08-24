Shader "Custom/WorldHeightStamp"
{
    // Used ONLY by the WorldHeightCamera capture pass, never by anything the player sees
    // directly. Renders a sprite's silhouette using BlendOp Max instead of normal alpha blending
    // — so where two contributors' silhouettes overlap (a tree in front of a house), the TALLER
    // one wins instead of whichever drew last painting over the other. Without this, overlap
    // order would silently corrupt the bake. BlendOp Max combines R and G independently, so this
    // stamps TWO different height values into the same texture in one pass:
    //  - R: the base-to-roof gradient (0 at sprite bottom -> height at top). CloudShadowOverlay
    //    reads this for the cloud warp, where a smooth per-pixel variation looks right.
    //  - G: one flat height across the whole silhouette (no gradient). CloudShadowOverlay reads
    //    THIS for the building-cast shadow march, which needs a value that can't vary within a
    //    single object — marching a couple of steps toward the sun from a building's own base can
    //    land on a higher point of that SAME building's R gradient, reading as the building
    //    shadowing itself. A flat value can't do that: every point in one silhouette compares
    //    equal, so the march only ever finds a DIFFERENT, taller object as an occluder.
    Properties
    {
        _MainTex ("Sprite (alpha = silhouette)",       2D)    = "white" {}
        _Color   ("Height At Top Of Sprite (grayscale)", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One
        BlendOp Max

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4    _Color;

            struct appdata_t { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f       { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed a = tex2D(_MainTex, i.uv).a;
                clip(a - 0.01); // outside the silhouette: don't stamp anything at all

                // R: base-to-roof gradient — cheap, no new art, not true per-pixel depth (an
                // L-shaped roof still just falls on the same straight ramp as a rectangle would).
                // G: flat height, same value everywhere in this silhouette — deliberately NOT a
                // gradient, see header comment above for why the shadow march needs this.
                float gradientHeight = _Color.r * saturate(i.uv.y);
                float flatHeight = _Color.r;
                return fixed4(gradientHeight, flatHeight, 0, 1); // BlendOp Max combines per-channel
            }
            ENDCG
        }
    }
}
