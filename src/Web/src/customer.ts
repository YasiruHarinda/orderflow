// There is no login yet, so the browser plays one customer: a random id,
// kept in localStorage so "My orders" still works after a reload.

const STORAGE_KEY = "orderflow.customerId";

let fallbackId: string | undefined;

export function getCustomerId(): string {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) return stored;
    const id = crypto.randomUUID();
    localStorage.setItem(STORAGE_KEY, id);
    return id;
  } catch {
    // Storage blocked (private mode): keep one id for this visit.
    fallbackId ??= crypto.randomUUID();
    return fallbackId;
  }
}
