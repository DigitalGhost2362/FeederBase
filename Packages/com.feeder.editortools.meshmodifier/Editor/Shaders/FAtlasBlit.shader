Shader "Hidden/Feeder/AtlasBlit"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Region ("Region (offset xy, size zw)", Vector) = (0, 0, 1, 1)
        _HalfTexel ("Half texel of the tile", Vector) = (0, 0, 0, 0)
        _Tint ("Tint (linear)", Vector) = (1, 1, 1, 1)
        _PackedNormal ("Source is a packed normal map", Float) = 0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off

        CGINCLUDE
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _Region;
        float4 _HalfTexel;
        float4 _Tint;
        float _PackedNormal;

        struct v2f
        {
            float4 pos : SV_POSITION;
            float2 local : TEXCOORD0;
        };

        v2f vert(appdata_img v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.local = v.texcoord.xy;
            return o;
        }

        float4 SampleRegion(float2 local)
        {
            // clamp keeps the padding ring a copy of the tile edge instead of wrapping to the far side
            float2 clamped = clamp(local, _HalfTexel.xy, 1.0 - _HalfTexel.xy);
            float2 uv = _Region.xy + clamped * _Region.zw;
            float2 unclampedUv = _Region.xy + local * _Region.zw;
            return tex2Dgrad(_MainTex, frac(uv), ddx(unclampedUv), ddy(unclampedUv));
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target
            {
                return SampleRegion(i.local) * _Tint;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(v2f i) : SV_Target
            {
                float4 c = SampleRegion(i.local) * _Tint;
                float3 n = _PackedNormal > 0.5 ? UnpackNormal(c) : c.rgb * 2.0 - 1.0;
                n = normalize(n);
                return float4(n * 0.5 + 0.5, 1.0);
            }
            ENDCG
        }
    }
}
