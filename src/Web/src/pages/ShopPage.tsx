import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { api, formatMoney, type Product } from "../api";
import { cartTotal, MAX_QUANTITY_PER_LINE, toPlaceOrderRequest, useCart } from "../cart";
import { getCustomerId } from "../customer";
import { ErrorMessage } from "../components";

export function ShopPage() {
  const products = useQuery({ queryKey: ["products"], queryFn: api.listProducts });

  return (
    <div className="shop">
      <section className="catalogue" aria-labelledby="catalogue-title">
        <div className="section-head">
          <h1 id="catalogue-title">Products</h1>
          <p className="muted">
            Stock comes live from the Inventory service. Out-of-stock items can still be ordered, so you can watch
            Inventory reject them.
          </p>
        </div>

        {products.isPending && <p className="muted">Loading products…</p>}
        {products.isError && <ErrorMessage error={products.error} />}
        {products.data && (
          <ul className="product-grid">
            {products.data.items.map((p) => (
              <ProductCard key={p.sku} product={p} />
            ))}
          </ul>
        )}
      </section>

      <Cart />
    </div>
  );
}

export function ProductCard({ product }: { product: Product }) {
  const { lines, dispatch } = useCart();
  const inCart = lines.find((l) => l.sku === product.sku)?.quantity ?? 0;
  const outOfStock = product.quantityAvailable === 0;
  const low = !outOfStock && product.quantityAvailable <= 5;

  return (
    <li className="product">
      <div className="product-sku">{product.sku}</div>
      <h2 className="product-name">{product.name}</h2>
      <div className="product-price">{formatMoney(product.price, product.currency)}</div>
      <div className={`stock ${outOfStock ? "stock-out" : low ? "stock-low" : "stock-ok"}`}>
        {outOfStock ? "Out of stock" : `${product.quantityAvailable} available`}
      </div>
      <button
        type="button"
        className={outOfStock ? "button button-quiet" : "button"}
        onClick={() => dispatch({ type: "add", product })}
        disabled={inCart >= MAX_QUANTITY_PER_LINE}
      >
        {inCart > 0 ? `Add another (${inCart} in cart)` : outOfStock ? "Add anyway" : "Add to cart"}
      </button>
    </li>
  );
}

function Cart() {
  const { lines, dispatch } = useCart();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const placeOrder = useMutation({
    mutationFn: () => api.placeOrder(toPlaceOrderRequest(getCustomerId(), lines)),
    onSuccess: ({ orderId }) => {
      dispatch({ type: "clear" });
      void queryClient.invalidateQueries({ queryKey: ["orders"] });
      navigate(`/orders/${orderId}`);
    },
  });

  const currency = lines[0]?.currency ?? "USD";

  return (
    <aside className="cart panel" aria-labelledby="cart-title">
      <h2 id="cart-title">Cart</h2>
      {lines.length === 0 ? (
        <p className="muted">Your cart is empty. Add a product to place an order.</p>
      ) : (
        <>
          <ul className="cart-lines">
            {lines.map((line) => (
              <li key={line.sku} className="cart-line">
                <div className="cart-line-name">
                  <span>{line.name}</span>
                  <span className="muted">{formatMoney(line.unitPrice, line.currency)} each</span>
                </div>
                <label className="visually-hidden" htmlFor={`qty-${line.sku}`}>
                  Quantity of {line.name}
                </label>
                <input
                  id={`qty-${line.sku}`}
                  className="qty"
                  type="number"
                  min={1}
                  max={MAX_QUANTITY_PER_LINE}
                  value={line.quantity}
                  onChange={(e) => dispatch({ type: "setQuantity", sku: line.sku, quantity: e.target.valueAsNumber })}
                />
                <button
                  type="button"
                  className="link-button"
                  onClick={() => dispatch({ type: "remove", sku: line.sku })}
                  aria-label={`Remove ${line.name}`}
                >
                  Remove
                </button>
              </li>
            ))}
          </ul>
          <div className="cart-total">
            <span>Total</span>
            <strong>{formatMoney(cartTotal(lines), currency)}</strong>
          </div>
          {placeOrder.isError && <ErrorMessage error={placeOrder.error} />}
          <button
            type="button"
            className="button button-primary"
            onClick={() => placeOrder.mutate()}
            disabled={placeOrder.isPending}
          >
            {placeOrder.isPending ? "Placing order…" : "Place order"}
          </button>
        </>
      )}
    </aside>
  );
}
