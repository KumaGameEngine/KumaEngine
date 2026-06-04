#version 450

layout(location = 0) in vec3 Position;
layout(location = 1) out vec4 fragDir;

void main()
{
	fragDir = vec4(Position,1);

	gl_Position = fragDir;
}