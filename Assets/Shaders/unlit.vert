#version 450

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec4 inColor;
layout(location = 2) in vec2 inUv;

layout(binding = 0) uniform ColorInfo {
    vec4 color;
} colorInfo;
layout(binding = 1) uniform TransformInfo {
	mat4 model;
} transformInfo;
layout(binding = 2) uniform CameraInfo {
	mat4 proj;
    mat4 view;
} cameraInfo;

layout(location = 0) out vec4 fragColor;
layout(location = 1) out vec2 uv;

out gl_PerVertex 
{
    vec4 gl_Position;   
};

void main() {
    gl_Position = cameraInfo.proj * cameraInfo.view * transformInfo.model * vec4(inPosition, 1.0);
    fragColor = colorInfo.color * inColor;
    uv = inUv;
}