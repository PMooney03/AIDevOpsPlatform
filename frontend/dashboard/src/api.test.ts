import { describe, expect, it } from "vitest";
import { canOperate, statusTone } from "./api";

describe("statusTone", () => {
  it("marks healthy as ok and unhealthy as bad", () => {
    expect(statusTone("Healthy")).toBe("ok");
    expect(statusTone("Unhealthy")).toBe("bad");
    expect(statusTone("Degraded")).toBe("warn");
  });
});

describe("canOperate", () => {
  it("allows operators and administrators only", () => {
    expect(canOperate("Viewer")).toBe(false);
    expect(canOperate("Operator")).toBe(true);
    expect(canOperate("Administrator")).toBe(true);
  });
});
