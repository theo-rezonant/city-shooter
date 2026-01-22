import{S as e}from"./index-gCZdN4BF.js";const i="meshUboDeclaration",r=`#ifdef WEBGL2
uniform mat4 world;uniform float visibility;
#else
layout(std140,column_major) uniform;uniform Mesh
{mat4 world;float visibility;};
#endif
#define WORLD_UBO
`;e.IncludesShadersStore[i]||(e.IncludesShadersStore[i]=r);const o="mainUVVaryingDeclaration",a=`#ifdef MAINUV{X}
varying vec2 vMainUV{X};
#endif
`;e.IncludesShadersStore[o]||(e.IncludesShadersStore[o]=a);
//# sourceMappingURL=mainUVVaryingDeclaration-9pdyuqM2.js.map
