import { ApiError } from "./api";
import type { OrderStatus } from "./api";
import { statusLabel } from "./orderStatus";

export function StatusPill({ status }: { status: OrderStatus }) {
  return <span className={`pill pill-${status.toLowerCase()}`}>{statusLabel[status]}</span>;
}

/** Shows an API error (problem details) or any other error in plain words. */
export function ErrorMessage({ error }: { error: unknown }) {
  const messages =
    error instanceof ApiError
      ? error.messages
      : ["Can't reach the OrderFlow API. Check that the gateway is running, then try again."];
  return (
    <div className="error" role="alert">
      {messages.length === 1 ? (
        <p>{messages[0]}</p>
      ) : (
        <ul>
          {messages.map((m) => (
            <li key={m}>{m}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
