#version 330 core
in vec3 aPos;
in vec2 aUv;
out vec2 uv;

void main()
{
    uv = aUv;
    gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
}