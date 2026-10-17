// Typed client for the OrderFlow API. Every call goes to /api/v1/... on the same origin:
// the YARP gateway serves this site AND routes /api to Orders and Inventory, so the
// browser never needs CORS. The types mirror the C# response records exactly.

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
}

/** Inventory: ProductResponse */
export interface Product {
  sku: string;
  name: string;
  price: number;
  currency: string;
  quantityAvailable: number;
}

export type OrderStatus = "Submitted" | "AwaitingStock" | "Confirmed" | "Rejected";

/** Orders: OrderResponse */
export interface Order {
  id: string;
  customerId: string;
  status: OrderStatus;
  total: number;
  currency: string;
  createdAt: string;
  rejectionReason: string | null;
  lines: OrderLine[];
}

export interface OrderLine {
  sku: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

/** Orders: OrderSummaryResponse */
export interface OrderSummary {
  id: string;
  status: OrderStatus;
  total: number;
  currency: string;
  createdAt: string;
}

export interface PlaceOrderRequest {
  customerId: string;
  currency: string;
  lines: { sku: string; productName: string; unitPrice: number; quantity: number }[];
}

/**
 * An error response from the API. The services answer with RFC 9457 "problem details";
 * validation failures also carry an "errors" map of field name -> messages.
 */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail: string | undefined,
    readonly fieldErrors: Record<string, string[]>,
  ) {
    super(detail ?? title);
    this.name = "ApiError";
  }

  /** Every message in one list, for showing to a person. */
  get messages(): string[] {
    const fields = Object.values(this.fieldErrors).flat();
    if (fields.length > 0) return fields;
    return [this.detail ?? this.title];
  }
}

export async function toApiError(response: Response): Promise<ApiError> {
  let body: { title?: string; detail?: string; errors?: Record<string, string[]> } = {};
  try {
    body = await response.json();
  } catch {
    // Not JSON (e.g. a 502 from a proxy, or 429 from the rate limiter). Fall back to the status.
  }
  const title =
    body.title ??
    (response.status === 429 ? "Too many requests. Wait a few seconds and try again." : `Request failed (${response.status})`);
  return new ApiError(response.status, title, body.detail, body.errors ?? {});
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: { Accept: "application/json", ...(init?.body ? { "Content-Type": "application/json" } : {}), ...init?.headers },
  });
  if (!response.ok) throw await toApiError(response);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const api = {
  listProducts: () => request<PagedResult<Product>>("/api/v1/products?pageSize=100"),

  placeOrder: (order: PlaceOrderRequest) =>
    request<{ orderId: string }>("/api/v1/orders", { method: "POST", body: JSON.stringify(order) }),

  getOrder: (orderId: string) => request<Order>(`/api/v1/orders/${encodeURIComponent(orderId)}`),

  listOrders: (customerId: string) =>
    request<PagedResult<OrderSummary>>(`/api/v1/orders?customerId=${encodeURIComponent(customerId)}&pageSize=50`),

  updateStock: (sku: string, quantityOnHand: number) =>
    request<void>(`/api/v1/products/${encodeURIComponent(sku)}/stock`, {
      method: "PUT",
      body: JSON.stringify({ quantityOnHand }),
    }),
};

export function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency }).format(amount);
}
