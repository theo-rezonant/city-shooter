/**
 * Vitest setup file for Babylon.js tests
 * Mocks browser APIs and Babylon.js dependencies
 */

import { vi } from 'vitest';

// Mock requestAnimationFrame
global.requestAnimationFrame = vi.fn((callback: FrameRequestCallback) => {
  return setTimeout(() => callback(performance.now()), 16) as unknown as number;
});

global.cancelAnimationFrame = vi.fn((id: number) => {
  clearTimeout(id);
});

// Mock ResizeObserver
global.ResizeObserver = vi.fn().mockImplementation(() => ({
  observe: vi.fn(),
  unobserve: vi.fn(),
  disconnect: vi.fn(),
}));

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// Mock pointer lock API
Object.defineProperty(document, 'pointerLockElement', {
  writable: true,
  value: null,
});

document.exitPointerLock = vi.fn();

// Mock canvas methods
HTMLCanvasElement.prototype.getContext = vi.fn().mockReturnValue({
  canvas: { width: 800, height: 600 },
  drawImage: vi.fn(),
  fillRect: vi.fn(),
  clearRect: vi.fn(),
  getImageData: vi.fn().mockReturnValue({ data: new Uint8ClampedArray() }),
  putImageData: vi.fn(),
  createImageData: vi.fn(),
  setTransform: vi.fn(),
  save: vi.fn(),
  restore: vi.fn(),
  scale: vi.fn(),
  rotate: vi.fn(),
  translate: vi.fn(),
  transform: vi.fn(),
  beginPath: vi.fn(),
  moveTo: vi.fn(),
  lineTo: vi.fn(),
  closePath: vi.fn(),
  stroke: vi.fn(),
  fill: vi.fn(),
  fillText: vi.fn(),
  measureText: vi.fn().mockReturnValue({ width: 0 }),
});

// Mock WebGL context
const mockWebGLContext = {
  canvas: { width: 800, height: 600 },
  getExtension: vi.fn().mockReturnValue({}),
  getParameter: vi.fn().mockReturnValue(null),
  createShader: vi.fn().mockReturnValue({}),
  shaderSource: vi.fn(),
  compileShader: vi.fn(),
  getShaderParameter: vi.fn().mockReturnValue(true),
  createProgram: vi.fn().mockReturnValue({}),
  attachShader: vi.fn(),
  linkProgram: vi.fn(),
  getProgramParameter: vi.fn().mockReturnValue(true),
  useProgram: vi.fn(),
  createBuffer: vi.fn().mockReturnValue({}),
  bindBuffer: vi.fn(),
  bufferData: vi.fn(),
  enable: vi.fn(),
  disable: vi.fn(),
  viewport: vi.fn(),
  clear: vi.fn(),
  clearColor: vi.fn(),
  drawArrays: vi.fn(),
  drawElements: vi.fn(),
  getUniformLocation: vi.fn().mockReturnValue({}),
  getAttribLocation: vi.fn().mockReturnValue(0),
  enableVertexAttribArray: vi.fn(),
  vertexAttribPointer: vi.fn(),
  uniform1f: vi.fn(),
  uniform1i: vi.fn(),
  uniform2f: vi.fn(),
  uniform3f: vi.fn(),
  uniform4f: vi.fn(),
  uniformMatrix4fv: vi.fn(),
  createTexture: vi.fn().mockReturnValue({}),
  bindTexture: vi.fn(),
  texImage2D: vi.fn(),
  texParameteri: vi.fn(),
  activeTexture: vi.fn(),
  generateMipmap: vi.fn(),
  deleteShader: vi.fn(),
  deleteProgram: vi.fn(),
  deleteBuffer: vi.fn(),
  deleteTexture: vi.fn(),
  getSupportedExtensions: vi.fn().mockReturnValue([]),
  createFramebuffer: vi.fn().mockReturnValue({}),
  bindFramebuffer: vi.fn(),
  framebufferTexture2D: vi.fn(),
  checkFramebufferStatus: vi.fn().mockReturnValue(36053), // FRAMEBUFFER_COMPLETE
  createRenderbuffer: vi.fn().mockReturnValue({}),
  bindRenderbuffer: vi.fn(),
  renderbufferStorage: vi.fn(),
  framebufferRenderbuffer: vi.fn(),
  pixelStorei: vi.fn(),
  readPixels: vi.fn(),
  getShaderInfoLog: vi.fn().mockReturnValue(''),
  getProgramInfoLog: vi.fn().mockReturnValue(''),
  isContextLost: vi.fn().mockReturnValue(false),
  blendFunc: vi.fn(),
  blendEquation: vi.fn(),
  depthFunc: vi.fn(),
  depthMask: vi.fn(),
  cullFace: vi.fn(),
  frontFace: vi.fn(),
  scissor: vi.fn(),
  colorMask: vi.fn(),
  stencilFunc: vi.fn(),
  stencilOp: vi.fn(),
  stencilMask: vi.fn(),
  lineWidth: vi.fn(),
  polygonOffset: vi.fn(),
  flush: vi.fn(),
  finish: vi.fn(),
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
(HTMLCanvasElement.prototype as any).getContext = vi.fn((contextType: string) => {
  if (contextType === 'webgl' || contextType === 'webgl2') {
    return mockWebGLContext;
  }
  return null;
});

// Mock performance API
if (!global.performance) {
  global.performance = {
    now: vi.fn(() => Date.now()),
  } as unknown as Performance;
}
