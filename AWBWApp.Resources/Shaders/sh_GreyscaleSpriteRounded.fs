#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

varying mediump vec2 v_TexCoord;

uniform lowp sampler2D m_Sampler;
uniform mediump float greyscaleAmount;

void main(void)
{
    vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);
    gl_FragColor = getRoundedColor(toSRGB(v_Colour * wrappedSampler(wrappedCoord, v_TexRect, m_Sampler, -0.9)), wrappedCoord);

    lowp float average = (gl_FragColor.r + gl_FragColor.g + gl_FragColor.b) / 3.0;
    gl_FragColor = mix(gl_FragColor, vec4(average, average, average, gl_FragColor.a), greyscaleAmount);
}