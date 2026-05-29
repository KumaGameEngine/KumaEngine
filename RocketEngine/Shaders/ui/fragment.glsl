#version 450

layout(set = 1, binding = 0) uniform texture2D ImageTex;
layout(set = 1, binding = 1) uniform sampler ImageSamp;

layout(location = 0) in vec2 fUV;
layout(location = 1) in vec4 fCol;
layout(location = 0) out vec4 outputColor;

void main()
{
	vec4 col = texture(sampler2D(ImageTex, ImageSamp), fUV);

	outputColor = col * fCol;

	gl_FragDepth = 0.0;
}