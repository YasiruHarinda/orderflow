import { createContext, useContext, useEffect, useReducer, type ReactNode } from "react";
import type { PlaceOrderRequest, Product } from "./api";

// The cart lives in the browser only. The server knows nothing about it until checkout,
// when it becomes one PlaceOrder request.

export const MAX_QUANTITY_PER_LINE = 100; // same rule as Order.MaxQuantityPerLine in the domain

export interface CartLine {
  sku: string;
  name: string;
  unitPrice: number;
  currency: string;
  quantity: number;
}

export type CartAction =
  | { type: "add"; product: Product }
  | { type: "setQuantity"; sku: string; quantity: number }
  | { type: "remove"; sku: string }
  | { type: "clear" };

export function cartReducer(lines: CartLine[], action: CartAction): CartLine[] {
  switch (action.type) {
    case "add": {
      const { product } = action;
      // One currency per order: the API rejects mixed currencies, so don't allow them here either.
      if (lines.length > 0 && lines[0]!.currency !== product.currency) return lines;
      const existing = lines.find((l) => l.sku === product.sku);
      if (existing) {
        return lines.map((l) =>
          l.sku === product.sku ? { ...l, quantity: Math.min(l.quantity + 1, MAX_QUANTITY_PER_LINE) } : l,
        );
      }
      return [
        ...lines,
        { sku: product.sku, name: product.name, unitPrice: product.price, currency: product.currency, quantity: 1 },
      ];
    }
    case "setQuantity": {
      if (!Number.isFinite(action.quantity) || action.quantity < 1) {
        return lines.filter((l) => l.sku !== action.sku);
      }
      const quantity = Math.min(Math.floor(action.quantity), MAX_QUANTITY_PER_LINE);
      return lines.map((l) => (l.sku === action.sku ? { ...l, quantity } : l));
    }
    case "remove":
      return lines.filter((l) => l.sku !== action.sku);
    case "clear":
      return [];
  }
}

export function cartTotal(lines: CartLine[]): number {
  // Work in cents so 0.1 + 0.2 style rounding never shows up in a total.
  const cents = lines.reduce((sum, l) => sum + Math.round(l.unitPrice * 100) * l.quantity, 0);
  return cents / 100;
}

export function toPlaceOrderRequest(customerId: string, lines: CartLine[]): PlaceOrderRequest {
  return {
    customerId,
    currency: lines[0]?.currency ?? "USD",
    lines: lines.map((l) => ({ sku: l.sku, productName: l.name, unitPrice: l.unitPrice, quantity: l.quantity })),
  };
}

const STORAGE_KEY = "orderflow.cart";

function loadCart(): CartLine[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as CartLine[]) : [];
  } catch {
    return [];
  }
}

interface CartContextValue {
  lines: CartLine[];
  dispatch: (action: CartAction) => void;
}

const CartContext = createContext<CartContextValue | null>(null);

export function CartProvider({ children }: { children: ReactNode }) {
  const [lines, dispatch] = useReducer(cartReducer, undefined, loadCart);

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(lines));
    } catch {
      // Storage blocked (private mode): the cart still works for this visit.
    }
  }, [lines]);

  return <CartContext.Provider value={{ lines, dispatch }}>{children}</CartContext.Provider>;
}

export function useCart(): CartContextValue {
  const value = useContext(CartContext);
  if (!value) throw new Error("useCart must be used inside <CartProvider>.");
  return value;
}
