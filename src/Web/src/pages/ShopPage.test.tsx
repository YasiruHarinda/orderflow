import { describe, expect, it } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { CartProvider, useCart } from "../cart";
import { ProductCard } from "./ShopPage";
import type { Product } from "../api";

function CartCount() {
  const { lines } = useCart();
  return <output aria-label="cart count">{lines.reduce((n, l) => n + l.quantity, 0)}</output>;
}

const renderCard = (product: Product) =>
  render(
    <CartProvider>
      <ul>
        <ProductCard product={product} />
      </ul>
      <CartCount />
    </CartProvider>,
  );

describe("ProductCard", () => {
  it("adds the product to the cart", () => {
    renderCard({ sku: "KB-001", name: "Mechanical Keyboard", price: 89.99, currency: "USD", quantityAvailable: 50 });

    expect(screen.getByText("50 available")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Add to cart" }));

    expect(screen.getByLabelText("cart count")).toHaveTextContent("1");
    expect(screen.getByRole("button", { name: "Add another (1 in cart)" })).toBeInTheDocument();
  });

  it("still lets you order an out-of-stock product, so the rejection can be seen", () => {
    renderCard({ sku: "LT-001", name: "Laptop Stand", price: 39, currency: "USD", quantityAvailable: 0 });

    expect(screen.getByText("Out of stock")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Add anyway" }));
    expect(screen.getByLabelText("cart count")).toHaveTextContent("1");
  });
});
