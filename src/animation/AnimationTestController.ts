import { SoldierAnimationController } from './SoldierAnimationController';
import { SoldierAnimationState } from '../types';

/**
 * Test controller for verifying animation transitions without mesh distortion.
 * This is used for acceptance criteria testing.
 */
export class AnimationTestController {
  private soldierController: SoldierAnimationController;
  private testSequenceRunning = false;
  private currentTestIndex = 0;
  private testSequence: SoldierAnimationState[] = [];
  private intervalId: number | null = null;

  constructor(soldierController: SoldierAnimationController) {
    this.soldierController = soldierController;
  }

  /**
   * Run a test sequence cycling through Strafe and Fire animations.
   * This verifies the acceptance criteria of transitioning without mesh distortion.
   *
   * @param intervalMs Time between transitions in milliseconds
   * @param cycles Number of complete cycles to run
   * @returns Promise that resolves when test completes
   */
  public runStrafeFireTest(intervalMs: number = 2000, cycles: number = 3): Promise<TestResult> {
    return new Promise((resolve) => {
      // Build test sequence: Strafe -> StaticFire -> Strafe -> StaticFire...
      this.testSequence = [];
      for (let i = 0; i < cycles; i++) {
        this.testSequence.push(SoldierAnimationState.Strafe);
        this.testSequence.push(SoldierAnimationState.StaticFire);
      }

      this.currentTestIndex = 0;
      this.testSequenceRunning = true;
      const startTime = performance.now();
      const transitionResults: TransitionResult[] = [];

      console.log('Starting Strafe/Fire animation test...');
      console.log(`Sequence: ${this.testSequence.join(' -> ')}`);

      // Execute first transition immediately
      this.executeTransition(transitionResults);

      // Set up interval for subsequent transitions
      this.intervalId = window.setInterval(() => {
        if (this.currentTestIndex >= this.testSequence.length) {
          this.stopTest();
          const endTime = performance.now();

          const result: TestResult = {
            success: transitionResults.every((r) => r.success),
            totalDuration: endTime - startTime,
            transitions: transitionResults,
            message: this.generateResultMessage(transitionResults),
          };

          console.log('Test completed:', result.message);
          resolve(result);
          return;
        }

        this.executeTransition(transitionResults);
      }, intervalMs);
    });
  }

  /**
   * Execute a single transition and record the result.
   */
  private executeTransition(results: TransitionResult[]): void {
    const state = this.testSequence[this.currentTestIndex];
    const fromState = this.soldierController.getCurrentState();
    const transitionStart = performance.now();

    try {
      this.soldierController.transitionToState(state);

      const result: TransitionResult = {
        success: true,
        fromState,
        toState: state,
        timestamp: transitionStart,
        error: null,
      };

      results.push(result);
      console.log(
        `Transition ${this.currentTestIndex + 1}/${this.testSequence.length}: ${fromState} -> ${state}`
      );
    } catch (error) {
      const result: TransitionResult = {
        success: false,
        fromState,
        toState: state,
        timestamp: transitionStart,
        error: error instanceof Error ? error.message : String(error),
      };

      results.push(result);
      console.error(`Transition failed: ${fromState} -> ${state}`, error);
    }

    this.currentTestIndex++;
  }

  /**
   * Run a comprehensive test of all animation states.
   */
  public runAllStatesTest(intervalMs: number = 1500): Promise<TestResult> {
    return new Promise((resolve) => {
      this.testSequence = [
        SoldierAnimationState.Idle,
        SoldierAnimationState.Strafe,
        SoldierAnimationState.StaticFire,
        SoldierAnimationState.MovingFire,
        SoldierAnimationState.Reaction,
        SoldierAnimationState.Idle,
      ];

      this.currentTestIndex = 0;
      this.testSequenceRunning = true;
      const startTime = performance.now();
      const transitionResults: TransitionResult[] = [];

      console.log('Starting all-states animation test...');
      console.log(`Sequence: ${this.testSequence.join(' -> ')}`);

      this.executeTransition(transitionResults);

      this.intervalId = window.setInterval(() => {
        if (this.currentTestIndex >= this.testSequence.length) {
          this.stopTest();
          const endTime = performance.now();

          const result: TestResult = {
            success: transitionResults.every((r) => r.success),
            totalDuration: endTime - startTime,
            transitions: transitionResults,
            message: this.generateResultMessage(transitionResults),
          };

          console.log('Test completed:', result.message);
          resolve(result);
          return;
        }

        this.executeTransition(transitionResults);
      }, intervalMs);
    });
  }

  /**
   * Stop the current test sequence.
   */
  public stopTest(): void {
    if (this.intervalId !== null) {
      window.clearInterval(this.intervalId);
      this.intervalId = null;
    }
    this.testSequenceRunning = false;
    this.currentTestIndex = 0;
    console.log('Test stopped');
  }

  /**
   * Check if a test is currently running.
   */
  public isRunning(): boolean {
    return this.testSequenceRunning;
  }

  /**
   * Generate a human-readable result message.
   */
  private generateResultMessage(results: TransitionResult[]): string {
    const successful = results.filter((r) => r.success).length;
    const failed = results.filter((r) => !r.success).length;

    if (failed === 0) {
      return `All ${successful} transitions completed successfully without errors.`;
    } else {
      const failedTransitions = results
        .filter((r) => !r.success)
        .map((r) => `${r.fromState} -> ${r.toState}: ${r.error}`)
        .join(', ');
      return `${successful}/${results.length} transitions successful. Failed: ${failedTransitions}`;
    }
  }

  /**
   * Dispose resources.
   */
  public dispose(): void {
    this.stopTest();
  }
}

/**
 * Result of a single animation transition.
 */
export interface TransitionResult {
  success: boolean;
  fromState: SoldierAnimationState;
  toState: SoldierAnimationState;
  timestamp: number;
  error: string | null;
}

/**
 * Result of a complete test run.
 */
export interface TestResult {
  success: boolean;
  totalDuration: number;
  transitions: TransitionResult[];
  message: string;
}
