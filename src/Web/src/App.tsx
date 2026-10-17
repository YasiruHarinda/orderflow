import { NavLink, Route, Routes } from "react-router";
import { useCart } from "./cart";
import { ShopPage } from "./pages/ShopPage";
import { OrderPage } from "./pages/OrderPage";
import { MyOrdersPage } from "./pages/MyOrdersPage";
import { StockAdminPage } from "./pages/StockAdminPage";

export function App() {
  const { lines } = useCart();
  const itemCount = lines.reduce((n, l) => n + l.quantity, 0);

  return (
    <div className="app">
      <header className="topbar">
        <div className="topbar-inner">
          <NavLink to="/" className="brand" end>
            <span className="brand-mark" aria-hidden="true" />
            OrderFlow
          </NavLink>
          <nav aria-label="Main">
            <NavLink to="/" end>
              Shop{itemCount > 0 && <span className="badge" aria-label={`${itemCount} items in cart`}>{itemCount}</span>}
            </NavLink>
            <NavLink to="/orders">My orders</NavLink>
            <NavLink to="/admin/stock">Stock admin</NavLink>
          </nav>
        </div>
      </header>

      <main className="content">
        <Routes>
          <Route path="/" element={<ShopPage />} />
          <Route path="/orders" element={<MyOrdersPage />} />
          <Route path="/orders/:orderId" element={<OrderPage />} />
          <Route path="/admin/stock" element={<StockAdminPage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>

      <footer className="footer">
        Demo store for the OrderFlow microservices. No real payments, no real customers.
      </footer>
    </div>
  );
}

function NotFound() {
  return (
    <section className="panel">
      <h1>Page not found</h1>
      <p>
        That address doesn't exist. <NavLink to="/">Go back to the shop</NavLink>.
      </p>
    </section>
  );
}
