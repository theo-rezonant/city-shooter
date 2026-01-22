/**
 * Vitest setup file
 * Mocks browser APIs and Babylon.js dependencies for testing
 */

import { vi } from "vitest";

// Mock canvas element
const mockCanvas = {
  getContext: vi.fn(() => ({
    clearRect: vi.fn(),
    fillRect: vi.fn(),
    getImageData: vi.fn(() => ({ data: [] })),
    putImageData: vi.fn(),
    createImageData: vi.fn(() => ({})),
    setTransform: vi.fn(),
    drawImage: vi.fn(),
    save: vi.fn(),
    restore: vi.fn(),
    beginPath: vi.fn(),
    moveTo: vi.fn(),
    lineTo: vi.fn(),
    closePath: vi.fn(),
    stroke: vi.fn(),
    fill: vi.fn(),
    translate: vi.fn(),
    scale: vi.fn(),
    rotate: vi.fn(),
    arc: vi.fn(),
    rect: vi.fn(),
    clip: vi.fn(),
    isPointInPath: vi.fn(),
    measureText: vi.fn(() => ({ width: 0 })),
    fillText: vi.fn(),
    strokeText: vi.fn(),
    createLinearGradient: vi.fn(() => ({
      addColorStop: vi.fn(),
    })),
    createRadialGradient: vi.fn(() => ({
      addColorStop: vi.fn(),
    })),
    createPattern: vi.fn(),
  })),
  width: 800,
  height: 600,
  style: {},
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
  getBoundingClientRect: vi.fn(() => ({
    left: 0,
    top: 0,
    right: 800,
    bottom: 600,
    width: 800,
    height: 600,
  })),
};

// Note: We don't stub the entire document since jsdom provides a valid one
// Just mock specific global methods that are needed

// Mock window methods
vi.stubGlobal(
  "requestAnimationFrame",
  vi.fn((cb) => setTimeout(cb, 16))
);
vi.stubGlobal(
  "cancelAnimationFrame",
  vi.fn((id) => clearTimeout(id))
);

// Mock performance
vi.stubGlobal("performance", {
  now: vi.fn(() => Date.now()),
  mark: vi.fn(),
  measure: vi.fn(),
  getEntriesByName: vi.fn(() => []),
  getEntriesByType: vi.fn(() => []),
  clearMarks: vi.fn(),
  clearMeasures: vi.fn(),
});

// Mock WebGL
const mockWebGLContext = {
  canvas: mockCanvas,
  drawingBufferWidth: 800,
  drawingBufferHeight: 600,
  getParameter: vi.fn(() => []),
  getExtension: vi.fn(() => ({})),
  getShaderPrecisionFormat: vi.fn(() => ({
    precision: 23,
    rangeMin: 127,
    rangeMax: 127,
  })),
  createShader: vi.fn(() => ({})),
  createProgram: vi.fn(() => ({})),
  createBuffer: vi.fn(() => ({})),
  createTexture: vi.fn(() => ({})),
  createFramebuffer: vi.fn(() => ({})),
  createRenderbuffer: vi.fn(() => ({})),
  shaderSource: vi.fn(),
  compileShader: vi.fn(),
  getShaderParameter: vi.fn(() => true),
  getShaderInfoLog: vi.fn(() => ""),
  attachShader: vi.fn(),
  linkProgram: vi.fn(),
  getProgramParameter: vi.fn(() => true),
  getProgramInfoLog: vi.fn(() => ""),
  useProgram: vi.fn(),
  getUniformLocation: vi.fn(() => ({})),
  getAttribLocation: vi.fn(() => 0),
  enableVertexAttribArray: vi.fn(),
  vertexAttribPointer: vi.fn(),
  bindBuffer: vi.fn(),
  bufferData: vi.fn(),
  bindTexture: vi.fn(),
  activeTexture: vi.fn(),
  texImage2D: vi.fn(),
  texParameteri: vi.fn(),
  viewport: vi.fn(),
  clear: vi.fn(),
  clearColor: vi.fn(),
  clearDepth: vi.fn(),
  clearStencil: vi.fn(),
  enable: vi.fn(),
  disable: vi.fn(),
  depthFunc: vi.fn(),
  cullFace: vi.fn(),
  frontFace: vi.fn(),
  blendFunc: vi.fn(),
  blendFuncSeparate: vi.fn(),
  drawArrays: vi.fn(),
  drawElements: vi.fn(),
  bindFramebuffer: vi.fn(),
  bindRenderbuffer: vi.fn(),
  framebufferTexture2D: vi.fn(),
  renderbufferStorage: vi.fn(),
  checkFramebufferStatus: vi.fn(() => 36053), // FRAMEBUFFER_COMPLETE
  deleteShader: vi.fn(),
  deleteProgram: vi.fn(),
  deleteBuffer: vi.fn(),
  deleteTexture: vi.fn(),
  deleteFramebuffer: vi.fn(),
  deleteRenderbuffer: vi.fn(),
  uniform1i: vi.fn(),
  uniform1f: vi.fn(),
  uniform2f: vi.fn(),
  uniform3f: vi.fn(),
  uniform4f: vi.fn(),
  uniformMatrix4fv: vi.fn(),
  pixelStorei: vi.fn(),
  readPixels: vi.fn(),
  scissor: vi.fn(),
  ARRAY_BUFFER: 34962,
  ELEMENT_ARRAY_BUFFER: 34963,
  STATIC_DRAW: 35044,
  FLOAT: 5126,
  UNSIGNED_SHORT: 5123,
  TRIANGLES: 4,
  TEXTURE_2D: 3553,
  TEXTURE0: 33984,
  RGBA: 6408,
  UNSIGNED_BYTE: 5121,
  LINEAR: 9729,
  NEAREST: 9728,
  TEXTURE_MAG_FILTER: 10240,
  TEXTURE_MIN_FILTER: 10241,
  CLAMP_TO_EDGE: 33071,
  TEXTURE_WRAP_S: 10242,
  TEXTURE_WRAP_T: 10243,
  DEPTH_TEST: 2929,
  CULL_FACE: 2884,
  BLEND: 3042,
  SRC_ALPHA: 770,
  ONE_MINUS_SRC_ALPHA: 771,
  LEQUAL: 515,
  BACK: 1029,
  CCW: 2305,
  COLOR_BUFFER_BIT: 16384,
  DEPTH_BUFFER_BIT: 256,
  STENCIL_BUFFER_BIT: 1024,
  VERTEX_SHADER: 35633,
  FRAGMENT_SHADER: 35632,
  COMPILE_STATUS: 35713,
  LINK_STATUS: 35714,
  FRAMEBUFFER: 36160,
  RENDERBUFFER: 36161,
  COLOR_ATTACHMENT0: 36064,
  DEPTH_ATTACHMENT: 36096,
  DEPTH_COMPONENT16: 33189,
  FRAMEBUFFER_COMPLETE: 36053,
};

// Add WebGL context to mock canvas
(mockCanvas.getContext as ReturnType<typeof vi.fn>).mockImplementation(
  (type: string) => {
    if (
      type === "webgl" ||
      type === "webgl2" ||
      type === "experimental-webgl"
    ) {
      return mockWebGLContext;
    }
    return null;
  }
);
