import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { api, formatMoney } from "../api";
import { getCustomerId } from "../customer";
import { isFinal } from "../orderStatus";
import { ErrorMessage, StatusPill } from "../components";

export function MyOrdersPage() {
  const customerId = getCustomerId();

  // Refresh every 3 s while any order is still in progress.
  const orders = useQuery({
    queryKey: ["orders", "customer", customerId],
    queryFn: () => api.listOrders(customerId),
    refetchInterval: (query) =>
      query.state.data?.items.some((o) => !isFinal(o.status)) ? 3000 : false,
  });

  return (
    <section>
      <div className="section-head">
        <h1>My orders</h1>
        <p className="muted">
          Orders placed from this browser. Your customer id is <code>{customerId}</code>.
        </p>
      </div>

      {orders.isPending && <p className="muted">Loading orders…</p>}
      {orders.isError && <ErrorMessage error={orders.error} />}
      {orders.data && orders.data.items.length === 0 && (
        <div className="panel">
          <p>
            You haven't placed an order yet. <Link to="/">Go to the shop</Link> to place one.
          </p>
        </div>
      )}
      {orders.data && orders.data.items.length > 0 && (
        <div className="table-wrap panel flush">
          <table>
            <thead>
              <tr>
                <th scope="col">Placed</th>
                <th scope="col">Order</th>
                <th scope="col">Status</th>
                <th scope="col" className="num">Total</th>
              </tr>
            </thead>
            <tbody>
              {orders.data.items.map((o) => (
                <tr key={o.id}>
                  <td>{new Date(o.createdAt).toLocaleString()}</td>
                  <td>
                    <Link to={`/orders/${o.id}`} className="mono">
                      {o.id.slice(0, 8)}…
                    </Link>
                  </td>
                  <td>
                    <StatusPill status={o.status} />
                  </td>
                  <td className="num">{formatMoney(o.total, o.currency)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
