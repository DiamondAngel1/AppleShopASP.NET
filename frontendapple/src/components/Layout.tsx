import { useEffect, useState } from 'react';

import {
    Link,
    NavLink,
    Outlet,
    useLocation,
    useNavigate,
} from 'react-router-dom';

import {
    ArrowUpRight,
    Heart,
    Home,
    Menu,
    ShoppingBag,
    UserRound,
    X,
} from 'lucide-react';

import {
    useCartQuery,
    useProfileQuery,
} from '../api/api';

import {
    useAppDispatch,
    useAppSelector,
} from '../app/hooks';

import { logout } from '../features/authSlice';

import { imageUrl, initials } from '../utils';

const links = [
    { to: '/', label: 'Home' },
    { to: '/catalog', label: 'Shop' },
    { to: '/catalog?sort=newest', label: 'New Arrivals' },
    { to: '/catalog#categories', label: 'Collections' },
    { to: '/about', label: 'About' },
    { to: '/contact', label: 'Contact' },
];

export function Layout() {
    const [mobile, setMobile] = useState(false);
    const [menu, setMenu] = useState(false);

    const { token, user } = useAppSelector(
        (state) => state.auth,
    );

    const dispatch = useAppDispatch();
    const navigate = useNavigate();
    const location = useLocation();

    const { data: cart } = useCartQuery(undefined, {
        skip: !token,
    });

    const { data: profile } = useProfileQuery(undefined, {
        skip: !token,
    });

    const currentUser = profile ?? user;

    useEffect(() => {
        setMobile(false);
        setMenu(false);
        window.scrollTo({ top: 0 });
    }, [location.pathname, location.search]);

    function signOut() {
        dispatch(logout());
        navigate('/');
    }

    return (
        <div className="site-shell">
            <header className="site-header">
                <Link to="/" className="logo">
                    APPLE<span>STORE</span>
                </Link>

                <div className="mt-6 flex items-center justify-center gap-3 md:gap-5">
                    <button
                        className="icon-button lg:hidden"
                        onClick={() => setMobile(!mobile)}
                        aria-label="Відкрити меню"
                    >
                        {mobile ? <X size={19} /> : <Menu size={19} />}
                    </button>

                    <nav className="nav-pill hidden lg:flex">
                        <Link
                            to="/"
                            className="flex items-center px-3 text-deep"
                            aria-label="На головну"
                        >
                            <Home size={14} />
                        </Link>

                        {links.map((link) => (
                            <NavLink
                                key={link.label}
                                to={link.to}
                                end={link.to === '/'}
                                className={({ isActive }) =>
                                    `nav-link ${
                                        isActive &&
                                        (
                                            link.label === 'Home' ||
                                            (
                                                link.label === 'Shop' &&
                                                !location.search
                                            )
                                        )
                                            ? 'selected'
                                            : ''
                                    }`
                                }
                            >
                                {link.label}
                            </NavLink>
                        ))}
                    </nav>

                    <div className="flex items-center gap-2">
                        <Link
                            to="/cart"
                            className="icon-button relative"
                            aria-label="Кошик"
                        >
                            <ShoppingBag size={19} />

                            {!!cart?.totalQuantity && (
                                <span className="counter">
                  {cart.totalQuantity}
                </span>
                            )}
                        </Link>

                        <Link
                            to="/favorites"
                            className="icon-button"
                            aria-label="Обране"
                        >
                            <Heart size={19} />
                        </Link>

                        {token && currentUser ? (
                            <div className="relative">
                                <button
                                    className="avatar"
                                    onClick={() => setMenu(!menu)}
                                    aria-label="Меню профілю"
                                    aria-expanded={menu}
                                >
                                    {currentUser.avatarUrl ? (
                                        <img
                                            src={imageUrl(currentUser.avatarUrl)}
                                            alt=""
                                        />
                                    ) : (
                                        initials(
                                            currentUser.firstName,
                                            currentUser.lastName,
                                        )
                                    )}
                                </button>

                                {menu && (
                                    <div className="profile-menu">
                                        <p className="mb-2 border-b border-white/10 p-3 text-sm">
                                            {currentUser.firstName}{' '}
                                            {currentUser.lastName}
                                        </p>

                                        <Link to="/profile">Профіль</Link>
                                        <Link to="/orders">Мої замовлення</Link>

                                        {currentUser.roles.includes('Admin') && (
                                            <Link to="/admin">Адмін-панель</Link>
                                        )}

                                        <button
                                            onClick={signOut}
                                            className="text-red-300"
                                        >
                                            Вийти
                                        </button>
                                    </div>
                                )}
                            </div>
                        ) : (
                            <Link
                                to="/login"
                                className="icon-button"
                                aria-label="Увійти"
                            >
                                <UserRound size={19} />
                            </Link>
                        )}
                    </div>
                </div>

                {mobile && (
                    <nav className="panel mt-5 grid p-3 lg:hidden">
                        {links.map((link) => (
                            <Link
                                key={link.label}
                                to={link.to}
                                className="rounded-xl px-4 py-3 hover:bg-white/10"
                            >
                                {link.label}
                            </Link>
                        ))}
                    </nav>
                )}
            </header>

            <main className="min-h-[60vh]">
                <Outlet />
            </main>

            <footer className="mt-24 border-t border-white/10 pt-12">
                <div className="grid gap-10 md:grid-cols-4">
                    <div className="md:col-span-2">
                        <Link to="/" className="logo text-left">
                            APPLE<span>STORE</span>
                        </Link>

                        <p className="mt-4 max-w-sm text-sm leading-7 text-muted">
                            Техніка, що поєднує дизайн, продуктивність
                            і твій щоденний ритм.
                        </p>

                        <p className="mt-3 text-xs text-muted">
                            Демонстраційний магазин. Не є офіційним сайтом Apple.
                        </p>
                    </div>

                    <div>
                        <p className="footer-title">Explore</p>

                        <Link to="/catalog" className="footer-link">
                            Каталог <ArrowUpRight size={13} />
                        </Link>

                        <Link to="/favorites" className="footer-link">
                            Обране
                        </Link>

                        <Link to="/orders" className="footer-link">
                            Замовлення
                        </Link>
                    </div>

                    <div>
                        <p className="footer-title">Information</p>
                        <Link to="/about" className="footer-link">
                            Про проєкт
                        </Link>
                        <Link to="/contact" className="footer-link">
                            Контакти
                        </Link>
                    </div>
                </div>

                <div className="mt-10 flex flex-wrap justify-between gap-3 border-t border-white/10 pt-6 text-xs text-muted">
                    <span>© {new Date().getFullYear()} AppleStore</span>
                    <span>Designed for your everyday.</span>
                </div>
            </footer>
        </div>
    );
}