import {
  Scene,
  SceneLoader,
  AbstractMesh,
  Mesh,
  MeshBuilder,
  Vector3,
  TransformNode,
  ISceneLoaderAsyncResult,
} from "@babylonjs/core";
import {
  PhysicsManager,
  CollisionGroups,
  CollisionMasks,
} from "@/physics/PhysicsManager";

/**
 * Configuration for mesh categorization
 */
interface MeshCategory {
  buildings: AbstractMesh[];
  floors: AbstractMesh[];
  props: AbstractMesh[];
  other: AbstractMesh[];
}

/**
 * Optimization statistics
 */
export interface OptimizationStats {
  originalMeshCount: number;
  optimizedMeshCount: number;
  mergedMeshGroups: number;
  frozenMaterials: number;
  occlusionEnabled: number;
  buildingColliders: number;
  floorColliders: number;
}

/**
 * MapLoader handles loading, optimizing, and preparing the city map
 * with physics collision baking
 */
export class MapLoader {
  private scene: Scene;
  private physicsManager: PhysicsManager;
  private loadedMeshes: AbstractMesh[] = [];
  private rootNode: TransformNode | null = null;
  private stats: OptimizationStats = {
    originalMeshCount: 0,
    optimizedMeshCount: 0,
    mergedMeshGroups: 0,
    frozenMaterials: 0,
    occlusionEnabled: 0,
    buildingColliders: 0,
    floorColliders: 0,
  };

  constructor(scene: Scene, physicsManager: PhysicsManager) {
    this.scene = scene;
    this.physicsManager = physicsManager;
  }

  /**
   * Load the map from a GLB file
   */
  async loadMap(
    url: string,
    onProgress?: (progress: number) => void
  ): Promise<void> {
    console.log(`[MapLoader] Loading map from: ${url}`);

    try {
      // Load the GLB file
      const result = await SceneLoader.ImportMeshAsync(
        "",
        url,
        "",
        this.scene,
        (event) => {
          if (event.lengthComputable) {
            const progress = event.loaded / event.total;
            onProgress?.(progress);
          }
        }
      );

      this.processLoadedAsset(result);
    } catch (error) {
      console.error("[MapLoader] Failed to load map:", error);
      throw error;
    }
  }

  /**
   * Process the loaded asset - handle coordinate system and categorize meshes
   */
  private processLoadedAsset(result: ISceneLoaderAsyncResult): void {
    console.log("[MapLoader] Processing loaded meshes...");

    this.stats.originalMeshCount = result.meshes.length;
    this.loadedMeshes = result.meshes;

    // Create a root node for the map
    this.rootNode = new TransformNode("mapRoot", this.scene);

    // Handle Blender Z-up to Babylon Y-up conversion
    // GLB loader usually handles this, but we verify alignment
    this.ensureCorrectOrientation();

    // Parent all meshes to root node
    for (const mesh of result.meshes) {
      if (!mesh.parent) {
        mesh.parent = this.rootNode;
      }
    }

    console.log(`[MapLoader] Loaded ${result.meshes.length} meshes`);
  }

  /**
   * Ensure correct coordinate system orientation
   * Blender exports Z-up, Babylon uses Y-up
   */
  private ensureCorrectOrientation(): void {
    // The GLB loader typically handles this conversion automatically
    // but we verify the bounds are sensible
    if (this.loadedMeshes.length === 0) return;

    let minY = Infinity;
    let maxY = -Infinity;

    for (const mesh of this.loadedMeshes) {
      const boundingInfo = mesh.getBoundingInfo();
      if (boundingInfo) {
        minY = Math.min(minY, boundingInfo.boundingBox.minimumWorld.y);
        maxY = Math.max(maxY, boundingInfo.boundingBox.maximumWorld.y);
      }
    }

    console.log(
      `[MapLoader] Y bounds: min=${minY.toFixed(2)}, max=${maxY.toFixed(2)}`
    );

    // If the map is below ground level, adjust the root node
    if (minY < -10) {
      console.warn(
        "[MapLoader] Map appears to be below ground, adjusting position"
      );
      if (this.rootNode) {
        this.rootNode.position.y -= minY;
      }
    }
  }

