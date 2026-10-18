import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, formatMoney, type Product } from "../api";
import { ErrorMessage } from "../components";

export function StockAdminPage() {
  const products = useQuery({ queryKey: ["products"], queryFn: api.listProducts });

  return (
    <section>
      <div className="section-head">
        <h1>Stock admin</h1>
        <p className="muted">
          Set how many units are physically in the warehouse. Available stock is that number minus what open orders
          have reserved.
        </p>
      </div>
      <div className="notice" role="note">
        This page has no login yet. In a real deployment the stock endpoint is the first thing to protect.
      </div>

      {products.isPending && <p className="muted">Loading products…</p>}
      {products.isError && <ErrorMessage error={products.error} />}
      {products.data && (
        <div className="table-wrap panel flush">
          <table>
            <thead>
              <tr>
                <th scope="col">Product</th>
                <th scope="col" className="num">Price</th>
                <th scope="col" className="num">Available</th>
                <th scope="col">Set on hand</th>
              </tr>
            </thead>
            <tbody>
              {products.data.items.map((p) => (
                <StockRow key={p.sku} product={p} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function StockRow({ product }: { product: Product }) {
  const queryClient = useQueryClient();
  const [value, setValue] = useState("");
  const [saved, setSaved] = useState(false);

  const update = useMutation({
    mutationFn: (quantity: number) => api.updateStock(product.sku, quantity),
    onSuccess: async () => {
      setValue("");
      setSaved(true);
      await queryClient.invalidateQueries({ queryKey: ["products"] });
    },
  });

  const quantity = Number(value);
  const valid = value.trim() !== "" && Number.isInteger(quantity) && quantity >= 0 && quantity <= 1_000_000;
  const inputId = `stock-${product.sku}`;

  return (
    <tr>
      <td>
        {product.name}
        <div className="muted small">{product.sku}</div>
      </td>
      <td className="num">{formatMoney(product.price, product.currency)}</td>
      <td className="num">{product.quantityAvailable}</td>
      <td>
        <form
          className="stock-form"
          onSubmit={(e) => {
            e.preventDefault();
            if (valid) update.mutate(quantity);
          }}
        >
          <label className="visually-hidden" htmlFor={inputId}>
            New quantity on hand for {product.name}
          </label>
          <input
            id={inputId}
            className="qty"
            type="number"
            min={0}
            max={1_000_000}
            placeholder="e.g. 50"
            value={value}
            onChange={(e) => {
              setValue(e.target.value);
              setSaved(false);
            }}
          />
          <button type="submit" className="button" disabled={!valid || update.isPending}>
            {update.isPending ? "Saving…" : "Save"}
          </button>
          {saved && <span className="saved" role="status">Saved</span>}
        </form>
        {update.isError && <ErrorMessage error={update.error} />}
      </td>
    </tr>
  );
}
