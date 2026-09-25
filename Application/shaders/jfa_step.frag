#version 330 core

out vec4 FragColor;

uniform sampler2D texture0;
uniform int jumpStep;

vec4 encodeSeed(ivec2 p) {
    return vec4(p.x >> 8, p.x & 255, p.y >> 8, p.y & 255) / 255.0;
}

ivec2 decodeSeed(vec4 c) {
    ivec4 b = ivec4(round(c * 255.0));
    return ivec2(b.r << 8 | b.g, b.b << 8 | b.a);
}

void main() {
    ivec2 p = ivec2(gl_FragCoord.xy);
    ivec2 size = textureSize(texture0, 0);

    ivec2 bestSeed = decodeSeed(texelFetch(texture0, p, 0));
    vec2 bestDelta = vec2(bestSeed - p);
    float bestDistSq = dot(bestDelta, bestDelta);

    for (int y = -1; y <= 1; ++y) {
        for (int x = -1; x <= 1; ++x) {
            ivec2 q = p + ivec2(x, y) * jumpStep;
            if (any(lessThan(q, ivec2(0))) || any(greaterThanEqual(q, size))) {
                continue;
            }

            ivec2 seed = decodeSeed(texelFetch(texture0, q, 0));
            vec2 delta = vec2(seed - p);
            float distSq = dot(delta, delta);
            if (distSq < bestDistSq) {
                bestDistSq = distSq;
                bestSeed = seed;
            }
        }
    }

    FragColor = encodeSeed(bestSeed);
}
