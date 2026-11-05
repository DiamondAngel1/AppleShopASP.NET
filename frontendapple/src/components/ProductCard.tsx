import { Link, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Heart, ShoppingBag } from 'lucide-react';

import toast from 'react-hot-toast';

import {
    useAddCartMutation,
    useAddFavoriteMutation,
} from '../api/api';

import { useAppSelector } from '../app/hooks';
import type { ProductCard as Product } from '../types';

import {
    errorText,
    imageUrl,
    money,
} from '../utils';

export function ProductCard({
                                product,
                            }: {
    product: Product;
}) {
    const navigate = useNavigate();

    const token = useAppSelector(
        (state) => state.auth.token,
    );

    const [addCart, cartState] = useAddCartMutation();
    const [addFavorite] = useAddFavoriteMutation();

    async function favorite() {
        if (!token) {
            navigate('/login');
            return;
        }

        try {
            await addFavorite(product.id).unwrap();
            toast.success('Додано в обране');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    async function add() {
        if (!token) {
            navigate('/login');
            return;
        }

        try {
            await addCart({
                productId: product.id,
                quantity: 1,
            }).unwrap();

            toast.success('Товар у кошику');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    return (
        <motion.article
            whileHover={{ y: -6 }}
            className="product-card"
        >
            <div className="relative overflow-hidden rounded-2xl">
                <Link to={`/product/${product.slug}`}>
                    <img
                        className="product-photo"
                        src={imageUrl(
                            product.thumbnailUrl ?? product.imageUrl,
                        )}
                        alt={product.name}
                        loading="lazy"
                    />
                </Link>

                <div className="absolute left-3 top-3 flex gap-1">
                    {product.isNew && (
                        <span className="badge">NEW</span>
                    )}

                    {!!product.discountPercent && (
                        <span className="badge">
              −{product.discountPercent}%
            </span>
                    )}
                </div>

                <button
                    onClick={favorite}
                    className="photo-action"
                    aria-label="Додати в обране"
                >
                    <Heart size={16} />
                </button>
            </div>

            <p className="mt-5 text-[10px] uppercase tracking-[.2em] text-muted">
                {product.categoryName}
            </p>

            <Link
                to={`/product/${product.slug}`}
                className="mt-2 block text-base font-semibold hover:text-accent"
            >
                {product.name}
            </Link>

            <p className="mt-2 line-clamp-2 min-h-10 text-xs leading-5 text-muted">
                {product.shortDescription}
            </p>

            <div className="mt-5 flex items-center justify-between">
                <div>
                    <p className="text-lg font-bold text-accent">
                        {money(product.price)}
                    </p>

                    {product.oldPrice && (
                        <p className="text-xs text-muted line-through">
                            {money(product.oldPrice)}
                        </p>
                    )}
                </div>

                <button
                    onClick={add}
                    disabled={cartState.isLoading || !product.stock}
                    className="round-accent disabled:opacity-40"
                    aria-label="Додати в кошик"
                >
                    <ShoppingBag size={17} />
                </button>
            </div>
        </motion.article>
    );
}