Shader "Custom/CloudShadowOverlay"
{
    // Wind-scrolled cloud coverage overlay, warped by a baked world height map. Cloud POSITION
    // is wind-only — never coupled to the sun. Sun elevation only ever affects overall darkness
    // fading at night, computed on the C# side into _Intensity — see CloudShadowOverlay.cs.
    //
    // History (2026-08-24, see project doc for full detail): a building-cast shadow march using
    // this same height map was tried twice — once with a >=-style test (real self-shadowing
    // bug), once with a margin+bias approximate-match test from a reference video (technically
    // more correct, but only 6 discrete march steps produces a sparse, jagged "outline" pattern
    // instead of a filled shadow — the reference used ~150 steps, which is a real GPU cost this
    // pass doesn't take on). Explicitly dropped, not deferred this time — scope is cloud warp
    // only. If building shadows come back, they need either far more march steps or a different
    // technique entirely, not a tuning pass on this version.
    //
    // Sources WORLD position, not mesh UV — same convention as MeshShadow2D.
    Properties
    {
        _MainTex            ("Cloud Shape (tileable)",                     2D)     = "white" {}
        _ShadowColor        ("Shadow Tint",                                Color)  = (0.55, 0.62, 0.78, 1)
        _Intensity          ("Intensity",                                  Range(0,1)) = 0.5
        _Offset             ("Wind Offset (cloud UV space)",               Vector) = (0,0,0,0)
        _InvTileSize        ("Inverse Cloud Tile Size (1 / world units)",  Float)  = 0.025

        _WorldHeightTex     ("Baked World Height Map",                     2D)     = "black" {}
        _WorldHeightOrigin  ("World Height Map Origin (world space)",      Vector) = (-50,-50,0,0)
        _WorldHeightInvSize ("World Height Map Inverse Size",              Vector) = (0.01,0.01,0,0)
        _CloudWarpUV        ("Cloud Warp At Max Height (UV units, signed)",Float)  = 0.05

        // Debug: when >0.5, frag() bypasses cloud entirely and paints the shader's own heightHere
        // reading straight onto the screen (white = 0, blue = max) via the same multiply blend.
        // Toggle from CloudShadowOverlay.debugVisualizeHeight.
        _DebugVisualizeHeight ("Debug: Visualize Height Map", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent"
               "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        // True multiply blend: final = dst * src. No alpha blending, no grab pass.
        Blend Zero SrcColor

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4    _ShadowColor;
            fixed     _Intensity;
            float4 _Offset;
            float  _InvTileSize;

            sampler2D _WorldHeightTex;
            float2 _WorldHeightOrigin;
            float2 _WorldHeightInvSize;
            float  _CloudWarpUV;
            float  _DebugVisualizeHeight;

            struct appdata_t { float4 vertex : POSITION; };
            struct v2f       { float4 pos : SV_POSITION; float2 worldPos : TEXCOORD0; };

            v2f vert(appdata_t v)
            {
                v2f o;
                // v.vertex is already an ABSOLUTE WORLD position — BuildQuad()/LateUpdate() in
                // CloudShadowOverlay.cs write world coordinates straight into the mesh, not
                // object-local ones. Going world space -> clip space via the view-projection
                // matrix only (instead of UnityObjectToClipPos) means this stays correct
                // regardless of where this GameObject sits in the hierarchy/scene.
                o.pos = mul(UNITY_MATRIX_VP, float4(v.vertex.xyz, 1.0));
                o.worldPos = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // R = base-to-roof gradient (see WorldHeightStamp.shader header). Deliberately
                // the gradient, not the flat .g channel — the warp is exactly the kind of smooth
                // per-pixel variation the gradient was built for.
                float2 heightUV = (i.worldPos - _WorldHeightOrigin) * _WorldHeightInvSize;
                float heightHere = tex2D(_WorldHeightTex, heightUV).r;

                if (_DebugVisualizeHeight > 0.5)
                    return lerp(fixed4(1, 1, 1, 1), fixed4(0, 0, 1, 1), heightHere);

                // Offset the sampled cloud UV straight along Y (the height map's own vertical
                // axis, matching world Y under this fixed top-down camera) by local height — NOT
                // along wind direction. This is deliberately independent of _WindDir/_Offset,
                // which only ever drive the cloud pattern's overall drift; the warp is a separate
                // effect reusing the same "coords" idea. _Offset (wind drift) still accumulates
                // every frame with no wraparound — frac() wraps the whole thing so cloud coverage
                // can't depend on the texture's own Wrap Mode import setting to stay correct.
                // Sign is controlled entirely by CloudWarpUV's own sign — flip it negative in the
                // Inspector if this reads backwards, no shader change needed.
                float2 cloudUV = i.worldPos * _InvTileSize + _Offset.xy;
                cloudUV += float2(0, 1) * (heightHere * _CloudWarpUV);
                fixed cloudCoverage = tex2D(_MainTex, frac(cloudUV)).r;

                return lerp(fixed4(1, 1, 1, 1), _ShadowColor, cloudCoverage * _Intensity);
            }
            ENDCG
        }
    }
}
