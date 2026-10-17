import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router";
import { api, formatMoney } from "../api";
import { isFinal, timeline } from "../orderStatus";
import { ErrorMessage, StatusPill } from "../components";

export function OrderPage() {
  const { orderId = "" } = useParams();

  // Poll once a second until the saga reaches a final status, then stop.
  const order = useQuery({
    queryKey: ["orders", orderId],
    queryFn: () => api.getOrder(orderId),
    refetchInterval: (query) => (isFinal(query.state.data?.status) ? false : 1000),
  });

  if (order.isPending) return <p className="muted">Loading order…</p>;
  if (order.isError) {
    return (
      <section className="panel">
        <h1>Order</h1>
        <ErrorMessage error={order.error} />
        <p>
          <Link to="/orders">See all your orders</Link>
        </p>
      </section>
    );
  }

  const o = order.data;
  const steps = timeline(o.status, o.rejectionReason);

  return (
    <div className="order-page">
      <div className="section-head">
        <p className="eyebrow">Order {o.id}</p>
        <h1>
          {o.status === "Confirmed"
            ? "Your order is confirmed"
            : o.status === "Rejected"
              ? "Your order was rejected"
              : "Your order is on its way through the system"}
        </h1>
        <p className="muted">
          Placed {new Date(o.createdAt).toLocaleString()} · <StatusPill status={o.status} />
          {!isFinal(o.status) && <span className="live"> Checking every second</span>}
        </p>
      </div>

      <div className="order-layout">
        <ol className="timeline" aria-label="Order progress">
          {steps.map((step) => (
            <li key={step.key} className={`step step-${step.state}`} aria-current={step.state === "current" ? "step" : undefined}>
              <span className="step-dot" aria-hidden="true" />
              <div>
                <h2 className="step-title">{step.title}</h2>
                <p className="step-text">{step.description}</p>
              </div>
            </li>
          ))}
        </ol>

        <section className="panel" aria-labelledby="lines-title">
          <h2 id="lines-title">Items</h2>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th scope="col">Product</th>
                  <th scope="col" className="num">Qty</th>
                  <th scope="col" className="num">Price</th>
                  <th scope="col" className="num">Total</th>
                </tr>
              </thead>
              <tbody>
                {o.lines.map((line) => (
                  <tr key={line.sku}>
                    <td>
                      {line.productName}
                      <div className="muted small">{line.sku}</div>
                    </td>
                    <td className="num">{line.quantity}</td>
                    <td className="num">{formatMoney(line.unitPrice, o.currency)}</td>
                    <td className="num">{formatMoney(line.lineTotal, o.currency)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr>
                  <th scope="row" colSpan={3}>Order total</th>
                  <td className="num"><strong>{formatMoney(o.total, o.currency)}</strong></td>
                </tr>
              </tfoot>
            </table>
          </div>
          <p className="actions">
            <Link to="/" className="button">Keep shopping</Link>
            <Link to="/orders" className="link-button">All my orders</Link>
          </p>
        </section>
      </div>
    </div>
  );
}
