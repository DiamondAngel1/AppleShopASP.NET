import type {
    ButtonHTMLAttributes,
    ReactNode,
} from 'react';

import { PackageOpen, Star } from 'lucide-react';

import type { Paged } from '../types';

export function Button({
                           children,
                           loading,
                           secondary,
                           className = '',
                           disabled,
                           ...props
                       }: ButtonHTMLAttributes<HTMLButtonElement> & {
    loading?: boolean;
    secondary?: boolean;
}) {
    return (
        <button
            {...props}
            disabled={disabled || loading}
            className={`${secondary ? 'button-secondary' : 'button'}
        ${className}`}
        >
            {loading && <span className="spinner small" />}
            {children}
        </button>
    );
}

export function PageTitle({
                              eyebrow,
                              title,
                              accent,
                          }: {
    eyebrow: string;
    title: string;
    accent?: string;
}) {
    return (
        <div className="mb-8">
            <span className="eyebrow">{eyebrow}</span>

            <h1 className="mt-4 text-4xl font-bold md:text-5xl">
                {title}{' '}
                <span className="script">{accent}</span>
            </h1>
        </div>
    );
}

export function Empty({
                          title = 'Поки що порожньо',
                          children,
                      }: {
    title?: string;
    children?: ReactNode;
}) {
    return (
        <div className="panel flex flex-col items-center gap-4 py-16 text-center">
            <PackageOpen
                size={36}
                className="text-accent"
            />

            <h3 className="text-xl font-semibold">
                {title}
            </h3>

            {children}
        </div>
    );
}

export function ErrorBox({
                             text,
                             retry,
                         }: {
    text: string;
    retry?: () => void;
}) {
    return (
        <div
            role="alert"
            className="panel space-y-4 border-red-400/30 p-6"
        >
            <p className="text-red-300">{text}</p>

            {retry && (
                <Button secondary onClick={retry}>
                    Спробувати знову
                </Button>
            )}
        </div>
    );
}

export function Loader() {
    return (
        <div className="flex min-h-60 items-center justify-center">
            <span className="spinner" />
        </div>
    );
}

export function Skeletons() {
    return (
        <div className="product-grid">
            {Array.from({ length: 8 }, (_, i) => (
                <div
                    key={i}
                    className="panel animate-pulse p-4"
                >
                    <div className="aspect-square rounded-2xl bg-white/10" />
                    <div className="mt-5 h-4 w-3/4 rounded bg-white/10" />
                    <div className="mt-3 h-4 w-1/2 rounded bg-white/10" />
                </div>
            ))}
        </div>
    );
}

export function Pagination<T>({
                                  data,
                                  onChange,
                              }: {
    data?: Paged<T>;
    onChange: (page: number) => void;
}) {
    if (!data || data.totalPages <= 1) return null;

    return (
        <div className="mt-8 flex items-center justify-center gap-4">
            <Button
                secondary
                disabled={data.page <= 1}
                onClick={() => onChange(data.page - 1)}
            >
                Назад
            </Button>

            <span className="text-sm text-muted">
        {data.page} / {data.totalPages}
      </span>

            <Button
                secondary
                disabled={data.page >= data.totalPages}
                onClick={() => onChange(data.page + 1)}
            >
                Далі
            </Button>
        </div>
    );
}

export function Stars({
                          value,
                      }: {
    value: number;
}) {
    return (
        <div className="flex gap-1">
            {Array.from({ length: 5 }, (_, i) => (
                <Star
                    key={i}
                    size={14}
                    className={
                        i < Math.round(value)
                            ? 'fill-accent text-accent'
                            : 'text-white/20'
                    }
                />
            ))}
        </div>
    );
}