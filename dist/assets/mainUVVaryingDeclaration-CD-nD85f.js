import{S as e}from"./index-gCZdN4BF.js";const r="meshUboDeclaration",a=`struct Mesh {world : mat4x4<f32>,
visibility : f32,};var<uniform> mesh : Mesh;
#define WORLD_UBO
`;e.IncludesShadersStoreWGSL[r]||(e.IncludesShadersStoreWGSL[r]=a);const s="mainUVVaryingDeclaration",n=`#ifdef MAINUV{X}
varying vMainUV{X}: vec2f;
#endif
`;e.IncludesShadersStoreWGSL[s]||(e.IncludesShadersStoreWGSL[s]=n);
//# sourceMappingURL=mainUVVaryingDeclaration-CD-nD85f.js.map
