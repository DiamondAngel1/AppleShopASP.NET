import { useEffect, useState } from 'react';

import {
    Link,
    useNavigate,
    useParams,
} from 'react-router-dom';

import ReactMarkdown from 'react-markdown';
import toast from 'react-hot-toast';

import {
    useCategoriesQuery,
    useCreateCategoryMutation,
    useCreateProductMutation,
    useDeleteCategoryMutation,
    useDeleteProductMutation,
    useOrdersQuery,
    useOrderStatusMutation,
    useProductQuery,
    useProductsQuery,
    useUpdateCategoryMutation,
    useUpdateProductMutation,
} from '../api/api';

import {
    Button,
    Empty,
    ErrorBox,
    Loader,
    PageTitle,
    Pagination,
} from '../components/UI';

import {
    FilePicker,
    ProductImages,
} from '../components/Uploaders';

import type {
    Category,
    OrderStatus,
    ProductWrite,
} from '../types';

import {
    errorText,
    imageUrl,
    money,
    orderLabels,
} from '../utils';

const statuses: OrderStatus[] = [
    'Pending',
    'Paid',
    'Shipped',
    'Delivered',
    'Cancelled',
];

export function AdminPage() {
    const [tab, setTab] = useState('products');

    return (
        <section className="page">
            <PageTitle
                eyebrow="ADMINISTRATION"
                title="Керування"
                accent="магазином"
            />

            <div className="mb-7 flex flex-wrap gap-3">
                {[
                    ['products', 'Товари'],
                    ['categories', 'Категорії'],
                    ['orders', 'Замовлення'],
                ].map(([value, label]) => (
                    <button
                        key={value}
                        className={
                            tab === value ? 'button' : 'button-secondary'
                        }
                        onClick={() => setTab(value)}
                    >
                        {label}
                    </button>
                ))}
            </div>

            {tab === 'products' && <AdminProducts />}
            {tab === 'categories' && <AdminCategories />}
            {tab === 'orders' && <AdminOrders />}
        </section>
    );
}

