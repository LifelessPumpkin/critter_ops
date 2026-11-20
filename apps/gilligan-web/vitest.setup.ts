import "@testing-library/jest-dom/vitest";

// Use jsdom's browser storage even when Node exposes its own localStorage global.
const environment = globalThis as typeof globalThis & { jsdom?: { window: Window } };
if (environment.jsdom) {
  Object.defineProperty(globalThis, "localStorage", {
    configurable: true,
    value: environment.jsdom.window.localStorage,
  });
}
