Shader "ShaderBasico"
{
    
    SubShader
    {
        Pass {
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma vertex vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma fragment frag

        struct appdata {
            float4 vertex : POSITION;
            fixed4 color : COLOR;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            fixed4 color : COLOR;
        };
        
        uniform float4x4 _ModelMatrix;
        uniform float4x4 _ViewMatrix;
        uniform float4x4 _ProjectionMatrix;
        

        v2f vert(appdata v) {
            v2f o;
            //o.vertex = unityObjectToClipPos(v.vertex);
            o.vertex = mul(mul(_ProjectionMatrix, mul(_ViewMatrix, _ModelMatrix)), v.vertex);
            o.color = v.color;
            return o;
        }
        
        fixed4 frag(v2f i) : SV_Target{
            return (i.color);
        }

        
        ENDCG

        }
    }

}