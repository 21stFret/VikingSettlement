Shader "Custom/CloudShadowOverlay"
{
    // Flat, wind-scrolled cloud coverage overlay. Cloud position AND shape are wind-only —
    // never coupled to the sun. Sun elevation only ever affects overall darkness fading at
    // night, computed on the C# side into _Intensity — see CloudShadowOverlay.cs.
    //
    // Rolled back (2026-08-24) from a baked whole-level height map system (cloud warp near tall
    // objects + building-cast shadows) that hit five separate real bugs across as many rounds and
    // still wasn't fully working. See the project doc for the full history — this flat version is
    // the only state in that whole thread that was ever actually confirmed bug-free.
    //
    // Sources WORLD position, not mesh UV — same convention as MeshShadow2D.
    Properties
    {
        _MainTex     ("Cloud Shape (tileable)",                    2D)     = "white" {}
        _ShadowColor ("Shadow Tint",                               Color)  = (0.55, 0.62, 0.78, 1)
        _Intensity   ("Intensity",                                 Range(0,1)) = 0.5
        _Offset      ("Wind Offset (cloud UV space)",              Vector) = (0,0,0,0)
        _InvTileSize ("Inverse Cloud Tile Size (1 / world units)", Float)  = 0.025
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

            struct appdata_t { float4 vertex : POSITION; };
            struct v2f       { float4 pos : SV_POSITION; float2 worldPos : TEXCOORD0; };

            v2f vert(appdata_t v)
            {
                v2f o;
                // v.vertex is already an ABSOLUTE WORLD position — BuildQuad()/LateUpdate() in
                // CloudShadowOverlay.cs write world coordinates straight into the mesh, not
                // object-local ones. Going world space -> clip space via the view-projection
                // matrix only (instead of UnityObjectToClipPos) means this stays correct
                // regardless of where this GameObject sits in the hierarchy/scene — kept from the
                // height-map rollback, this specific fix was real and unrelated to the parts that
                // got rolled back.
                o.pos = mul(UNITY_MATRIX_VP, float4(v.vertex.xyz, 1.0));
                o.worldPos = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // _Offset accumulates every frame, forever, with no wraparound — frac() wraps it
                // here so cloud coverage can't depend on the texture's own Wrap Mode import
                // setting to stay correct. Also kept from the rollback for the same reason.
                float2 cloudUV = frac(i.worldPos * _InvTileSize + _Offset.xy);
                fixed cloudCoverage = tex2D(_MainTex, cloudUV).r;
                return lerp(fixed4(1, 1, 1, 1), _ShadowColor, cloudCoverage * _Intensity);
            }
            ENDCG
        }
    }
}
