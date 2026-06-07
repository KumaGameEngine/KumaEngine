#version 450

layout(set = 0, binding = 0) uniform ProjView
{
    mat4 View;
    mat4 Proj;
};

layout(location = 0) in vec3 Position;
layout(location = 0) out vec3 vRayDir;

void main()
{
    vec4 clipPos = vec4(Position, 1.0);
    gl_Position = clipPos;

    vec4 viewPos = Proj * clipPos;
    viewPos /= viewPos.w;

    vRayDir = (View * vec4(viewPos.xyz, 0.0)).xyz;
}