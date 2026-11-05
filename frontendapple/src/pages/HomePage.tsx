import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';

import {
    ArrowRight,
    BadgeCheck,
    LockKeyhole,
    ShieldCheck,
    Truck,
} from 'lucide-react';

import {
    useArrivalsQuery,
    useCategoriesQuery,
    useFeaturedQuery,
} from '../api/api';

import { ProductCard } from '../components/ProductCard';

import {
    ErrorBox,
    Skeletons,
} from '../components/UI';

import { imageUrl } from '../utils';

const features = [
    { icon: BadgeCheck, title: 'Premium', text: 'Quality' },
    { icon: ShieldCheck, title: '1 Year', text: 'Warranty' },
    { icon: Truck, title: 'Free Shipping', text: 'Orders $50+' },
    { icon: LockKeyhole, title: 'Secure', text: 'Shopping' },
];

export function HomePage() {
    const featured = useFeaturedQuery();
    const categories = useCategoriesQuery();
    const arrivals = useArrivalsQuery();

    const hero =
        featured.data?.find(
            (product) => product.categorySlug === 'airpods',
        ) ?? featured.data?.[0];

    const arrival = arrivals.data?.[0];

    return (
        <>
            <section className="hero">
                <div className="hero-copy">
                    <span className="eyebrow">NEXT-GEN APPLE</span>

                    <h1>
                        Power Up
                        <br />
                        Your <span className="script">World.</span>
                    </h1>

                    <p className="mt-5 max-w-sm text-sm leading-7 text-muted">
                        Більше ідей. Більше можливостей.
                        <br />
                        Відкрий техніку, що працює у твоєму ритмі.
                    </p>

                    <div className="mt-7 flex flex-wrap items-center gap-5">
                        <Link to="/catalog" className="button">
                            SHOP NOW <ArrowRight size={17} />
                        </Link>

                        <Link
                            to="/catalog?category=airpods"
                            className="flex items-center gap-3 text-xs"
                        >
                            <span className="play-button">↗</span>
                            <span>
                Watch Collection
                <span className="mt-1 block text-[10px] text-muted">
                  Explore Apple audio
                </span>
              </span>
                        </Link>
                    </div>

                    <div className="hero-features">
                        {features.map(({ icon: Icon, title, text }) => (
                            <div key={title} className="flex items-center gap-2">
                <span className="feature-icon">
                  <Icon size={16} />
                </span>

                                <span className="text-[10px] leading-4">
                  {title}
                                    <span className="block text-muted">{text}</span>
                </span>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="hero-stage">
                    <div className="orbit orbit-one" />
                    <div className="orbit orbit-two" />
                    <div className="stage-glow" />
                    <div className="podium" />

                    {hero ? (
                        <motion.img
                            src={imageUrl(hero.imageUrl)}
                            alt={hero.name}
                            className="hero-image"
                            animate={{ y: [-8, 8, -8], rotate: [-3, 0, -3] }}
                            transition={{
                                duration: 7,
                                repeat: Infinity,
                                ease: 'easeInOut',
                            }}
                        />
                    ) : (
                        <div className="hero-placeholder">
                            <span className="script">Apple.</span>
                        </div>
                    )}

                    {hero && (
                        <span className="hero-product-label">
              {hero.name}
            </span>
                    )}
                </div>
            </section>

            <section id="categories" className="section-space">
                <div className="section-heading">
                    <h2>Shop By Category</h2>

                    <Link to="/catalog">
                        View All Categories
                        <span className="mini-arrow">↗</span>
                    </Link>
                </div>

                {categories.isError && (
                    <ErrorBox
                        text="Не вдалося завантажити категорії."
                        retry={() => void categories.refetch()}
                    />
                )}

                <div className="category-grid">
                    {categories.isLoading &&
                        Array.from({ length: 6 }, (_, i) => (
                            <div
                                key={i}
                                className="h-56 animate-pulse rounded-2xl bg-white/10"
                            />
                        ))}

                    {categories.data?.map((category) => (
                        <Link
                            key={category.id}
                            to={`/catalog?category=${category.slug}`}
                            className="category-card"
                        >
                            <img
                                src={imageUrl(category.imageUrl)}
                                alt={category.name}
                            />

                            <div className="relative z-10 mt-auto">
                                <h3 className="font-semibold">
                                    {category.name}
                                </h3>

                                <span className="mt-2 block text-[10px] text-white/75">
                  Shop Now →
                </span>
                            </div>
                        </Link>
                    ))}
                </div>
            </section>

            {arrival && (
                <section className="arrival-banner section-space">
                    <div className="arrival-art">
                        <div className="orbit" />

                        <img
                            src={imageUrl(arrival.imageUrl)}
                            alt={arrival.name}
                        />

                        <div className="podium" />
                    </div>

                    <div className="relative z-10 py-8">
                        <span className="eyebrow">NEW ARRIVAL</span>

                        <h2 className="mt-5 text-3xl font-semibold md:text-4xl">
                            Small Size.
                            <br />
                            Big <span className="script">Performance.</span>
                        </h2>

                        <p className="mt-4 max-w-sm text-sm text-muted">
                            {arrival.shortDescription}
                        </p>

                        <Link
                            to={`/product/${arrival.slug}`}
                            className="button mt-6"
                        >
                            SHOP NOW <ArrowRight size={16} />
                        </Link>
                    </div>
                </section>
            )}

            <section className="section-space">
                <div className="section-heading">
                    <h2>Selected for you</h2>

                    <Link to="/catalog">
                        Explore Collection
                        <span className="mini-arrow">↗</span>
                    </Link>
                </div>

                {featured.isLoading && <Skeletons />}

                {featured.isError && (
                    <ErrorBox
                        text="API недоступне. Перевір, чи запущений backend."
                        retry={() => void featured.refetch()}
                    />
                )}

                <div className="product-grid">
                    {featured.data?.map((product) => (
                        <ProductCard
                            key={product.id}
                            product={product}
                        />
                    ))}
                </div>
            </section>
        </>
    );
}