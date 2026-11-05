import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';

import {
    useFavoritesQuery,
    useOrdersQuery,
    useProfileQuery,
    useRemoveFavoriteMutation,
    useUpdateProfileMutation,
} from '../api/api';

import { AvatarUploader } from '../components/Uploaders';
import { ProductCard } from '../components/ProductCard';

import {
    Button,
    Empty,
    ErrorBox,
    Loader,
    PageTitle,
    Pagination,
} from '../components/UI';

import {
    date,
    errorText,
    money,
    orderLabels,
} from '../utils';

export function ProfilePage() {
    const query = useProfileQuery();
    const [update, state] = useUpdateProfileMutation();

    const {
        register,
        handleSubmit,
        reset,
    } = useForm<{
        firstName: string;
        lastName: string;
        phone: string;
    }>();

    useEffect(() => {
        if (!query.data) return;

        reset({
            firstName: query.data.firstName,
            lastName: query.data.lastName,
            phone: query.data.phone ?? '',
        });
    }, [query.data, reset]);

    async function submit(values: {
        firstName: string;
        lastName: string;
        phone: string;
    }) {
        try {
            await update(values).unwrap();
            toast.success('Профіль збережено');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    if (query.isLoading) return <Loader />;

    if (!query.data) {
        return (
            <section className="page">
                <ErrorBox text="Профіль недоступний." />
            </section>
        );
    }

    return (
        <section className="page">
            <PageTitle
                eyebrow="PERSONAL SPACE"
                title="Мій"
                accent="профіль"
            />

            <div className="panel grid gap-10 p-7 md:grid-cols-[240px_1fr]">
                <AvatarUploader profile={query.data} />

                <div>
                    <p className="mb-6 text-sm text-muted">
                        {query.data.email}
                    </p>

                    <form
                        onSubmit={handleSubmit(submit)}
                        className="space-y-4"
                    >
                        <label className="block">
                            <span className="label">Ім’я</span>
                            <input
                                required
                                className="field"
                                {...register('firstName')}
                            />
                        </label>

                        <label className="block">
                            <span className="label">Прізвище</span>
                            <input
                                required
                                className="field"
                                {...register('lastName')}
                            />
                        </label>

                        <label className="block">
                            <span className="label">Телефон</span>
                            <input
                                className="field"
                                {...register('phone')}
                            />
                        </label>

                        <Button loading={state.isLoading}>
                            Зберегти зміни
                        </Button>
                    </form>

                    <div className="mt-7 flex flex-wrap gap-3">
                        <Link to="/orders" className="button-secondary">
                            Замовлення · {query.data.ordersCount}
                        </Link>

                        <Link to="/favorites" className="button-secondary">
                            Обране · {query.data.favoritesCount}
                        </Link>
                    </div>
                </div>
            </div>
        </section>
    );
}

export function FavoritesPage() {
    const [page, setPage] = useState(1);
    const query = useFavoritesQuery(page);

    const [remove, state] = useRemoveFavoriteMutation();

    async function deleteFavorite(id: number) {
        try {
            await remove(id).unwrap();
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    return (
        <section className="page">
            <PageTitle
                eyebrow="WISHLIST"
                title="Збережено"
                accent="для тебе"
            />

            {query.isLoading && <Loader />}

            {query.isError && (
                <ErrorBox
                    text="Не вдалося завантажити обране."
                    retry={() => void query.refetch()}
                />
            )}

            {query.data?.items.length === 0 && (
                <Empty title="В обраному ще немає товарів" />
            )}

            <div className="product-grid">
                {query.data?.items.map((product) => (
                    <div key={product.id}>
                        <ProductCard product={product} />

                        <button
                            className="mt-3 w-full text-xs text-muted hover:text-red-300"
                            disabled={state.isLoading}
                            onClick={() =>
                                void deleteFavorite(product.id)
                            }
                        >
                            Прибрати з обраного
                        </button>
                    </div>
                ))}
            </div>

            <Pagination data={query.data} onChange={setPage} />
        </section>
    );
}

export function OrdersPage() {
    const [page, setPage] = useState(1);
    const query = useOrdersQuery({ page });

    return (
        <section className="page">
            <PageTitle
                eyebrow="ORDER HISTORY"
                title="Мої"
                accent="замовлення"
            />

            {query.isLoading && <Loader />}

            {query.isError && (
                <ErrorBox
                    text="Не вдалося завантажити замовлення."
                    retry={() => void query.refetch()}
                />
            )}

            {query.data?.items.length === 0 && (
                <Empty title="Замовлень поки немає" />
            )}

            <div className="space-y-5">
                {query.data?.items.map((order) => (
                    <article key={order.id} className="panel p-6">
                        <div className="flex flex-wrap justify-between gap-4">
                            <div>
                                <h2 className="font-semibold">
                                    Замовлення #{order.id}
                                </h2>

                                <p className="mt-1 text-xs text-muted">
                                    {date(order.createdAt)}
                                </p>
                            </div>

                            <span className="badge h-fit">
                {orderLabels[order.status]}
              </span>
                        </div>

                        <div className="mt-5 space-y-3">
                            {order.items.map((item) => (
                                <div
                                    key={item.id}
                                    className="flex justify-between gap-3 text-sm"
                                >
                  <span className="text-muted">
                    {item.productName} × {item.quantity}
                  </span>

                                    <span>{money(item.lineTotal)}</span>
                                </div>
                            ))}
                        </div>

                        <div className="mt-5 flex flex-wrap justify-between gap-3 border-t border-white/10 pt-4">
                            <p className="text-xs text-muted">
                                {order.city}, {order.address}
                            </p>

                            <span className="font-semibold text-accent">
                {money(order.totalPrice)}
              </span>
                        </div>
                    </article>
                ))}
            </div>

            <Pagination data={query.data} onChange={setPage} />
        </section>
    );
}