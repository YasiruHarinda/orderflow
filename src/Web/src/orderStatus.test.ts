import { describe, expect, it } from "vitest";
import { isFinal, timeline } from "./orderStatus";

const states = (status: Parameters<typeof timeline>[0]) => timeline(status).map((s) => s.state);

describe("timeline", () => {
  it("follows the order's state machine", () => {
    expect(states("Submitted")).toEqual(["current", "todo", "todo"]);
    expect(states("AwaitingStock")).toEqual(["done", "current", "todo"]);
    expect(states("Confirmed")).toEqual(["done", "done", "done"]);
    expect(states("Rejected")).toEqual(["done", "done", "failed"]);
  });

  it("shows the rejection reason on the last step", () => {
    const last = timeline("Rejected", "Not enough stock for LT-001").at(-1)!;
    expect(last.title).toBe("Rejected");
    expect(last.description).toBe("Not enough stock for LT-001");
  });
});

describe("isFinal", () => {
  it("is true only for Confirmed and Rejected", () => {
    expect(isFinal("Confirmed")).toBe(true);
    expect(isFinal("Rejected")).toBe(true);
    expect(isFinal("Submitted")).toBe(false);
    expect(isFinal("AwaitingStock")).toBe(false);
    expect(isFinal(undefined)).toBe(false);
  });
});
