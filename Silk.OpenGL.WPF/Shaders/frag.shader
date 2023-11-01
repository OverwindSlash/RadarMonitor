#version 330 core
in vec2 fUv;

uniform sampler2D uTexture0;

uniform int uSection;
uniform int uCell;
uniform vec3 uColor;
uniform int uFadeDuration;
uniform float uNow;
uniform float uEchoRadius;
uniform float uEchoThreshold;

out vec4 FragColor;

const float PI = 3.1415926535897932384626433832795;
float GetAngle(float x, float y)
{
    float angle = atan(y, x);
    if (angle < 0.0f)
        angle = PI * 2 + angle;

    return angle;
}



void main()
{
    float x = fUv.x - 0.5;
    float y = fUv.y - 0.5;
    
    float r = sqrt(x * x + y * y);
    float theta = GetAngle(x, y);
    
    float range = 2 * PI / uSection;
    int row = int(theta / range);
    int col = int(r / (0.5f / uCell));
    
    // attention: col,row
    float g = 0.0;
    if(r< uEchoRadius)
    {
        g = texelFetch(uTexture0, ivec2(col, row), 0).r;
        g = g > uEchoThreshold ? g : 0.0;
        float prev = texelFetch(uTexture0, ivec2(0, row), 0).r;
        float delta = uNow - prev;
        g = (1.0 - delta / float(uFadeDuration * 1000)) * g;
    }
    
    FragColor = vec4(uColor, min(g * 2, 1.0));
    //FragColor = vec4(vec3(0.0, g, 0.0), 1.0);

}