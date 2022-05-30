Shader "Billboard/S_BillboardSurface" {
	Properties {
		[NoScaleOffset]_MainTex ("Albedo (RGB)", 2D) = "white" {}
		[HideInInspector]_Offset("CenterOffset",Vector) = (0,0,0)
		[HideInInspector]_BoundSize("BoundSize",Vector) = (0,0,0)
		[HideInInspector]_UV_Y_0("Y_0Y",Vector) = (0,0,0)
		[HideInInspector]_UV_Y_3("Y_3Y",Vector) = (0,0,0)
		[HideInInspector]_UV_Y_1("Y_1Y",Vector) = (0,0,0)
		[HideInInspector]_UV_Z_0("Z_0Y",Vector) = (0,0,0)
		[HideInInspector]_UV_Z_3("Z_3Y",Vector) = (0,0,0)
		[HideInInspector]_UV_Z_1("Z_1Y",Vector) = (0,0,0)
		[HideInInspector]_UV_X_0("X_0Y",Vector) = (0,0,0)
		[HideInInspector]_UV_X_3("X_3Y",Vector) = (0,0,0)
		[HideInInspector]_UV_X_1("X_1Y",Vector) = (0,0,0)

		_SphereNormalSteps("SphereNormal_Steps",Range(1,10)) = 10
		_Radius("SphericalNormal_RadiusMultiplier",Range(0,2)) = 1
		_SOffset("SphericalNormal_Offset",Vector) = (0,0,0)
		[Toggle]_Detail("DetailedNormal_Enabled", float) = 0

		_Blend ("DetailedNormal_Blend",Range(0,1)) = 0.3

		_Precision("DetailedNormal_SampleScale",Range(0,1)) = 0.2

		_DepthX("DetailedNormal_DepthX",Range(0,1)) = 0.5
		_DepthY("DetailedNormal_DepthY",Range(0,1)) = 0.5
		_DepthZ("DetailedNormal_DepthZ",Range(0,1)) = 0.5

		_Slice_Offset("DetailedNormal_Offset",Vector) = (0,0,0)
		_ToonScale("TransitionDither_Scale",Range(0,1)) = 0.3
		

	}
		SubShader{
			Tags { "Queue" = "Transparent" "ForceNoShadowCasting" = "True" "DisableBatching" = "True"}

			LOD 1000
			Cull off
			ZWrite On
			ZTest LEqual
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma vertex vert
		#pragma target 4.0

			sampler2D _MainTex;
			float3 _Offset;
			float3 _BoundSize;
			float _UVX[12];
			float _UVY[12];
			float2	_UV_Y_0;
			float2 	_UV_Y_3;
			float2 	_UV_Y_1;
			float2 	_UV_Z_0;
			float2 	_UV_Z_3;
			float2 	_UV_Z_1;
			float2 	_UV_X_0;
			float2	_UV_X_3;
			float2	_UV_X_1;
			float3 _Slice_Offset;
			float2 ScreenPos;
			float _Precision;
			float _ToonScale;
			float _DepthX;
			float _DepthY;
			float _DepthZ;
			int _SphereNormalSteps;

		float3 vertexColor;
		half vertexFace;
		float _Radius;
		float3 _SOffset;
		float _Detail;
		float _Blend;

		struct Input {
			float2 uv_MainTex;
			float3 objectPos;
			float3 worldPos;
			float3 viewDirection;
			float4 screenPos;
			float3 objNormal;
			float3 VColor : COLOR;
		};
	
		//转换世界坐标系到物体空间坐标系。储存物体空间相机位置并且储存拍摄方向。
		void vert (inout appdata_full v,out Input IN)
		{
			UNITY_INITIALIZE_OUTPUT(Input,IN);
			IN.worldPos = mul (unity_ObjectToWorld, v.vertex).xyz;
			IN.objectPos = v.vertex.xyz;
						
			float3 objectSpaceCameraPos = mul (unity_WorldToObject,float4 (_WorldSpaceCameraPos,1)).xyz;
			//IN.worldViewDirection = IN.worldPos - _WorldSpaceCameraPos;
			IN.viewDirection = v.vertex.xyz - objectSpaceCameraPos;
		}
		//从物体坐标系的像素位置获得该点的UV。注意Z面的U是相反的。
		float2 GetUV (float3 pos)
		{
			float halfX = _BoundSize.x/2;
			float halfY = _BoundSize.y/2;
			float halfZ = _BoundSize.z/2;
			float RColorCheck = vertexColor.r > 0.99?1:0;
			float GColorCheck = vertexColor.g > 0.99?1:0;
			float BColorCheck = vertexColor.b > 0.99?1:0;

			float2 uvX=0;
			float2 uvY=0;
			float2 uvZ=0;

			uvX.x = ((halfZ + pos.z)/(halfZ*2)*(_UV_X_0.x- _UV_X_3.x)+_UV_X_3.x)*RColorCheck;
			uvX.y = ((halfY + pos.y)/(halfY*2)*(_UV_X_0.y- _UV_X_1.y)+_UV_X_1.y)*RColorCheck;

			uvY.x = ((halfX + pos.x)/(halfX*2)*(_UV_Y_0.x - _UV_Y_3.x)+ _UV_Y_3.x)*GColorCheck;
			uvY.y = ((halfZ + pos.z)/(halfZ*2)*(_UV_Y_0.y - _UV_Y_1.y)+ _UV_Y_1.y)*GColorCheck;
			
			uvZ.x = (_UV_Z_0.x- (halfX + pos.x)/(halfX*2)*(_UV_Z_0.x - _UV_Z_3.x))*BColorCheck;
			uvZ.y = ((halfY + pos.y)/(halfY*2)*(_UV_Z_0.y - _UV_Z_1.y)+ _UV_Z_1.y)*BColorCheck;

			return uvX + uvY + uvZ;
		}
		//通过获得的UV来取样贴图的A通道以获得0-1高度信息。
		float Height (float3 pos)
		{
			//sampler2D instance = UNITY_ACCESS_INSTANCED_PROP(_MainTex);
			float4 c = tex2D (_MainTex,GetUV(pos));

			float RColorCheck = vertexColor.r > 0.99?1:0;
			float GColorCheck = vertexColor.g > 0.99?1:0;
			float BColorCheck = vertexColor.b > 0.99?1:0;

			float heightX = 0;
			float heightY = 0;
			float heightZ = 0;

			float DX = lerp(0, _BoundSize.x / 2, _DepthX);
			float DY = lerp(0, _BoundSize.y / 2, _DepthY);
			float DZ = lerp(0, _BoundSize.z / 2, _DepthZ);

			float remapedHeight = 2*(0.5 -c.a);

			heightX = RColorCheck*((remapedHeight) * DX);
			heightY = GColorCheck* ((remapedHeight) * DY);
			heightZ = BColorCheck*((remapedHeight) * DZ);

			return heightX + heightY + heightZ;
		}
	
		//根据定点色区分网格朝向，并在物体坐标系内分情况设置SDF。若取样点在SDF内会导出负值。
		float SDF (float3 pos)
		{
			float distanceX = 0;
			float distanceY = 0;
			float distanceZ = 0;
			float dist = 0;

			float RColorCheck = vertexColor.r > 0.99?1:0;
			float GColorCheck = vertexColor.g > 0.99?1:0;
			float BColorCheck = vertexColor.b > 0.99?1:0;

			float offset = _Slice_Offset.x*_BoundSize.x/2* RColorCheck + _Slice_Offset.y*_BoundSize.y/2*GColorCheck +_Slice_Offset.z*_BoundSize.z/2*BColorCheck;
	
			distanceX =(vertexFace >0? pos.x - Height(pos) -  offset:-pos.x - Height(pos) +  offset) * RColorCheck;
			distanceY =(vertexFace >0? pos.y - Height(pos) -  offset:-pos.y - Height(pos) +  offset) * GColorCheck;
			distanceZ =(vertexFace >0? pos.z - Height(pos) -  offset:-pos.z - Height(pos) +  offset) * BColorCheck;

			//distanceX = ((pos.x >0&&Height(pos)>=0 ||pos.x <0&&Height(pos)<0)?  pos.x - Height(pos)  :-pos.x - Height(pos)) * RColorCheck;
			//distanceY = ((pos.y >0&&Height(pos)>=0 ||pos.y <0&&Height(pos)<0)?  pos.y - Height(pos) :-pos.y - Height(pos)) * GColorCheck;
			//distanceZ = ((pos.z >0&&Height(pos)>=0 ||pos.z <0&&Height(pos)<0)?  pos.z - Height(pos)  :-pos.z - Height(pos)) * BColorCheck;

			return distanceX+distanceY+distanceZ;
		}
	
		float NORMAL_EPSILON ()
		{
			float max = _BoundSize.x > _BoundSize.y?_BoundSize.x :_BoundSize.y;
			float totalMax = max > _BoundSize.z? max: _BoundSize.z;
			float precision = lerp(0.01, totalMax / 10, _Precision);
			return precision;
		}
		//此函数可以在一个SDF内获得它粗略的表面法线。NORMAL_EPSLION 是取样精准度，参照标量是物体Scale为1时的绝对大小。
		//***surface shader 会把法线自动应用于切线坐标系（随物体转动），导入世界坐标系的法线会随物体转动。
		//目前没有什么好办法直接应用world space normal。所以就太极一下用object space normal让两个旋转抵消掉吧。。
		float3 normal(float3 pos){

			float changeX = SDF(pos + float3(NORMAL_EPSILON(), 0, 0)) - SDF(pos - float3(NORMAL_EPSILON(), 0, 0));
			float changeY = SDF(pos + float3(0, NORMAL_EPSILON(), 0)) - SDF(pos - float3(0, NORMAL_EPSILON(), 0));
			float changeZ = SDF(pos + float3(0, 0, NORMAL_EPSILON())) - SDF(pos - float3(0, 0, NORMAL_EPSILON()));

			float3 surfaceNormal = float3(changeX, changeY, changeZ);

			//float3 worldNormal= mul(unity_ObjectToWorld, float4(surfaceNormal, 0)).xyz;
			return normalize(surfaceNormal);
		}
		float sphereSDF(float3 pos)
		{
			return distance(pos,  _SOffset)-(_BoundSize.x+_BoundSize.y+_BoundSize.z)/3*_Radius;
		}
		float3 sphereNormal(float3 pos) {

			float changeX = sphereSDF(pos + float3(NORMAL_EPSILON(), 0, 0)) - sphereSDF(pos - float3(NORMAL_EPSILON(), 0, 0));
			float changeY = sphereSDF(pos + float3(0, NORMAL_EPSILON(), 0)) - sphereSDF(pos - float3(0, NORMAL_EPSILON(), 0));
			float changeZ = sphereSDF(pos + float3(0, 0, NORMAL_EPSILON())) - sphereSDF(pos - float3(0, 0, NORMAL_EPSILON()));

			float3 surfaceNormal = float3(changeX, changeY, changeZ);
			return normalize(surfaceNormal);
		}
		fixed Pattern(float2 UV, float3 pos, float atten) 
		{
			float scale = lerp(1, 200, _ToonScale);
			float2 scaledUV = UV * scale;
			float2 fractUV = frac(scaledUV);
			fractUV -= 0.5;
			float circle = distance(fractUV, 0);
			float depth = 1- tex2D(_MainTex, UV).a;
			float intensity = step(atten , circle);
			return 1- intensity;
		}
		void surf (Input IN, inout SurfaceOutputStandard o) {
			
			//float3 objectCenterWorldPos = mul (unity_ObjectToWorld,float4(0,0,0,1)).xyz;
			//float3 worldPos = IN.worldPos;

			float3 pos = IN.objectPos-_Offset;
			float3 viewDirection = normalize (IN.viewDirection.xyz);
			vertexColor =IN.VColor;
			vertexFace = -dot (vertexColor,viewDirection);

			float progress = 0;
			float sphereProgress = 0;
			float3 col;
			float3 sphereCol;

			float3 samplePoint = pos;
			float3 sphereSamplePoint = pos;


			for (int i = 0; i< _SphereNormalSteps;i++)
			{
			float sphereDistance = sphereSDF(sphereSamplePoint);
			sphereProgress += sphereDistance;
			sphereSamplePoint = pos +sphereProgress*viewDirection;
			}

			float distance = SDF(samplePoint);
			progress += distance;
			samplePoint = pos + progress * viewDirection;
	
			col = normal(samplePoint);
			sphereCol = sphereNormal(sphereSamplePoint);
			normalize(col);
			normalize(sphereCol);

			fixed4 displacedC = tex2D(_MainTex,GetUV(samplePoint));
			fixed4 c = tex2D ( _MainTex, IN.uv_MainTex);
			float angleClip = abs(dot(vertexColor, viewDirection));
			if(displacedC.r  == 1&& displacedC.b  == 1&&displacedC.g == 0
				||c.r  == 1&& c.b  == 1&&c.g == 0 
				||(Pattern(IN.uv_MainTex,pos,angleClip) < 1||angleClip<0.3) && _ToonScale!= 0
				)
			{
				discard;
			}
			o.Normal =_Detail == 0? sphereCol:(col*_Blend+sphereCol*(1-_Blend));
			o.Albedo = displacedC.rgb;
			o.Smoothness =  0.0;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