  /**
   * Categorize meshes by type (buildings, floors, props)
   */
  categorizeMeshes(): MeshCategory {
    const categories: MeshCategory = {
      buildings: [],
      floors: [],
      props: [],
      other: [],
    };

    for (const mesh of this.loadedMeshes) {
      const name = mesh.name.toLowerCase();
      const boundingInfo = mesh.getBoundingInfo();

      if (!boundingInfo) {
        categories.other.push(mesh);
        continue;
      }

      const size = boundingInfo.boundingBox.extendSizeWorld;
      const height = size.y * 2;
      const footprint = Math.max(size.x, size.z) * 2;

      // Categorize based on name patterns and geometry
      if (
        name.includes("build") ||
        name.includes("bldg") ||
        name.includes("house") ||
        name.includes("shop") ||
        name.includes("tower") ||
        (height > 3 && footprint > 2)
      ) {
        categories.buildings.push(mesh);
      } else if (
        name.includes("floor") ||
        name.includes("ground") ||
        name.includes("street") ||
        name.includes("road") ||
        name.includes("sidewalk") ||
        name.includes("pavement") ||
        (height < 0.5 && footprint > 5)
      ) {
        categories.floors.push(mesh);
      } else if (
        name.includes("prop") ||
        name.includes("sign") ||
        name.includes("light") ||
        name.includes("bench") ||
        name.includes("trash") ||
        name.includes("car") ||
        name.includes("vehicle")
      ) {
        categories.props.push(mesh);
      } else {
        categories.other.push(mesh);
      }
    }

    console.log(
      "[MapLoader] Mesh categories:",
      `\n  Buildings: ${categories.buildings.length}`,
      `\n  Floors: ${categories.floors.length}`,
      `\n  Props: ${categories.props.length}`,
      `\n  Other: ${categories.other.length}`
    );

    return categories;
  }

  /**
   * Apply all optimizations to the map
   */
  optimizeMap(): void {
    console.log("[MapLoader] Applying optimizations...");

    const categories = this.categorizeMeshes();

    // 1. Freeze world matrices for all static meshes
    this.freezeStaticMeshes();

    // 2. Merge non-interactive building meshes by material
    this.mergeMeshesByMaterial(categories.buildings, "merged_buildings");
    this.mergeMeshesByMaterial(categories.props, "merged_props");

    // 3. Enable occlusion queries for large buildings
    this.enableOcclusionQueries(categories.buildings);

    // 4. Freeze all materials
    this.scene.freezeMaterials();
    this.stats.frozenMaterials = this.scene.materials.length;

    // 5. Optimize scene for better performance
    this.scene.freezeActiveMeshes();

    this.stats.optimizedMeshCount = this.scene.meshes.length;

    console.log(
      "[MapLoader] Optimization complete:",
      `\n  Original meshes: ${this.stats.originalMeshCount}`,
      `\n  Optimized meshes: ${this.stats.optimizedMeshCount}`,
      `\n  Merged groups: ${this.stats.mergedMeshGroups}`,
      `\n  Frozen materials: ${this.stats.frozenMaterials}`,
      `\n  Occlusion enabled: ${this.stats.occlusionEnabled}`
    );
  }

  /**
   * Freeze world matrices for static meshes
   */
  private freezeStaticMeshes(): void {
    let frozenCount = 0;

    for (const mesh of this.loadedMeshes) {
      // Freeze world matrix to avoid recalculation
      mesh.freezeWorldMatrix();

      // Disable bounding info update since mesh is static
      mesh.doNotSyncBoundingInfo = true;

      frozenCount++;
    }

    console.log(`[MapLoader] Frozen ${frozenCount} mesh world matrices`);
  }

