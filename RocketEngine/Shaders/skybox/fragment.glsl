#version 450

layout(set = 0, binding = 0) uniform ProjView
{
    mat4 View;
    mat4 Proj;
};
layout(set = 1, binding = 0) uniform textureCube SkyboxTex; //!
layout(set = 1, binding = 1) uniform sampler SkyboxSamp;    //!

layout(location = 0) in vec4 fragDir;
layout(location = 0) out vec4 outputColor;

void main()
{
    vec4 viewPos = inverse(Proj) * fragDir;
    viewPos /= viewPos.w;

    vec3 rayDir = normalize((inverse(View) * vec4(viewPos.xyz, 0.0)).xyz);

	outputColor = vec4(texture(samplerCube(SkyboxTex, SkyboxSamp), rayDir).xyz,1); //!

    gl_FragDepth = 1.0;
}