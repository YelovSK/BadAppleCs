#version 330 core

// In/Out
out vec4 FragColor;
in vec2 fragTexCoord;

// Uniforms
uniform sampler2D texture0;
uniform float time;
uniform vec2 texSize;
uniform vec2 lightPos;
uniform bool softShadows;
uniform int raymarchStepSizePx;
uniform int softShadowSamples;

// Constants
const vec3 LIGHT_COL = vec3(1.0, 0.3, 0.3);
const float LIGHT_AREA_RADIUS = 20.0; // The bigger the radius, the softer the shadows

// https://stackoverflow.com/a/4275343/13822225
float rand(vec2 n) {
	return fract(sin(dot(n, vec2(12.9898, 78.233))) * 43758.5453);
}

// https://gist.github.com/Pikachuxxxx/136940d6d0d64074aba51246f514bd26
vec3 aces(vec3 x) {
  const float a = 2.51;
  const float b = 0.03;
  const float c = 2.43;
  const float d = 0.59;
  const float e = 0.14;
  return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

vec3 srgb(vec3 lin) {
    return pow(lin, vec3(1.0 / 2.2));
}

float getIllumination(vec2 pos, vec2 lightPos) {
    vec2 dir = normalize(lightPos - pos);
    float dist = distance(lightPos, pos);

	int maxSteps = int(dist / raymarchStepSizePx);
	vec2 stepDir = dir * raymarchStepSizePx;

    for (int i = 0; i < maxSteps; ++i) {
		bool isWhite = texelFetch(texture0, ivec2(pos), 0).r > 0.5;
        if (isWhite) {
            return 1.0 - (i * raymarchStepSizePx / dist);
        }

		pos += stepDir;
    }

    // Color of black areas
    return 0.4;
}

float getSoftIllumination(vec2 pos, vec2 lightPos) {
    float totalIllumination = 0.0;
    
    for (int i = 0; i < softShadowSamples; i++) {
        vec2 seed = pos.xy + fract(time) + float(i) * 0.1; 
        float r1 = rand(seed);
        float r2 = rand(seed + vec2(5.2, 1.3));
        float angle = r1 * 2.0 * 3.14159;
        float radius = sqrt(r2) * LIGHT_AREA_RADIUS; 
        
        vec2 randomOffset = vec2(cos(angle), sin(angle)) * radius;

        totalIllumination += getIllumination(pos, lightPos + randomOffset);
    }

    return totalIllumination / float(softShadowSamples);
}

void main() {
	vec2 pos = fragTexCoord * texSize;
    vec4 texCol = texture(texture0, fragTexCoord);
    float luminance = texCol.r;

    float illum = softShadows
        ? getSoftIllumination(pos, lightPos)
        : getIllumination(pos, lightPos);

    float dist = length(pos - lightPos);
    float falloff = 1.0 - (dist / texSize.x / 1.5);
    falloff = max(0.0, falloff * falloff * falloff);

	vec3 lighting = vec3(0.05) + illum * LIGHT_COL * falloff;
	vec3 color = lighting * (luminance + 0.5);

    // Post-processing
    color = aces(color);
    color = srgb(color);

    FragColor = vec4(color, 1.0);
}