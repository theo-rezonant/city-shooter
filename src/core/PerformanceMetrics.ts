import { Scene, Engine } from "@babylonjs/core";

/**
 * Performance metrics tracking for draw calls, FPS, and other stats
 */
export interface MetricsSnapshot {
  fps: number;
  drawCalls: number;
  totalMeshes: number;
  activeMeshes: number;
  totalVertices: number;
  totalFaces: number;
  activeIndices: number;
  activeBones: number;
  activeParticles: number;
  timestamp: number;
}

export class PerformanceMetrics {
  private scene: Scene;
  private engine: Engine;
  private metricsElement: HTMLElement | null = null;
  private lastSnapshot: MetricsSnapshot | null = null;
  private baselineSnapshot: MetricsSnapshot | null = null;
  private updateCounter = 0;
  private updateInterval = 30; // Update display every N frames

  constructor(scene: Scene, engine: Engine) {
    this.scene = scene;
    this.engine = engine;
    this.metricsElement = document.getElementById("metricsContent");
  }

  /**
   * Capture current performance metrics
   */
  captureSnapshot(): MetricsSnapshot {
    return {
      fps: this.engine.getFps(),
      drawCalls: this.getDrawCallCount(),
      totalMeshes: this.scene.meshes.length,
      activeMeshes: this.scene.getActiveMeshes().length,
      totalVertices: this.scene.getTotalVertices(),
      totalFaces: this.getTotalFaces(),
      activeIndices: this.scene.getActiveIndices(),
      activeBones: this.scene.getActiveBones(),
      activeParticles: this.scene.getActiveParticles(),
      timestamp: performance.now(),
    };
  }

  /**
   * Get approximate draw call count
   */
  private getDrawCallCount(): number {
    // Babylon.js doesn't directly expose draw calls, but we can estimate
    // based on active meshes with different materials
    const activeMeshes = this.scene.getActiveMeshes();
    const uniqueMaterials = new Set<string>();

    for (let i = 0; i < activeMeshes.length; i++) {
      const mesh = activeMeshes.data[i];
      if (mesh.material) {
        uniqueMaterials.add(mesh.material.id);
      }
    }

    // Each unique material typically results in at least one draw call
    // Multi-material meshes can have more
    return activeMeshes.length;
  }

  /**
   * Get total faces count
   */
  private getTotalFaces(): number {
    return Math.floor(this.scene.getActiveIndices() / 3);
  }

  /**
   * Store baseline metrics for comparison
   */
  setBaseline(): void {
    this.baselineSnapshot = this.captureSnapshot();
    console.log("[PerformanceMetrics] Baseline set:", this.baselineSnapshot);
  }

  /**
   * Log metrics with optional label
   */
  logMetrics(label?: string): void {
    const snapshot = this.captureSnapshot();

    console.log(
      `[PerformanceMetrics] ${label || "Current metrics"}:`,
      `\n  FPS: ${snapshot.fps.toFixed(1)}`,
      `\n  Draw Calls (estimated): ${snapshot.drawCalls}`,
      `\n  Total Meshes: ${snapshot.totalMeshes}`,
      `\n  Active Meshes: ${snapshot.activeMeshes}`,
      `\n  Total Vertices: ${snapshot.totalVertices.toLocaleString()}`,
      `\n  Total Faces: ${snapshot.totalFaces.toLocaleString()}`,
      `\n  Active Indices: ${snapshot.activeIndices.toLocaleString()}`
    );

    if (this.baselineSnapshot) {
      const drawCallReduction =
        ((this.baselineSnapshot.drawCalls - snapshot.drawCalls) /
          this.baselineSnapshot.drawCalls) *
        100;
      const meshReduction =
        ((this.baselineSnapshot.activeMeshes - snapshot.activeMeshes) /
          this.baselineSnapshot.activeMeshes) *
        100;

      console.log(
        "[PerformanceMetrics] Compared to baseline:",
        `\n  Draw Call reduction: ${drawCallReduction.toFixed(1)}%`,
        `\n  Active Mesh reduction: ${meshReduction.toFixed(1)}%`
      );
    }
  }

  /**
   * Update metrics (called every frame)
   */
  update(): void {
    this.updateCounter++;

    if (this.updateCounter >= this.updateInterval) {
      this.updateCounter = 0;
      this.lastSnapshot = this.captureSnapshot();
      this.updateDisplay();
    }
  }

  /**
   * Update the metrics display element
   */
  private updateDisplay(): void {
    if (!this.metricsElement || !this.lastSnapshot) return;

    const s = this.lastSnapshot;
    let html = `
FPS: ${s.fps.toFixed(1)}
Draw Calls: ~${s.drawCalls}
Meshes: ${s.activeMeshes}/${s.totalMeshes}
Vertices: ${s.totalVertices.toLocaleString()}
Faces: ${s.totalFaces.toLocaleString()}
`;

    if (this.baselineSnapshot) {
      const drawCallReduction =
        ((this.baselineSnapshot.drawCalls - s.drawCalls) /
          this.baselineSnapshot.drawCalls) *
        100;
      html += `\nOptimization: ${drawCallReduction > 0 ? "+" : ""}${drawCallReduction.toFixed(1)}%`;
    }

    this.metricsElement.textContent = html;
  }

  /**
   * Toggle metrics panel visibility
   */
  toggleDisplay(): void {
    const panel = document.getElementById("metricsPanel");
    if (panel) {
      panel.classList.toggle("visible");
    }
  }

  /**
   * Show metrics panel
   */
  showDisplay(): void {
    const panel = document.getElementById("metricsPanel");
    if (panel) {
      panel.classList.add("visible");
    }
  }

  /**
   * Get the last captured snapshot
   */
  getLastSnapshot(): MetricsSnapshot | null {
    return this.lastSnapshot;
  }

  /**
   * Get the baseline snapshot
   */
  getBaselineSnapshot(): MetricsSnapshot | null {
    return this.baselineSnapshot;
  }

  /**
   * Compare current metrics to baseline and return reduction percentages
   */
  getOptimizationStats(): {
    drawCallReduction: number;
    meshReduction: number;
  } | null {
    if (!this.baselineSnapshot || !this.lastSnapshot) return null;

    return {
      drawCallReduction:
        ((this.baselineSnapshot.drawCalls - this.lastSnapshot.drawCalls) /
          this.baselineSnapshot.drawCalls) *
        100,
      meshReduction:
        ((this.baselineSnapshot.activeMeshes - this.lastSnapshot.activeMeshes) /
          this.baselineSnapshot.activeMeshes) *
        100,
    };
  }
}
