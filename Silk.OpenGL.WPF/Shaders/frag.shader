#version 330 core
in
vec2 fUv;

uniform sampler2D uTexture0;

uniform int uSection;
uniform int uCell;
uniform int uLastSection;

out
vec4 FragColor;

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
    
    int displayDegree = 359;
    int displaySections = uSection * displayDegree / 360;
    int firstSection = uLastSection;
    int lastSection = firstSection - displaySections > 0 ? firstSection - displaySections : firstSection + uSection - displaySections;

    // attention: col,row
    float g = 0.0;

    if (row == uLastSection && r < 0.5)
    {
        // g = 1.0;
    }
    else
    {
        
        if (firstSection > lastSection && row > lastSection && row< firstSection)
        {
            //float fade = float(displaySections - firstSection + row) / displaySections;
            float fade = float(firstSection - row) / displaySections;

            g = texelFetch(uTexture0, ivec2(col, row), 0).r *fade *1.1 ;
        }
        else if (firstSection < lastSection && (row < firstSection || row > lastSection))
        {
            float fade = firstSection - row >= 0 ?
            //float(displaySections - firstSection + row) / displaySections : float(-firstSection + row ) / displaySections;
            float(firstSection - row) / displaySections : float(displaySections + firstSection - row ) / displaySections;

            g = texelFetch(uTexture0, ivec2(col, row), 0).r * fade * 1.1;
        }
        
    }

    FragColor = vec4(vec3(0.0, g, 0.0), 1.0);
}