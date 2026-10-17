import { describe, expect, it } from "vitest";
import { ApiError, toApiError } from "./api";

const json = (status: number, body: unknown) =>
  new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/problem+json" } });

describe("toApiError", () => {
  it("reads every field error from a validation problem", async () => {
    const error = await toApiError(
      json(400, {
        title: "One or more validation errors occurred.",
        errors: { CustomerId: ["'Customer Id' must not be empty."], Lines: ["'Lines' must not be empty."] },
      }),
    );
    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(400);
    expect(error.messages).toEqual(["'Customer Id' must not be empty.", "'Lines' must not be empty."]);
  });

  it("uses the detail of a plain problem", async () => {
    const error = await toApiError(json(404, { title: "Order.NotFound", detail: "Order 123 was not found." }));
    expect(error.messages).toEqual(["Order 123 was not found."]);
  });

  it("explains the gateway's rate limit, which has no JSON body", async () => {
    const error = await toApiError(new Response("", { status: 429 }));
    expect(error.messages[0]).toMatch(/Too many requests/);
  });
});
