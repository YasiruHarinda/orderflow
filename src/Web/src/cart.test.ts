import { describe, expect, it } from "vitest";
import { cartReducer, cartTotal, MAX_QUANTITY_PER_LINE, toPlaceOrderRequest, type CartLine } from "./cart";
import type { Product } from "./api";

const keyboard: Product = { sku: "KB-001", name: "Mechanical Keyboard", price: 89.99, currency: "USD", quantityAvailable: 50 };
const cable: Product = { sku: "CB-001", name: "USB-C Cable", price: 9.99, currency: "USD", quantityAvailable: 500 };

describe("cartReducer", () => {
  it("adds a new product with quantity 1, then increments it", () => {
    let lines = cartReducer([], { type: "add", product: keyboard });
    expect(lines).toEqual([{ sku: "KB-001", name: "Mechanical Keyboard", unitPrice: 89.99, currency: "USD", quantity: 1 }]);

    lines = cartReducer(lines, { type: "add", product: keyboard });
    expect(lines[0]!.quantity).toBe(2);
  });

  it("never goes above the domain's maximum quantity per line", () => {
    let lines = cartReducer([], { type: "add", product: keyboard });
    lines = cartReducer(lines, { type: "setQuantity", sku: "KB-001", quantity: 500 });
    expect(lines[0]!.quantity).toBe(MAX_QUANTITY_PER_LINE);
  });

  it("removes a line when its quantity is set below 1", () => {
    const lines = cartReducer(cartReducer([], { type: "add", product: keyboard }), {
      type: "setQuantity",
      sku: "KB-001",
      quantity: 0,
    });
    expect(lines).toEqual([]);
  });

  it("refuses a second currency, because one order has one currency", () => {
    const lines = cartReducer([], { type: "add", product: keyboard });
    const euro = { ...cable, currency: "EUR" };
    expect(cartReducer(lines, { type: "add", product: euro })).toBe(lines);
  });
});

describe("cartTotal", () => {
  it("adds prices without floating-point drift", () => {
    const lines: CartLine[] = [
      { sku: "A", name: "A", unitPrice: 0.1, currency: "USD", quantity: 1 },
      { sku: "B", name: "B", unitPrice: 0.2, currency: "USD", quantity: 1 },
    ];
    expect(cartTotal(lines)).toBe(0.3);
  });
});

describe("toPlaceOrderRequest", () => {
  it("maps the cart to the API's PlaceOrder shape", () => {
    const lines = cartReducer(cartReducer([], { type: "add", product: keyboard }), { type: "add", product: cable });
    expect(toPlaceOrderRequest("cust-1", lines)).toEqual({
      customerId: "cust-1",
      currency: "USD",
      lines: [
        { sku: "KB-001", productName: "Mechanical Keyboard", unitPrice: 89.99, quantity: 1 },
        { sku: "CB-001", productName: "USB-C Cable", unitPrice: 9.99, quantity: 1 },
      ],
    });
  });
});
