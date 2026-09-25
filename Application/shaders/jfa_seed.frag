#version 330 core

out vec4 FragColor;

uniform sampler2D texture0;

// Texels without a seed decode to (65535, 65535), which is far enough away to never be picked as nearest
const vec4 NO_SEED = vec4(1.0);

vec4 encodeSeed(ivec2 p) {
    return vec4(p.x >> 8, p.x & 255, p.y >> 8, p.y & 255) / 255.0;
}

void main() {
    ivec2 p = ivec2(gl_FragCoord.xy);
    bool isWhite = texelFetch(texture0, p, 0).r > 0.5;
    FragColor = isWhite ? encodeSeed(p) : NO_SEED;
}