function AdminProducts() {
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);

    const query = useProductsQuery({
        search,
        page,
        pageSize: 12,
    });

    const [remove] = useDeleteProductMutation();

    async function deleteProduct(id: number) {
        if (!window.confirm('Видалити товар і його фото?')) {
            return;
        }

        try {
            await remove(id).unwrap();
            toast.success('Товар видалено');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    return (
        <div>
            <div className="mb-5 flex flex-wrap gap-3">
                <input
                    className="field flex-1"
                    placeholder="Пошук товарів"
                    value={search}
                    onChange={(event) => {
                        setSearch(event.target.value);
                        setPage(1);
                    }}
                />

                <Link to="/admin/products/new" className="button">
                    + Додати товар
                </Link>
            </div>

            {query.isLoading && <Loader />}

            {query.isError && (
                <ErrorBox text="Не вдалося завантажити товари." />
            )}

            <div className="panel overflow-x-auto">
                <table className="admin-table">
                    <thead>
                    <tr>
                        <th>Товар</th>
                        <th>Ціна</th>
                        <th>Залишок</th>
                        <th>Дії</th>
                    </tr>
                    </thead>

                    <tbody>
                    {query.data?.items.map((product) => (
                        <tr key={product.id}>
                            <td>
                                <div className="flex items-center gap-3">
                                    <img
                                        src={imageUrl(product.thumbnailUrl)}
                                        alt=""
                                        className="h-12 w-12 rounded-lg object-cover"
                                    />

                                    <span>{product.name}</span>
                                </div>
                            </td>

                            <td>{money(product.price)}</td>
                            <td>{product.stock}</td>

                            <td>
                                <div className="flex gap-4">
                                    <Link
                                        to={`/admin/products/${product.slug}`}
                                        className="text-accent"
                                    >
                                        Редагувати
                                    </Link>

                                    <button
                                        className="text-red-300"
                                        onClick={() =>
                                            void deleteProduct(product.id)
                                        }
                                    >
                                        Видалити
                                    </button>
                                </div>
                            </td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            </div>

            <Pagination data={query.data} onChange={setPage} />
        </div>
    );
}

interface SpecRow {
    key: string;
    value: string;
}

interface VariantRow {
    color: string;
    storage: string;
    priceDelta: number;
    stock: number;
}

interface EditorState {
    name: string;
    shortDescription: string;
    description: string;
    price: number;
    oldPrice: string;
    stock: number;
    categoryId: number;
    isFeatured: boolean;
    isNew: boolean;
}

const initialEditor: EditorState = {
    name: '',
    shortDescription: '',
    description: '',
    price: 0,
    oldPrice: '',
    stock: 0,
    categoryId: 0,
    isFeatured: false,
    isNew: false,
};

export function ProductEditorPage() {
    const { slug } = useParams();

    return (
        <ProductEditor
            key={slug ?? 'new'}
            slug={slug}
        />
    );
}

function ProductEditor({
                           slug,
                       }: {
    slug?: string;
}) {
    const query = useProductQuery(slug ?? '', {
        skip: !slug,
    });

    const categories = useCategoriesQuery();

    const [form, setForm] = useState(initialEditor);

    const [specs, setSpecs] = useState<SpecRow[]>([]);
    const [variants, setVariants] = useState<VariantRow[]>([]);
    const [files, setFiles] = useState<File[]>([]);
    const [preview, setPreview] = useState(false);
    const [initialized, setInitialized] = useState(false);

    const [create, createState] = useCreateProductMutation();
    const [update, updateState] = useUpdateProductMutation();

    const navigate = useNavigate();

    useEffect(() => {
        if (!query.data || initialized) return;

        const product = query.data;

        setForm({
            name: product.name,
            shortDescription: product.shortDescription,
            description: product.description,
            price: product.price,
            oldPrice: product.oldPrice?.toString() ?? '',
            stock: product.stock,
            categoryId: product.categoryId,
            isFeatured: product.isFeatured,
            isNew: product.isNew,
        });

        setSpecs(
            product.specs.map(({ key, value }) => ({
                key,
                value,
            })),
        );

        setVariants(
            product.variants.map(
                ({ color, storage, priceDelta, stock }) => ({
                    color,
                    storage,
                    priceDelta,
                    stock,
                }),
            ),
        );

        setInitialized(true);
    }, [query.data, initialized]);

    function field<K extends keyof EditorState>(
        key: K,
        value: EditorState[K],
    ) {
        setForm((current) => ({
            ...current,
            [key]: value,
        }));
    }

    async function submit(event: React.FormEvent) {
        event.preventDefault();

        if (
            !form.categoryId ||
            form.price <= 0 ||
            form.stock < 0
        ) {
            toast.error('Перевір категорію, ціну й залишок.');
            return;
        }

        if (
            form.oldPrice &&
            Number(form.oldPrice) <= form.price
        ) {
            toast.error('Стара ціна має бути більшою за поточну.');
            return;
        }

        const body: ProductWrite = {
            ...form,
            oldPrice: form.oldPrice
                ? Number(form.oldPrice)
                : null,
            specs: JSON.stringify(
                specs.filter((spec) => spec.key.trim()),
            ),
            variants: JSON.stringify(
                variants.filter((variant) => variant.color.trim()),
            ),
        };

        try {
            if (query.data) {
                const saved = await update({
                    id: query.data.id,
                    body,
                }).unwrap();

                toast.success('Товар оновлено');

                if (saved.slug !== slug) {
                    navigate(`/admin/products/${saved.slug}`);
                }
            } else {
                const data = new FormData();

                Object.entries(body).forEach(([key, value]) => {
                    if (value !== null) {
                        data.append(key, String(value));
                    }
                });

                files.forEach((file) =>
                    data.append('Images', file),
                );

                const created = await create(data).unwrap();
                toast.success('Товар створено');

                navigate(`/admin/products/${created.slug}`);
            }
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    if (slug && query.isLoading) return <Loader />;

    if (slug && query.isError) {
        return (
            <section className="page">
                <ErrorBox text="Товар не вдалося завантажити." />
            </section>
        );
    }

    return (
        <section className="page">
            <Link to="/admin" className="text-sm text-muted">
                ← Адмін-панель
            </Link>

            <h1 className="my-6 text-3xl font-semibold">
                {slug ? 'Редагування товару' : 'Новий товар'}
            </h1>

            <form
                onSubmit={submit}
                className="grid gap-6 lg:grid-cols-[1fr_340px]"
            >
                <div className="space-y-6">
                    <div className="panel space-y-4 p-6">
                        <label className="block">
                            <span className="label">Назва</span>

                            <input
                                required
                                className="field"
                                value={form.name}
                                onChange={(event) =>
                                    field('name', event.target.value)
                                }
                            />
                        </label>

                        <label className="block">
                            <span className="label">Короткий опис</span>

                            <textarea
                                required
                                maxLength={400}
                                className="field"
                                value={form.shortDescription}
                                onChange={(event) =>
                                    field(
                                        'shortDescription',
                                        event.target.value,
                                    )
                                }
                            />
                        </label>

                        <div className="grid gap-4 sm:grid-cols-2">
                            <label>
                                <span className="label">Ціна</span>
                                <input
                                    required
                                    type="number"
                                    min="0.01"
                                    step="0.01"
                                    className="field"
                                    value={form.price}
                                    onChange={(event) =>
                                        field('price', Number(event.target.value))
                                    }
                                />
                            </label>

                            <label>
                                <span className="label">Стара ціна</span>
                                <input
                                    type="number"
                                    step="0.01"
                                    className="field"
                                    value={form.oldPrice}
                                    onChange={(event) =>
                                        field('oldPrice', event.target.value)
                                    }
                                />
                            </label>

                            <label>
                                <span className="label">Залишок</span>
                                <input
                                    type="number"
                                    min="0"
                                    className="field"
                                    value={form.stock}
                                    onChange={(event) =>
                                        field('stock', Number(event.target.value))
                                    }
                                />
                            </label>

                            <label>
                                <span className="label">Категорія</span>

                                <select
                                    required
                                    className="field"
                                    value={form.categoryId || ''}
                                    onChange={(event) =>
                                        field(
                                            'categoryId',
                                            Number(event.target.value),
                                        )
                                    }
                                >
                                    <option value="">Оберіть категорію</option>

                                    {categories.data?.map((category) => (
                                        <option
                                            key={category.id}
                                            value={category.id}
                                        >
                                            {category.name}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </div>

                        <div className="flex gap-6 text-sm">
                            <label className="flex gap-2">
                                <input
                                    type="checkbox"
                                    checked={form.isFeatured}
                                    onChange={(event) =>
                                        field('isFeatured', event.target.checked)
                                    }
                                />
                                Рекомендований
                            </label>

                            <label className="flex gap-2">
                                <input
                                    type="checkbox"
                                    checked={form.isNew}
                                    onChange={(event) =>
                                        field('isNew', event.target.checked)
                                    }
                                />
                                Новинка
                            </label>
                        </div>
                    </div>

                    <div className="panel p-6">
                        <div className="mb-4 flex justify-between">
                            <h2 className="font-semibold">
                                Опис Markdown
                            </h2>

                            <button
                                type="button"
                                className="text-sm text-accent"
                                onClick={() => setPreview(!preview)}
                            >
                                {preview ? 'Редагувати' : 'Preview'}
                            </button>
                        </div>

                        {preview ? (
                            <div className="markdown min-h-48">
                                <ReactMarkdown>
                                    {form.description}
                                </ReactMarkdown>
                            </div>
                        ) : (
                            <textarea
                                required
                                className="field min-h-60"
                                value={form.description}
                                onChange={(event) =>
                                    field('description', event.target.value)
                                }
                            />
                        )}
                    </div>

                    <div className="panel p-6">
                        <div className="mb-4 flex justify-between">
                            <h2 className="font-semibold">
                                Характеристики
                            </h2>

                            <button
                                type="button"
                                className="text-accent"
                                onClick={() =>
                                    setSpecs([
                                        ...specs,
                                        { key: '', value: '' },
                                    ])
                                }
                            >
                                + Додати
                            </button>
                        </div>

                        <div className="space-y-3">
                            {specs.map((spec, index) => (
                                <div key={index} className="flex gap-2">
                                    <input
                                        className="field min-w-0"
                                        placeholder="Назва"
                                        value={spec.key}
                                        onChange={(event) =>
                                            setSpecs(
                                                specs.map((row, i) =>
                                                    i === index
                                                        ? { ...row, key: event.target.value }
                                                        : row,
                                                ),
                                            )
                                        }
                                    />

                                    <input
                                        className="field min-w-0"
                                        placeholder="Значення"
                                        value={spec.value}
                                        onChange={(event) =>
                                            setSpecs(
                                                specs.map((row, i) =>
                                                    i === index
                                                        ? { ...row, value: event.target.value }
                                                        : row,
                                                ),
                                            )
                                        }
                                    />

                                    <button
                                        type="button"
                                        className="text-red-300"
                                        onClick={() =>
                                            setSpecs(
                                                specs.filter((_, i) => i !== index),
                                            )
                                        }
                                    >
                                        ×
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className="panel p-6">
                        <div className="mb-4 flex justify-between">
                            <h2 className="font-semibold">Варіанти</h2>

                            <button
                                type="button"
                                className="text-accent"
                                onClick={() =>
                                    setVariants([
                                        ...variants,
                                        {
                                            color: '',
                                            storage: '',
                                            priceDelta: 0,
                                            stock: 0,
                                        },
                                    ])
                                }
                            >
                                + Додати
                            </button>
                        </div>

                        {variants.map((variant, index) => (
                            <div
                                key={index}
                                className="mb-4 grid gap-2 rounded-xl border border-white/10 p-3 sm:grid-cols-2"
                            >
                                {([
                                    ['color', 'Колір'],
                                    ['storage', 'Пам’ять'],
                                ] as const).map(([name, label]) => (
                                    <input
                                        key={name}
                                        className="field"
                                        placeholder={label}
                                        value={variant[name]}
                                        onChange={(event) =>
                                            setVariants(
                                                variants.map((row, i) =>
                                                    i === index
                                                        ? {
                                                            ...row,
                                                            [name]: event.target.value,
                                                        }
                                                        : row,
                                                ),
                                            )
                                        }
                                    />
                                ))}

                                {([
                                    ['priceDelta', 'Різниця в ціні'],
                                    ['stock', 'Залишок'],
                                ] as const).map(([name, label]) => (
                                    <label key={name}>
                                        <span className="label">{label}</span>

                                        <input
                                            type="number"
                                            min={name === 'stock' ? 0 : undefined}
                                            step={name === 'stock' ? 1 : 0.01}
                                            className="field"
                                            value={variant[name]}
                                            onChange={(event) =>
                                                setVariants(
                                                    variants.map((row, i) =>
                                                        i === index
                                                            ? {
                                                                ...row,
                                                                [name]: Number(
                                                                    event.target.value,
                                                                ),
                                                            }
                                                            : row,
                                                    ),
                                                )
                                            }
                                        />
                                    </label>
                                ))}

                                <button
                                    type="button"
                                    className="text-left text-xs text-red-300"
                                    onClick={() =>
                                        setVariants(
                                            variants.filter((_, i) => i !== index),
                                        )
                                    }
                                >
                                    Видалити варіант
                                </button>
                            </div>
                        ))}
                    </div>

                    <Button
                        loading={
                            createState.isLoading ||
                            updateState.isLoading
                        }
                    >
                        Зберегти товар
                    </Button>
                </div>

                <aside className="panel h-fit p-6">
                    <h2 className="mb-5 font-semibold">
                        Фотографії
                    </h2>

                    {query.data ? (
                        <ProductImages
                            id={query.data.id}
                            images={query.data.images}
                        />
                    ) : (
                        <FilePicker
                            files={files}
                            onChange={setFiles}
                        />
                    )}
                </aside>
            </form>
        </section>
    );
}

function AdminCategories() {
    const query = useCategoriesQuery();

    const [editing, setEditing] = useState<Category | null>(null);
    const [name, setName] = useState('');
    const [slug, setSlug] = useState('');
    const [file, setFile] = useState<File | null>(null);

    const [create, createState] = useCreateCategoryMutation();
    const [update, updateState] = useUpdateCategoryMutation();
    const [remove] = useDeleteCategoryMutation();

    function edit(category: Category) {
        setEditing(category);
        setName(category.name);
        setSlug(category.slug);
        setFile(null);
    }

    function reset() {
        setEditing(null);
        setName('');
        setSlug('');
        setFile(null);
    }

    async function submit(event: React.FormEvent) {
        event.preventDefault();

        if (file && file.size > 5 * 1024 * 1024) {
            toast.error('Фото має бути до 5 МБ.');
            return;
        }

        const body = new FormData();
        body.append('Name', name);

        if (slug.trim()) body.append('Slug', slug);
        if (file) body.append('Image', file);

        try {
            if (editing) {
                await update({
                    id: editing.id,
                    body,
                }).unwrap();
            } else {
                await create(body).unwrap();
            }

            reset();
            toast.success('Категорію збережено');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    async function deleteCategory(id: number) {
        if (!window.confirm('Видалити категорію?')) return;

        try {
            await remove(id).unwrap();
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    return (
        <div className="grid gap-6 lg:grid-cols-[1fr_330px]">
            <div className="panel p-5">
                {query.isLoading && <Loader />}

                {query.data?.map((category) => (
                    <div
                        key={category.id}
                        className="flex items-center gap-3 border-b border-white/10 py-4"
                    >
                        <img
                            src={imageUrl(category.imageUrl)}
                            alt=""
                            className="h-12 w-12 rounded-xl object-cover"
                        />

                        <div className="flex-1">
                            <p>{category.name}</p>
                            <p className="text-xs text-muted">
                                {category.productsCount} товарів
                            </p>
                        </div>

                        <button
                            className="text-xs text-accent"
                            onClick={() => edit(category)}
                        >
                            Змінити
                        </button>

                        <button
                            className="text-xs text-red-300"
                            onClick={() =>
                                void deleteCategory(category.id)
                            }
                        >
                            Видалити
                        </button>
                    </div>
                ))}
            </div>

            <form
                onSubmit={submit}
                className="panel h-fit space-y-4 p-5"
            >
                <h2 className="font-semibold">
                    {editing ? 'Редагувати категорію' : 'Нова категорія'}
                </h2>

                <input
                    required
                    className="field"
                    placeholder="Назва"
                    value={name}
                    onChange={(event) => setName(event.target.value)}
                />

                <input
                    className="field"
                    placeholder="Slug"
                    value={slug}
                    onChange={(event) => setSlug(event.target.value)}
                />

                <input
                    type="file"
                    accept="image/jpeg,image/png,image/webp"
                    className="field text-xs"
                    onChange={(event) =>
                        setFile(event.target.files?.[0] ?? null)
                    }
                />

                <Button
                    loading={
                        createState.isLoading ||
                        updateState.isLoading
                    }
                >
                    Зберегти
                </Button>

                {editing && (
                    <Button
                        type="button"
                        secondary
                        onClick={reset}
                    >
                        Скасувати
                    </Button>
                )}
            </form>
        </div>
    );
}

function AdminOrders() {
    const [page, setPage] = useState(1);

    const query = useOrdersQuery({
        page,
        admin: true,
    });

    const [update, state] = useOrderStatusMutation();

    async function change(id: number, status: OrderStatus) {
        try {
            await update({ id, status }).unwrap();
            toast.success('Статус оновлено');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    return (
        <div>
            {query.isLoading && <Loader />}

            {query.isError && (
                <ErrorBox text="Замовлення недоступні." />
            )}

            {query.data?.items.length === 0 && <Empty />}

            <div className="space-y-4">
                {query.data?.items.map((order) => (
                    <article key={order.id} className="panel p-5">
                        <div className="flex flex-wrap items-center justify-between gap-4">
                            <div>
                                <h3 className="font-semibold">
                                    #{order.id} · {money(order.totalPrice)}
                                </h3>

                                <p className="mt-2 text-xs text-muted">
                                    {order.city}, {order.address} · {order.phone}
                                </p>
                            </div>

                            <select
                                className="field w-56"
                                value={order.status}
                                disabled={state.isLoading}
                                onChange={(event) =>
                                    void change(
                                        order.id,
                                        event.target.value as OrderStatus,
                                    )
                                }
                            >
                                {statuses.map((status) => (
                                    <option key={status} value={status}>
                                        {orderLabels[status]}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div className="mt-4 text-sm text-muted">
                            {order.items.map((item) => (
                                <p key={item.id} className="mt-1">
                                    {item.productName} × {item.quantity}
                                </p>
                            ))}
                        </div>
                    </article>
                ))}
            </div>

            <Pagination data={query.data} onChange={setPage} />
        </div>
    );
}