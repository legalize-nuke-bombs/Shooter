#ifndef BOID_FLAP_INCLUDED
#define BOID_FLAP_INCLUDED

void BoidFlap_float(float3 PositionOS, float3 ObjectPosition, float Time, float Butterfly, float FlapSpeed, out float3 Out)
{
    float flappingSpeed = (Butterfly > 0.5 ? 12.0 : 10.0) * FlapSpeed;
    float yPower        = Butterfly > 0.5 ?  5.0 :  0.9;
    float yOffset       = Butterfly > 0.5 ?  3.0 :  2.0;
    float xPower        = Butterfly > 0.5 ?  1.2 :  0.1;
    float xOffset       = Butterfly > 0.5 ? -0.1 :  0.4;
    float xCenter       = Butterfly > 0.5 ?  0.2 : 0.15;
    float zPower        = Butterfly > 0.5 ? -0.1 :  0.1;
    float waveY         = Butterfly > 0.5 ?  1.0 :  0.0;
    float waveYSpeed    = Butterfly > 0.5 ? 0.75 :  0.0;
    float phaseScale    = Butterfly > 0.5 ?  0.2 :  0.5;

    float yf = PositionOS.y + yOffset;
    float xf = abs(PositionOS.x) + xOffset;
    xf = Butterfly > 0.5 ? xf * 0.5 : xf * xf * xf;

    float phase = Time + sin(ObjectPosition.y * phaseScale);
    float flap = sin(PositionOS.y / 5.0) * yf * xf * cos(phase * flappingSpeed);

    float3 p = PositionOS;
    p.y += flap * yPower;
    if (abs(PositionOS.x) > xCenter) p.x -= flap * xPower * PositionOS.x;
    p.z += flap * zPower;

    if (waveY > 0.0)
    {
        float bob = cos((ObjectPosition.x + ObjectPosition.z) * waveY * 0.1) * 3.0;
        p.y += sin((Time + bob) * waveYSpeed) * waveY;
    }

    Out = p;
}

#endif