  /**
   * Merge meshes by material to reduce draw calls
   */
  private mergeMeshesByMaterial(
    meshes: AbstractMesh[],
    groupName: string
  ): void {
    if (meshes.length === 0) return;

    // Group meshes by material
    const materialGroups = new Map<string, Mesh[]>();

    for (const mesh of meshes) {
      if (!(mesh instanceof Mesh)) continue;
      if (mesh.getTotalVertices() === 0) continue;

      const materialId = mesh.material?.id || "no_material";

      if (!materialGroups.has(materialId)) {
        materialGroups.set(materialId, []);
      }
      materialGroups.get(materialId)!.push(mesh);
    }

    // Merge each group
    let groupIndex = 0;
    for (const [materialId, group] of materialGroups) {
      if (group.length < 2) continue; // No need to merge single mesh

      try {
        const mergedMesh = Mesh.MergeMeshes(
          group,
          true, // Dispose source meshes
          true, // Allow different vertex data types
          undefined,
          false, // Don't subdivide
          true // Allow multi-material
        );

        if (mergedMesh) {
          mergedMesh.name = `${groupName}_${groupIndex}`;
          mergedMesh.freezeWorldMatrix();
          mergedMesh.doNotSyncBoundingInfo = true;

          // Keep the material
          if (group[0].material) {
            mergedMesh.material = group[0].material;
          }

          this.stats.mergedMeshGroups++;
          groupIndex++;

          console.log(
            `[MapLoader] Merged ${group.length} meshes into ${mergedMesh.name}`
          );
        }
      } catch (error) {
        console.warn(`[MapLoader] Failed to merge group ${materialId}:`, error);
      }
    }
  }

  /**
   * Enable occlusion queries for large structures
   */
  private enableOcclusionQueries(meshes: AbstractMesh[]): void {
    const minSize = 5; // Minimum size for occlusion query (in units)

    for (const mesh of meshes) {
      const boundingInfo = mesh.getBoundingInfo();
      if (!boundingInfo) continue;

      const size = boundingInfo.boundingBox.extendSizeWorld;
      const maxDimension = Math.max(size.x, size.y, size.z) * 2;

      if (maxDimension >= minSize) {
        mesh.occlusionQueryAlgorithmType = 0; // Conservative
        mesh.occlusionType = 1; // Strict
        mesh.isOccluded = false;
        this.stats.occlusionEnabled++;
      }
    }

    console.log(
      `[MapLoader] Enabled occlusion queries for ${this.stats.occlusionEnabled} meshes`
    );
  }

  /**
   * Bake physics collision for the map
   */
  async bakeCollision(): Promise<void> {
    if (!this.physicsManager.isReady()) {
      console.error(
        "[MapLoader] Physics not initialized, cannot bake collision"
      );
      return;
    }

    console.log("[MapLoader] Baking physics collision...");

    const categories = this.categorizeMeshes();

    // 1. Create colliders for floor meshes (need precise collision)
    this.stats.floorColliders = await this.bakeFloorCollision(
      categories.floors
    );

    // 2. Create colliders for buildings (use box approximation where possible)
    this.stats.buildingColliders = await this.bakeBuildingCollision([
      ...categories.buildings,
      ...categories.other, // Include other meshes as potential buildings
    ]);

    console.log(
      "[MapLoader] Collision baking complete:",
      `\n  Floor colliders: ${this.stats.floorColliders}`,
      `\n  Building colliders: ${this.stats.buildingColliders}`
    );
  }

  /**
   * Bake collision for floor meshes
   * Uses mesh colliders for accurate ground collision
   */
  private async bakeFloorCollision(meshes: AbstractMesh[]): Promise<number> {
    let colliderCount = 0;

    for (const mesh of meshes) {
      if (!(mesh instanceof Mesh)) continue;
      if (mesh.getTotalVertices() === 0) continue;

      // Unfreeze temporarily to create physics
      mesh.unfreezeWorldMatrix();

      const aggregate = this.physicsManager.createStaticMeshCollider(mesh, {
        friction: 0.8, // Higher friction for ground
        restitution: 0.1,
        collisionGroup: CollisionGroups.STATIC_ENVIRONMENT,
        collisionMask: CollisionMasks.STATIC_ENVIRONMENT,
      });

      if (aggregate) {
        colliderCount++;
      }

      // Re-freeze after physics creation
      mesh.freezeWorldMatrix();
    }

    return colliderCount;
  }

