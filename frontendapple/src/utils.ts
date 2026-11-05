export const API_URL = (
    import.meta.env.VITE_API_URL ??
    'http://localhost:5000/api'
).replace(/\/$/, '');

const API_ORIGIN = new URL(
    API_URL,
    window.location.origin,
).origin;

export function imageUrl(
    value?: string | null,
): string | undefined {
    if (!value) return undefined;

    return new URL(value, `${API_ORIGIN}/`).href;
}

export function money(value: number): string {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        maximumFractionDigits: 0,
    }).format(value);
}

export function date(value: string): string {
    return new Intl.DateTimeFormat('uk-UA', {
        dateStyle: 'medium',
    }).format(new Date(value));
}

export function initials(
    firstName?: string,
    lastName?: string,
): string {
    return (
        `${firstName?.[0] ?? ''}${lastName?.[0] ?? ''}`
            .toUpperCase() || 'A'
    );
}

export function errorText(error: unknown): string {
    if (!error || typeof error !== 'object') {
        return 'Не вдалося виконати запит.';
    }

    const value = error as {
        data?: {
            detail?: string;
            title?: string;
            errors?: Record<string, string[]>;
        } | string;
        error?: string;
    };

    if (typeof value.data === 'string') {
        return value.data;
    }

    if (value.data?.errors) {
        return Object.values(value.data.errors)
            .flat()
            .join(' ');
    }

    return (
        value.data?.detail ??
        value.data?.title ??
        value.error ??
        'Не вдалося виконати запит.'
    );
}

export const orderLabels: Record<string, string> = {
    Pending: 'Очікує підтвердження',
    Paid: 'Оплачено',
    Shipped: 'Відправлено',
    Delivered: 'Доставлено',
    Cancelled: 'Скасовано',
};