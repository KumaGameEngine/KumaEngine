#version 450

layout(set = 1, binding = 0) uniform textureCube SkyboxTex; //!
layout(set = 1, binding = 1) uniform sampler SkyboxSamp;    //!

layout(location = 0) in vec3 vRayDir;
layout(location = 0) out vec4 outputColor;

void main()
{
    vec3 rayDir = normalize(vRayDir);

    outputColor = vec4(texture(samplerCube(SkyboxTex, SkyboxSamp), rayDir).xyz, 1.0); //!

    gl_FragDepth = 1.0;
}