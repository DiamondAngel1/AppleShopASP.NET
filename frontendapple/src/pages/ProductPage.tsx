import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import ReactMarkdown from 'react-markdown';
import toast from 'react-hot-toast';

import { Heart, ShoppingBag } from 'lucide-react';

import {
    useAddCartMutation,
    useAddFavoriteMutation,
    useAddReviewMutation,
    useProductQuery,
    useReviewsQuery,
} from '../api/api';

import { useAppSelector } from '../app/hooks';

import {
    Button,
    Empty,
    ErrorBox,
    Loader,
    Pagination,
    Stars,
} from '../components/UI';

import {
    date,
    errorText,
    imageUrl,
    money,
} from '../utils';

function Reviews({
                     id,
                 }: {
    id: number;
}) {
    const [page, setPage] = useState(1);
    const [rating, setRating] = useState(5);
    const [comment, setComment] = useState('');

    const token = useAppSelector(
        (state) => state.auth.token,
    );

    const query = useReviewsQuery({ id, page });
    const [add, state] = useAddReviewMutation();

    async function submit(event: React.FormEvent) {
        event.preventDefault();

        try {
            await add({ id, rating, comment }).unwrap();
            setComment('');
            toast.success('Відгук опубліковано');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    return (
        <section className="mt-14">
            <h2 className="mb-6 text-2xl font-semibold">
                Відгуки
            </h2>

            <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
                <div className="space-y-3">
                    {query.isLoading && <Loader />}

                    {query.isError && (
                        <ErrorBox
                            text="Не вдалося завантажити відгуки."
                            retry={() => void query.refetch()}
                        />
                    )}

                    {query.data?.items.length === 0 && (
                        <Empty title="Будь першим, хто залишить відгук" />
                    )}

                    {query.data?.items.map((review) => (
                        <article key={review.id} className="panel p-5">
                            <div className="flex items-start justify-between gap-3">
                                <div>
                                    <p className="text-sm font-semibold">
                                        {review.userName}
                                    </p>

                                    <p className="mt-1 text-xs text-muted">
                                        {date(review.createdAt)}
                                    </p>
                                </div>

                                <Stars value={review.rating} />
                            </div>

                            <p className="mt-4 text-sm leading-7 text-muted">
                                {review.comment}
                            </p>
                        </article>
                    ))}

                    <Pagination
                        data={query.data}
                        onChange={setPage}
                    />
                </div>

                <form
                    onSubmit={submit}
                    className="panel h-fit space-y-4 p-5"
                >
                    <h3 className="font-semibold">Твій досвід</h3>

                    {token ? (
                        <>
                            <select
                                className="field"
                                value={rating}
                                onChange={(event) =>
                                    setRating(Number(event.target.value))
                                }
                            >
                                {[5, 4, 3, 2, 1].map((value) => (
                                    <option key={value} value={value}>
                                        {value} / 5
                                    </option>
                                ))}
                            </select>

                            <textarea
                                required
                                maxLength={1500}
                                className="field min-h-32"
                                placeholder="Поділись враженнями"
                                value={comment}
                                onChange={(event) =>
                                    setComment(event.target.value)
                                }
                            />

                            <Button loading={state.isLoading}>
                                Надіслати
                            </Button>
                        </>
                    ) : (
                        <Link to="/login" className="text-sm text-accent">
                            Увійди, щоб залишити відгук
                        </Link>
                    )}
                </form>
            </div>
        </section>
    );
}

export function ProductPage() {
    const { slug = '' } = useParams();

    return <ProductContent key={slug} slug={slug} />;
}

function ProductContent({
                            slug,
                        }: {
    slug: string;
}) {
    const query = useProductQuery(slug);

    const [activeImage, setActiveImage] = useState(0);
    const [variantId, setVariantId] = useState<number>();
    const [quantity, setQuantity] = useState(1);

    const token = useAppSelector(
        (state) => state.auth.token,
    );

    const navigate = useNavigate();

    const [addCart, cartState] = useAddCartMutation();
    const [favorite] = useAddFavoriteMutation();

    if (query.isLoading) return <Loader />;

    if (!query.data) {
        return (
            <section className="page">
                <ErrorBox
                    text="Товар не знайдено."
                    retry={() => void query.refetch()}
                />
            </section>
        );
    }

    const product = query.data;

    const variant = product.variants.find(
        (item) => item.id === variantId,
    ) ?? product.variants[0];

    const stock = Math.min(
        product.stock,
        variant?.stock ?? product.stock,
    );

    const price =
        product.price + (variant?.priceDelta ?? 0);

    async function add() {
        if (!token) {
            navigate('/login');
            return;
        }

        try {
            await addCart({
                productId: product.id,
                variantId: variant?.id,
                quantity,
            }).unwrap();

            toast.success('Товар у кошику');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    async function addFavorite() {
        if (!token) {
            navigate('/login');
            return;
        }

        try {
            await favorite(product.id).unwrap();
            toast.success('Додано в обране');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    return (
        <section className="page">
            <Link to="/catalog" className="text-xs text-muted">
                ← Назад до каталогу
            </Link>

            <div className="mt-7 grid gap-10 lg:grid-cols-2">
                <div>
                    <div className="detail-photo">
                        <img
                            src={imageUrl(
                                product.images[activeImage]?.url ??
                                product.images[0]?.url,
                            )}
                            alt={product.name}
                        />
                    </div>

                    <div className="mt-4 flex gap-3 overflow-auto">
                        {product.images.map((image, index) => (
                            <button
                                key={image.id}
                                onClick={() => setActiveImage(index)}
                                className={`shrink-0 overflow-hidden rounded-xl border ${
                                    activeImage === index
                                        ? 'border-accent'
                                        : 'border-white/10'
                                }`}
                            >
                                <img
                                    src={imageUrl(image.thumbnailUrl)}
                                    alt={`Фото ${index + 1}`}
                                    className="h-20 w-20 object-cover"
                                />
                            </button>
                        ))}
                    </div>
                </div>

                <div className="py-3">
          <span className="eyebrow">
            {product.categoryName}
          </span>

                    <h1 className="mt-5 text-3xl font-bold md:text-4xl">
                        {product.name}
                    </h1>

                    <div className="mt-4 flex items-center gap-3">
                        <Stars value={product.averageRating} />

                        <span className="text-xs text-muted">
              {product.reviewsCount} відгуків
            </span>
                    </div>

                    <p className="mt-5 text-sm leading-7 text-muted">
                        {product.shortDescription}
                    </p>

                    <div className="mt-7 flex items-center gap-4">
            <span className="text-3xl font-bold text-accent">
              {money(price)}
            </span>

                        {product.oldPrice && !variant?.priceDelta && (
                            <span className="text-muted line-through">
                {money(product.oldPrice)}
              </span>
                        )}
                    </div>

                    {!!product.variants.length && (
                        <div className="mt-8">
                            <label className="label" htmlFor="variant">
                                Колір / конфігурація
                            </label>

                            <select
                                id="variant"
                                className="field"
                                value={variant?.id ?? ''}
                                onChange={(event) =>
                                    setVariantId(Number(event.target.value))
                                }
                            >
                                {product.variants.map((item) => (
                                    <option key={item.id} value={item.id}>
                                        {item.color} · {item.storage}
                                        {item.stock === 0 ? ' — немає в наявності' : ''}
                                    </option>
                                ))}
                            </select>
                        </div>
                    )}

                    <p className="mt-4 text-xs text-muted">
                        {stock > 0
                            ? `У наявності: ${stock} шт.`
                            : 'Немає в наявності'}
                    </p>

                    <div className="mt-6 flex flex-wrap gap-3">
                        <input
                            type="number"
                            min={1}
                            max={Math.min(99, stock || 1)}
                            value={quantity}
                            onChange={(event) =>
                                setQuantity(
                                    Math.max(
                                        1,
                                        Math.min(
                                            stock || 1,
                                            Number(event.target.value) || 1,
                                        ),
                                    ),
                                )
                            }
                            className="field w-20"
                            aria-label="Кількість"
                        />

                        <Button
                            onClick={add}
                            disabled={!stock || quantity > stock}
                            loading={cartState.isLoading}
                        >
                            <ShoppingBag size={17} />
                            В кошик
                        </Button>

                        <Button
                            secondary
                            onClick={addFavorite}
                            aria-label="Додати в обране"
                        >
                            <Heart size={18} />
                        </Button>
                    </div>
                </div>
            </div>

            <div className="mt-14 grid gap-7 lg:grid-cols-2">
                <div className="panel p-6">
                    <h2 className="mb-5 text-xl font-semibold">Опис</h2>

                    <div className="markdown">
                        <ReactMarkdown>
                            {product.description}
                        </ReactMarkdown>
                    </div>
                </div>

                <div className="panel p-6">
                    <h2 className="mb-5 text-xl font-semibold">
                        Характеристики
                    </h2>

                    {product.specs.map((spec) => (
                        <div
                            key={spec.id}
                            className="flex justify-between gap-5 border-b border-white/10 py-4 text-sm"
                        >
                            <span className="text-muted">{spec.key}</span>
                            <span className="text-right">{spec.value}</span>
                        </div>
                    ))}
                </div>
            </div>

            <Reviews id={product.id} />
        </section>
    );
}