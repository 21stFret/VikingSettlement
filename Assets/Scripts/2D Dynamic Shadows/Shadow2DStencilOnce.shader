Shader "Custom/Shadow2DStencilOnce"
{
    Properties
    {
        _MainTex    ("Sprite Texture", 2D)    = "white" {}
        _Color      ("Tint",          Color)  = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent"
               "IgnoreProjector"="True" "PreviewType"="Plane" }
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
            fixed4    _Color;

            struct appdata_t { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f       { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                clip(c.a - 0.01);
                return c;
            }
            ENDCG
        }
    }
}
