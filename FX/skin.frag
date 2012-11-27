uniform float hasNormalMap;
uniform float hasSpecularMap;
uniform sampler2D diffuseMap;
uniform sampler2D specularMap;
uniform sampler2D normalMap;
varying vec3 lightPos;
varying vec3 halfVector;
varying vec3 vertexPos;
varying vec3 vertexNormal;
varying vec2 texcoord;

mat3 getTangentFrame(vec3 normal, vec3 position, vec2 texCoord)
{
    vec3 dpx = dFdx(position);
    vec3 dpy = dFdy(position);
    vec2 dtx = dFdx(texCoord);
    vec2 dty = dFdy(texCoord);
    
    vec3 tangent = normalize(dpx * dty.t - dpy * dtx.t);
	vec3 binormal = -normalize(dpx * dty.s - dpy * dtx.s);
   
    return mat3(tangent, binormal, normal);
}

void main(void)
{	
	vec3 V = -normalize(vertexPos);
    vec3 L = normalize(lightPos);
    vec3 N = normalize(vertexNormal);
	
	if(hasNormalMap == 1.0)
	{
		// If object has normal map, calculate normal from it instead.
		// vec3 TL = normalize(tangentFrame * L);
		// vec3 TV = normalize(tangentFrame * V);
		mat3 tangentFrame = getTangentFrame(N, V, texcoord);
		N = tangentFrame * normalize(texture2D(normalMap, texcoord).xyz * 2.0 - 1.0);
	}
	float NdotL = clamp(dot(N, L), 0.0, 1.0);
	
    // Calculate the ambient term
    vec4 ambient_color = texture2D(diffuseMap, texcoord) * gl_FrontMaterial.ambient * gl_LightSource[0].ambient + gl_LightModel.ambient * gl_FrontMaterial.ambient;

    // Calculate the diffuse term
    vec4 diffuse_color = texture2D(diffuseMap, texcoord) * gl_FrontMaterial.diffuse * gl_LightSource[0].diffuse;

    // Calculate the specular value
	vec4 specular_color;
	if(hasSpecularMap == 1.0)
	{
		specular_color = texture2D(specularMap, texcoord) * pow(clamp(dot(vertexNormal, halfVector), 0.0, 1.0) , gl_FrontMaterial.shininess);
	}
	else
	{
		specular_color = gl_FrontMaterial.specular * gl_LightSource[0].specular * pow(clamp(dot(vertexNormal, halfVector), 0.0, 1.0) , gl_FrontMaterial.shininess);
    }
	
	// Set the diffuse value (darkness)
	// TODO: add shadows
	float diffuse = NdotL;
	
    // Set the output color of our current pixel
    gl_FragColor = ambient_color + diffuse_color * diffuse + specular_color;
	gl_FragColor.a=texture2D(diffuseMap, texcoord).a;
}
