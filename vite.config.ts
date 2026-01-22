import { defineConfig } from "vite";
import { resolve } from "path";

export default defineConfig({
  resolve: {
    alias: {
      "@": resolve(__dirname, "./src"),
    },
  },
  assetsInclude: ["**/*.glb", "**/*.gltf"],
  build: {
    target: "esnext",
    rollupOptions: {
      output: {
        manualChunks: {
          babylon: [
            "@babylonjs/core",
            "@babylonjs/loaders",
            "@babylonjs/gui",
            "@babylonjs/materials",
          ],
          havok: ["@babylonjs/havok"],
        },
      },
    },
  },
  server: {
    headers: {
      "Cross-Origin-Opener-Policy": "same-origin",
      "Cross-Origin-Embedder-Policy": "require-corp",
    },
  },
  optimizeDeps: {
    exclude: ["@babylonjs/havok"],
  },
});
