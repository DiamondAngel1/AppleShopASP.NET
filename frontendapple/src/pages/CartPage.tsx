import { Link, useNavigate } from 'react-router-dom';
import { Trash2 } from 'lucide-react';
import toast from 'react-hot-toast';

import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

import {
    useCartQuery,
    useCheckoutMutation,
    useDeleteCartMutation,
    useUpdateCartMutation,
} from '../api/api';

import {
    Button,
    Empty,
    ErrorBox,
    Loader,
    PageTitle,
} from '../components/UI';

import {
    errorText,
    imageUrl,
    money,
} from '../utils';

const checkoutSchema = z.object({
    city: z.string().min(2, 'Вкажи місто'),
    address: z.string().min(5, 'Вкажи повну адресу'),
    phone: z.string().min(10, 'Вкажи телефон'),
});

type CheckoutValues = z.infer<typeof checkoutSchema>;

export function CartPage({
                             checkout = false,
                         }: {
    checkout?: boolean;
}) {
    const query = useCartQuery();
    const navigate = useNavigate();

    const [update, updateState] = useUpdateCartMutation();
    const [remove, removeState] = useDeleteCartMutation();
    const [placeOrder, orderState] = useCheckoutMutation();

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<CheckoutValues>({
        resolver: zodResolver(checkoutSchema),
    });

    async function quantity(id: number, value: number) {
        try {
            await update({ id, quantity: value }).unwrap();
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    async function deleteItem(id: number) {
        try {
            await remove(id).unwrap();
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    async function submit(values: CheckoutValues) {
        try {
            await placeOrder(values).unwrap();
            toast.success('Замовлення створено');
            navigate('/orders');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    if (query.isLoading) return <Loader />;

    if (query.isError) {
        return (
            <section className="page">
                <ErrorBox
                    text="Не вдалося завантажити кошик."
                    retry={() => void query.refetch()}
                />
            </section>
        );
    }

    const cart = query.data;

    if (!cart?.items.length) {
        return (
            <section className="page">
                <Empty title="Твій кошик порожній">
                    <Link className="button" to="/catalog">
                        Обрати товари
                    </Link>
                </Empty>
            </section>
        );
    }

    return (
        <section className="page">
            <PageTitle
                eyebrow={checkout ? 'CHECKOUT' : 'YOUR COLLECTION'}
                title={checkout ? 'Оформлення' : 'Твій'}
                accent={checkout ? 'замовлення' : 'кошик'}
            />

            <div className="grid gap-7 lg:grid-cols-[1fr_320px]">
                <div>
                    {checkout ? (
                        <form
                            id="checkout"
                            onSubmit={handleSubmit(submit)}
                            className="panel space-y-5 p-6"
                        >
                            {([
                                ['city', 'Місто'],
                                ['address', 'Адреса доставки'],
                                ['phone', 'Телефон'],
                            ] as const).map(([name, label]) => (
                                <label key={name} className="block">
                                    <span className="label">{label}</span>

                                    <input
                                        className="field"
                                        {...register(name)}
                                    />

                                    <span className="mt-2 block text-xs text-red-300">
                    {errors[name]?.message}
                  </span>
                                </label>
                            ))}

                            <p className="text-sm leading-6 text-muted">
                                Демонстраційне оформлення без онлайн-оплати.
                                Створення замовлення не означає списання коштів.
                            </p>
                        </form>
                    ) : (
                        <div className="space-y-4">
                            {cart.items.map((item) => (
                                <article
                                    key={item.id}
                                    className="panel flex gap-4 p-4"
                                >
                                    <Link
                                        to={`/product/${item.productSlug}`}
                                        className="shrink-0"
                                    >
                                        <img
                                            src={imageUrl(item.imageUrl)}
                                            alt={item.productName}
                                            className="h-24 w-24 rounded-xl object-cover"
                                        />
                                    </Link>

                                    <div className="min-w-0 flex-1">
                                        <Link
                                            to={`/product/${item.productSlug}`}
                                            className="font-medium hover:text-accent"
                                        >
                                            {item.productName}
                                        </Link>

                                        <p className="mt-1 text-xs text-muted">
                                            {[item.color, item.storage]
                                                .filter(Boolean)
                                                .join(' · ')}
                                        </p>

                                        <div className="mt-4 flex items-center gap-3">
                                            <button
                                                className="quantity-button"
                                                disabled={updateState.isLoading}
                                                onClick={() =>
                                                    void quantity(
                                                        item.id,
                                                        Math.max(0, item.quantity - 1),
                                                    )
                                                }
                                            >
                                                −
                                            </button>

                                            <span className="text-sm">
                        {item.quantity}
                      </span>

                                            <button
                                                className="quantity-button"
                                                disabled={
                                                    updateState.isLoading ||
                                                    item.quantity >= item.availableStock
                                                }
                                                onClick={() =>
                                                    void quantity(
                                                        item.id,
                                                        item.quantity + 1,
                                                    )
                                                }
                                            >
                                                +
                                            </button>
                                        </div>
                                    </div>

                                    <div className="flex flex-col items-end justify-between">
                                        <button
                                            aria-label="Видалити позицію"
                                            disabled={removeState.isLoading}
                                            onClick={() => void deleteItem(item.id)}
                                        >
                                            <Trash2 size={17} className="text-red-300" />
                                        </button>

                                        <span className="font-semibold text-accent">
                      {money(item.lineTotal)}
                    </span>
                                    </div>
                                </article>
                            ))}
                        </div>
                    )}
                </div>

                <aside className="panel h-fit p-6">
                    <h2 className="mb-5 text-xl font-semibold">
                        Підсумок
                    </h2>

                    <div className="space-y-3 text-sm text-muted">
                        <p className="flex justify-between">
                            <span>Кількість</span>
                            <span>{cart.totalQuantity}</span>
                        </p>

                        <p className="flex justify-between">
                            <span>Товари</span>
                            <span>{money(cart.totalPrice)}</span>
                        </p>
                    </div>

                    <div className="mt-5 flex justify-between border-t border-white/10 pt-5">
                        <span>Разом</span>

                        <span className="text-xl font-bold text-accent">
              {money(cart.totalPrice)}
            </span>
                    </div>

                    {checkout ? (
                        <Button
                            type="submit"
                            form="checkout"
                            className="mt-6 w-full"
                            loading={orderState.isLoading}
                        >
                            Підтвердити
                        </Button>
                    ) : (
                        <Link
                            to="/checkout"
                            className="button mt-6 w-full"
                        >
                            Оформити замовлення
                        </Link>
                    )}
                </aside>
            </div>
        </section>
    );
}