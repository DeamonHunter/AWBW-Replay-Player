#include "sh_Utils.h"
#include "sh_TextureWrapping.h"

varying lowp vec4 v_Colour;
varying mediump vec2 v_TexCoord;
varying mediump vec4 v_TexRect;

uniform lowp sampler2D m_Sampler;
uniform mediump float greyscaleAmount;

void main(void)
{
	gl_FragColor = toSRGB(v_Colour * wrappedSampler(wrap(v_TexCoord, v_TexRect), v_TexRect, m_Sampler, -0.9));

    lowp float average = (gl_FragColor.r + gl_FragColor.g + gl_FragColor.b) / 3.0;
    gl_FragColor = mix(gl_FragColor, vec4(average, average, average, gl_FragColor.a), greyscaleAmount);
}