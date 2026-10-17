import type { OrderStatus } from "./api";

// The order's state machine, as the customer sees it:
//   Submitted -> AwaitingStock -> Confirmed
//                      \-------> Rejected

export const isFinal = (status: OrderStatus | undefined): boolean =>
  status === "Confirmed" || status === "Rejected";

export const statusLabel: Record<OrderStatus, string> = {
  Submitted: "Placed",
  AwaitingStock: "Reserving stock",
  Confirmed: "Confirmed",
  Rejected: "Rejected",
};

export type StepState = "done" | "current" | "todo" | "failed";

export interface TimelineStep {
  key: string;
  title: string;
  description: string;
  state: StepState;
}

/** The three steps shown on the order page, and how far the order has got. */
export function timeline(status: OrderStatus, rejectionReason?: string | null): TimelineStep[] {
  const placed: TimelineStep = {
    key: "placed",
    title: "Order placed",
    description: "Orders saved your order and queued an OrderSubmitted event in its outbox.",
    state: status === "Submitted" ? "current" : "done",
  };

  const reserving: TimelineStep = {
    key: "reserving",
    title: "Reserving stock",
    description: "Inventory receives the event over RabbitMQ and reserves every line, or none of them.",
    state: status === "Submitted" ? "todo" : status === "AwaitingStock" ? "current" : "done",
  };

  const outcome: TimelineStep =
    status === "Rejected"
      ? {
          key: "outcome",
          title: "Rejected",
          description: rejectionReason ?? "The order could not be completed.",
          state: "failed",
        }
      : {
          key: "outcome",
          title: "Confirmed",
          description:
            status === "Confirmed"
              ? "Stock is committed and Notifications has told the customer."
              : "Waiting for Inventory's answer. If none comes, the order is rejected after a timeout.",
          state: status === "Confirmed" ? "done" : "todo",
        };

  return [placed, reserving, outcome];
}
