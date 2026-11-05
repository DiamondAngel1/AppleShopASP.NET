import { useSearchParams } from 'react-router-dom';

import {
    useCategoriesQuery,
    useProductsQuery,
} from '../api/api';

import { ProductCard } from '../components/ProductCard';

import {
    Empty,
    ErrorBox,
    PageTitle,
    Pagination,
    Skeletons,
} from '../components/UI';

export function CatalogPage() {
    const [params, setParams] = useSearchParams();

    const categories = useCategoriesQuery();

    const page = Math.max(
        1,
        Number(params.get('page')) || 1,
    );

    const query = useProductsQuery({
        search: params.get('search') || undefined,
        category: params.get('category') || undefined,
        color: params.get('color') || undefined,
        sort: params.get('sort') || 'newest',

        minPrice: params.get('min')
            ? Number(params.get('min'))
            : undefined,

        maxPrice: params.get('max')
            ? Number(params.get('max'))
            : undefined,

        page,
        pageSize: 12,
    });

    function change(key: string, value: string) {
        const next = new URLSearchParams(params);

        if (value) next.set(key, value);
        else next.delete(key);

        if (key !== 'page') next.delete('page');

        setParams(next, { replace: true });
    }

    return (
        <section className="page">
            <PageTitle
                eyebrow="THE COLLECTION"
                title="Find your"
                accent="Apple."
            />

            <div className="mb-7 flex flex-col gap-3 sm:flex-row">
                <input
                    className="field flex-1"
                    placeholder="Пошук iPhone, Mac, AirPods…"
                    aria-label="Пошук"
                    value={params.get('search') ?? ''}
                    onChange={(event) =>
                        change('search', event.target.value)
                    }
                />

                <select
                    className="field sm:w-56"
                    value={params.get('sort') ?? 'newest'}
                    onChange={(event) =>
                        change('sort', event.target.value)
                    }
                >
                    <option value="newest">Спочатку нові</option>
                    <option value="price-asc">Ціна: за зростанням</option>
                    <option value="price-desc">Ціна: за спаданням</option>
                    <option value="name">За назвою</option>
                </select>
            </div>

            <div className="grid gap-7 lg:grid-cols-[210px_1fr]">
                <aside className="panel h-fit space-y-6 p-5">
                    <div>
                        <h3 className="label">Категорія</h3>

                        <button
                            className={`filter-link ${
                                !params.get('category') ? 'active' : ''
                            }`}
                            onClick={() => change('category', '')}
                        >
                            Усі товари
                        </button>

                        {categories.data?.map((category) => (
                            <button
                                key={category.id}
                                className={`filter-link ${
                                    params.get('category') === category.slug
                                        ? 'active'
                                        : ''
                                }`}
                                onClick={() =>
                                    change('category', category.slug)
                                }
                            >
                                {category.name}

                                <span className="text-xs opacity-60">
                  {category.productsCount}
                </span>
                            </button>
                        ))}
                    </div>

                    <div>
                        <h3 className="label">Ціна, $</h3>

                        <div className="flex gap-2">
                            <input
                                type="number"
                                min="0"
                                className="field min-w-0"
                                placeholder="Від"
                                value={params.get('min') ?? ''}
                                onChange={(event) =>
                                    change('min', event.target.value)
                                }
                            />

                            <input
                                type="number"
                                min="0"
                                className="field min-w-0"
                                placeholder="До"
                                value={params.get('max') ?? ''}
                                onChange={(event) =>
                                    change('max', event.target.value)
                                }
                            />
                        </div>
                    </div>

                    <div>
                        <h3 className="label">Колір</h3>

                        <input
                            className="field"
                            placeholder="Наприклад Silver"
                            value={params.get('color') ?? ''}
                            onChange={(event) =>
                                change('color', event.target.value)
                            }
                        />
                    </div>

                    <button
                        className="text-xs text-accent"
                        onClick={() => setParams({})}
                    >
                        Скинути фільтри
                    </button>
                </aside>

                <div>
                    <p className="mb-5 text-xs text-muted">
                        {query.data?.totalCount ?? 0} товарів
                    </p>

                    {query.isLoading && <Skeletons />}

                    {query.isError && (
                        <ErrorBox
                            text="Не вдалося завантажити каталог."
                            retry={() => void query.refetch()}
                        />
                    )}

                    {query.data?.items.length === 0 && (
                        <Empty title="За цими фільтрами товарів немає" />
                    )}

                    <div
                        className={`grid gap-5 sm:grid-cols-2 xl:grid-cols-3 ${
                            query.isFetching ? 'opacity-60' : ''
                        }`}
                    >
                        {query.data?.items.map((product) => (
                            <ProductCard
                                key={product.id}
                                product={product}
                            />
                        ))}
                    </div>

                    <Pagination
                        data={query.data}
                        onChange={(next) =>
                            change('page', String(next))
                        }
                    />
                </div>
            </div>
        </section>
    );
}