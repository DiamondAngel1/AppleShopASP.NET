import {
  Link,
  Navigate,
  Outlet,
  Route,
  Routes,
  useLocation,
} from 'react-router-dom';

import { useAppSelector } from './app/hooks';

import { Layout } from './components/Layout';
import { PageTitle } from './components/UI';

import { HomePage } from './pages/HomePage';
import { CatalogPage } from './pages/CatalogPage';
import { ProductPage } from './pages/ProductPage';
import { AuthPage } from './pages/AuthPage';
import { CartPage } from './pages/CartPage';

import {
  FavoritesPage,
  OrdersPage,
  ProfilePage,
} from './pages/AccountPage';

import {
  AdminPage,
  ProductEditorPage,
} from './pages/AdminPage';

function ProtectedRoute({
                          admin = false,
                        }: {
  admin?: boolean;
}) {
  const { token, user } = useAppSelector(
      (state) => state.auth,
  );

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (admin && !user?.roles.includes('Admin')) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
}

function InformationPage() {
  const location = useLocation();
  const contact = location.pathname === '/contact';

  return (
      <section className="page">
        <PageTitle
            eyebrow={contact ? 'GET IN TOUCH' : 'OUR STORY'}
            title={contact ? 'Контакти' : 'Про'}
            accent={contact ? 'AppleStore' : 'проєкт'}
        />

        <div className="panel max-w-3xl space-y-5 p-7 text-sm leading-8 text-muted">
          {contact ? (
              <>
                <p>
                  Це демонстраційний магазин. Контактні дані
                  продавця потрібно додати перед публічним запуском.
                </p>

                <Link to="/catalog" className="text-accent">
                  Перейти до каталогу →
                </Link>
              </>
          ) : (
              <>
                <p>
                  AppleStore — демонстраційний інтернет-магазин
                  із каталогом Apple, кошиком, профілем та
                  адміністративною панеллю.
                </p>

                <p>
                  Frontend: React, TypeScript, Tailwind CSS,
                  Redux Toolkit і RTK Query. Backend:
                  ASP.NET Core та PostgreSQL.
                </p>
              </>
          )}
        </div>
      </section>
  );
}

function NotFoundPage() {
  return (
      <section className="page py-24 text-center">
        <p className="text-8xl font-bold text-accent">404</p>

        <h1 className="mt-5 text-2xl">
          Цю сторінку не знайдено
        </h1>

        <Link to="/" className="button mt-7">
          На головну
        </Link>
      </section>
  );
}

export default function App() {
  return (
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<HomePage />} />
          <Route path="catalog" element={<CatalogPage />} />
          <Route path="product/:slug" element={<ProductPage />} />

          <Route
              path="login"
              element={<AuthPage key="login" />}
          />

          <Route
              path="register"
              element={<AuthPage key="register" registration />}
          />

          <Route path="about" element={<InformationPage />} />
          <Route path="contact" element={<InformationPage />} />

          <Route element={<ProtectedRoute />}>
            <Route path="profile" element={<ProfilePage />} />
            <Route path="favorites" element={<FavoritesPage />} />
            <Route path="orders" element={<OrdersPage />} />
            <Route path="cart" element={<CartPage />} />

            <Route
                path="checkout"
                element={<CartPage checkout />}
            />
          </Route>

          <Route element={<ProtectedRoute admin />}>
            <Route path="admin" element={<AdminPage />} />

            <Route
                path="admin/products/new"
                element={<ProductEditorPage />}
            />

            <Route
                path="admin/products/:slug"
                element={<ProductEditorPage />}
            />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
  );
}