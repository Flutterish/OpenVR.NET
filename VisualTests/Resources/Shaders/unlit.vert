#version 330 core
in vec3 aPos;
in vec2 aUv;
out vec2 uv;

uniform mat4 transform;
uniform mat4 gProj;

void main()
{
    uv = aUv;
    gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0) * transform * gProj;
}