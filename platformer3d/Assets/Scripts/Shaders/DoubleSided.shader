Shader "DoubleSided" {
	Properties {
	    //_Color ("Main Color", Color) = (1,1,1,1)
	    //_MainTex ("Base (RGB)", 2D) = "white" {}
	    
	    //_BumpMap ("Bump (RGB) Illumin (A)", 2D) = "bump" {}
	    
	    _Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	}
	SubShader {
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		LOD 100    
	    //UsePass "Self-Illumin/VertexLit/BASE"
	    //UsePass "Bumped Diffuse/PPL"

		// Vertex lights
		Pass {
		    Name "BASE"
		    Tags {"LightMode" = "Vertex"}
		    Material {
		        Diffuse [_Color]
		        Emission [_PPLAmbient]
		        Shininess [_Shininess]
		        Specular [_SpecColor]
	        }
		    SeparateSpecular On
		    Lighting On
		    Cull Off
		    Alphatest Greater [_Cutoff]
		    
		    SetTexture [_BumpMap] {
		        constantColor (.5,.5,.5)
		        combine constant lerp (texture) previous
	        }
		    SetTexture [_MainTex] {
		        Combine texture * previous DOUBLE, texture*primary
	        }
	       
			//SetTexture [_MainTex] {
        	//	constantColor [_Color]
        	//	Combine texture * primary DOUBLE, texture * constant
    		//}
		}
	}
	//FallBack "Diffuse", 1
	Fallback "Transparent/Cutout/VertexLit"
}

