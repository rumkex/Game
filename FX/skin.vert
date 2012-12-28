#define MAX_BONES 60

attribute vec3 inVertex;
attribute vec3 inNormal;
attribute vec2 inTexCoord;
attribute vec4 inBones;
attribute vec4 inWeights;
uniform float animated;
uniform mat4 Bones[MAX_BONES];
varying vec3 lightPos;
varying vec3 halfVector;
varying vec3 vertexPos;
varying vec3 vertexNormal;
varying vec2 texcoord;
// varying mat4 skin;

void main(void) 
{    	 
	// calculate skin matrix if skinning is enabled (for consequent translation of vertices)
	mat4 skin = mat4(1.0);
	if (animated == 0.0)
		skin = mat4(1.0);
	else
	{
		skin = mat4(0.0);
		for (int i = 0; i < 4; i++)
		{
			skin += Bones[int(inBones[i])] * inWeights[i];
		}
	}
    		
    // Calculate the normal value for this vertex, in world coordinates (multiply by gl_NormalMatrix and rotate
    // using skin matrix)
    vertexNormal = normalize(gl_NormalMatrix * (mat3(skin[0].xyz,skin[1].xyz,skin[2].xyz) * inNormal));	
	vec4 v = skin * vec4(inVertex, 1.0);
    // Calculate the light position for this vertex
	halfVector = normalize(gl_LightSource[0].halfVector.xyz);
	vertexPos = (gl_ModelViewMatrix * v).xyz;
    lightPos = gl_LightSource[0].position.xyz - vertexPos;
	
    // Set the position of the current vertex
    gl_Position = gl_ModelViewProjectionMatrix * v;    
    
	texcoord = inTexCoord;
}
