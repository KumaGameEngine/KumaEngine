#version 450

layout(set = 0, binding = 1) uniform Model
{
	mat4 mdl;
	mat3 normalMdl;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;
layout(location = 2) in vec2 UV;

layout(location = 0) out vec2 fUV;
layout(location = 1) out vec4 fCol;

void main()
{
	fUV = UV;
	fCol = Color;

	gl_Position = mdl * vec4(Position,1);
}