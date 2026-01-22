import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";

// Mock Babylon.js modules
vi.mock("@babylonjs/core", () => ({
  Scene: vi.fn().mockImplementation(() => ({
    getInstrumentation: vi.fn(() => ({})),
    getActiveMeshes: vi.fn(() => ({
      length: 50,
      data: Array(50).fill({
        material: { id: "test_material" },
      }),
    })),
    getTotalVertices: vi.fn(() => 100000),
    getActiveIndices: vi.fn(() => 75000),
    getActiveBones: vi.fn(() => 0),
    getActiveParticles: vi.fn(() => 0),
    meshes: Array(100).fill({}),
    materials: Array(10).fill({}),
  })),
  Engine: vi.fn().mockImplementation(() => ({
    getFps: vi.fn(() => 60),
  })),
}));

import { PerformanceMetrics } from "@/core/PerformanceMetrics";
import { Scene, Engine } from "@babylonjs/core";

describe("PerformanceMetrics", () => {
  let scene: Scene;
  let engine: Engine;
  let metrics: PerformanceMetrics;

  beforeEach(() => {
    // Setup DOM elements
    document.body.innerHTML = `
      <div id="metricsPanel">
        <div id="metricsContent"></div>
      </div>
    `;

    scene = new Scene({} as never);
    engine = new Engine({} as never);
    metrics = new PerformanceMetrics(scene, engine);
  });

  afterEach(() => {
    document.body.innerHTML = "";
    vi.clearAllMocks();
  });

  describe("captureSnapshot", () => {
    it("should capture current performance metrics", () => {
      const snapshot = metrics.captureSnapshot();

      expect(snapshot).toBeDefined();
      expect(snapshot.fps).toBe(60);
      expect(snapshot.totalMeshes).toBe(100);
      expect(snapshot.activeMeshes).toBe(50);
      expect(snapshot.totalVertices).toBe(100000);
      expect(snapshot.timestamp).toBeGreaterThan(0);
    });

    it("should calculate draw calls based on active meshes", () => {
      const snapshot = metrics.captureSnapshot();

      // Draw calls are estimated from active meshes
      expect(snapshot.drawCalls).toBe(50);
    });

    it("should calculate total faces from active indices", () => {
      const snapshot = metrics.captureSnapshot();

      // Total faces = activeIndices / 3
      expect(snapshot.totalFaces).toBe(25000);
    });
  });

  describe("setBaseline", () => {
    it("should store baseline metrics", () => {
      metrics.setBaseline();
      const baseline = metrics.getBaselineSnapshot();

      expect(baseline).not.toBeNull();
      expect(baseline?.fps).toBe(60);
    });
  });

  describe("getOptimizationStats", () => {
    it("should return null if no baseline is set", () => {
      metrics.update();
      const stats = metrics.getOptimizationStats();

      expect(stats).toBeNull();
    });

    it("should calculate optimization stats when baseline is set", () => {
      metrics.setBaseline();

      // Update to capture last snapshot
      for (let i = 0; i <= 30; i++) {
        metrics.update();
      }

      const stats = metrics.getOptimizationStats();

      expect(stats).not.toBeNull();
      expect(typeof stats?.drawCallReduction).toBe("number");
      expect(typeof stats?.meshReduction).toBe("number");
    });
  });

  describe("update", () => {
    it("should update metrics at specified interval", () => {
      // Update less than interval
      for (let i = 0; i < 29; i++) {
        metrics.update();
      }

      expect(metrics.getLastSnapshot()).toBeNull();

      // One more update should trigger snapshot
      metrics.update();

      expect(metrics.getLastSnapshot()).not.toBeNull();
    });
  });

  describe("toggleDisplay", () => {
    it("should toggle metrics panel visibility", () => {
      const panel = document.getElementById("metricsPanel");
      expect(panel?.classList.contains("visible")).toBe(false);

      metrics.toggleDisplay();
      expect(panel?.classList.contains("visible")).toBe(true);

      metrics.toggleDisplay();
      expect(panel?.classList.contains("visible")).toBe(false);
    });
  });

  describe("showDisplay", () => {
    it("should show the metrics panel", () => {
      const panel = document.getElementById("metricsPanel");
      expect(panel?.classList.contains("visible")).toBe(false);

      metrics.showDisplay();
      expect(panel?.classList.contains("visible")).toBe(true);
    });
  });

  describe("logMetrics", () => {
    it("should log metrics to console", () => {
      const consoleSpy = vi.spyOn(console, "log");

      metrics.logMetrics("Test label");

      expect(consoleSpy).toHaveBeenCalled();
      expect(consoleSpy.mock.calls[0][0]).toContain("[PerformanceMetrics]");
      expect(consoleSpy.mock.calls[0][0]).toContain("Test label");
    });

    it("should include comparison to baseline if set", () => {
      const consoleSpy = vi.spyOn(console, "log");

      metrics.setBaseline();
      consoleSpy.mockClear();

      metrics.logMetrics("After optimization");

      // Should have two log calls - one for metrics, one for comparison
      expect(consoleSpy).toHaveBeenCalledTimes(2);
    });
  });
});
