import { describe, it, expect } from 'vitest';
import { AppState } from './AppState';

describe('AppState', () => {
  it('should have LOADING state', () => {
    expect(AppState.LOADING).toBe('LOADING');
  });

  it('should have MAIN_MENU state', () => {
    expect(AppState.MAIN_MENU).toBe('MAIN_MENU');
  });

  it('should have GAME_PLAY state', () => {
    expect(AppState.GAME_PLAY).toBe('GAME_PLAY');
  });

  it('should have GAME_OVER state', () => {
    expect(AppState.GAME_OVER).toBe('GAME_OVER');
  });

  it('should have exactly 4 states', () => {
    const states = Object.values(AppState);
    expect(states).toHaveLength(4);
  });

  it('should have unique values for each state', () => {
    const states = Object.values(AppState);
    const uniqueStates = new Set(states);
    expect(uniqueStates.size).toBe(states.length);
  });
});