  /**
   * Bake collision for building meshes
   * Uses box colliders for simple shapes, mesh for complex
   */
  private async bakeBuildingCollision(meshes: AbstractMesh[]): Promise<number> {
    let colliderCount = 0;

    for (const mesh of meshes) {
      if (!(mesh instanceof Mesh)) continue;
      if (mesh.getTotalVertices() === 0) continue;

      const boundingInfo = mesh.getBoundingInfo();
      if (!boundingInfo) continue;

      // Unfreeze temporarily to create physics
      mesh.unfreezeWorldMatrix();

      // Decide between box and mesh collider based on complexity
      const vertexCount = mesh.getTotalVertices();
      const useBoxCollider = vertexCount < 200; // Simple geometry

      let aggregate;

      if (useBoxCollider) {
        // Use box collider for simple buildings (faster)
        aggregate = this.physicsManager.createStaticBoxCollider(mesh, {
          friction: 0.5,
          restitution: 0.1,
          collisionGroup: CollisionGroups.STATIC_ENVIRONMENT,
          collisionMask: CollisionMasks.STATIC_ENVIRONMENT,
        });
      } else {
        // Use mesh collider for complex buildings (accurate)
        aggregate = this.physicsManager.createStaticMeshCollider(mesh, {
          friction: 0.5,
          restitution: 0.1,
          collisionGroup: CollisionGroups.STATIC_ENVIRONMENT,
          collisionMask: CollisionMasks.STATIC_ENVIRONMENT,
        });
      }

      if (aggregate) {
        colliderCount++;
      }

      // Re-freeze after physics creation
      mesh.freezeWorldMatrix();
    }

    return colliderCount;
  }

  /**
   * Create an invisible floor plane if no ground is detected
   */
  createFallbackGround(size: number = 200, y: number = 0): Mesh {
    console.log("[MapLoader] Creating fallback ground plane");

    const ground = MeshBuilder.CreateGround(
      "fallbackGround",
      { width: size, height: size },
      this.scene
    );

    ground.position.y = y;
    ground.isVisible = false; // Invisible but collidable
    ground.freezeWorldMatrix();

    // Add physics collider
    this.physicsManager.createStaticBoxCollider(ground, {
      friction: 0.8,
      restitution: 0.1,
      collisionGroup: CollisionGroups.STATIC_ENVIRONMENT,
      collisionMask: CollisionMasks.STATIC_ENVIRONMENT,
    });

    return ground;
  }

  /**
   * Get optimization statistics
   */
  getStats(): OptimizationStats {
    return { ...this.stats };
  }

  /**
   * Get all loaded meshes
   */
  getMeshes(): AbstractMesh[] {
    return this.loadedMeshes;
  }

  /**
   * Get the root transform node
   */
  getRootNode(): TransformNode | null {
    return this.rootNode;
  }

  /**
   * Find a spawn point in the map
   * Returns a safe position above ground level
   */
  findSpawnPoint(): Vector3 {
    // Try to find a spawn marker in the map
    for (const mesh of this.loadedMeshes) {
      if (
        mesh.name.toLowerCase().includes("spawn") ||
        mesh.name.toLowerCase().includes("start")
      ) {
        return mesh.position.clone().add(new Vector3(0, 1.5, 0));
      }
    }

    // Default spawn at center, elevated
    return new Vector3(0, 5, 0);
  }

  /**
   * Dispose of all map resources
   */
  dispose(): void {
    if (this.rootNode) {
      this.rootNode.dispose();
      this.rootNode = null;
    }
    this.loadedMeshes = [];
  }
}
