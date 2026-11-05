export interface Paged<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
}

export interface User {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    phone: string | null;
    avatarUrl: string | null;
    roles: string[];
    createdAt: string;
}

export interface Profile extends User {
    ordersCount: number;
    favoritesCount: number;
}

export interface AuthResponse {
    token: string;
    expiresAt: string;
    user: User;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest extends LoginRequest {
    firstName: string;
    lastName: string;
    phone?: string;
}

export interface Category {
    id: number;
    name: string;
    slug: string;
    imageUrl: string | null;
    productsCount: number;
}

export interface ProductImage {
    id: number;
    url: string;
    thumbnailUrl: string;
    isMain: boolean;
    sortOrder: number;
}

export interface ProductVariant {
    id: number;
    color: string;
    storage: string;
    priceDelta: number;
    stock: number;
}

export interface ProductSpec {
    id: number;
    key: string;
    value: string;
}

export interface ProductCard {
    id: number;
    name: string;
    slug: string;
    shortDescription: string;
    price: number;
    oldPrice: number | null;
    stock: number;
    isFeatured: boolean;
    isNew: boolean;
    categoryId: number;
    categoryName: string;
    categorySlug: string;
    imageUrl: string | null;
    thumbnailUrl: string | null;
    colors: string[];
    averageRating: number;
    reviewsCount: number;
    discountPercent: number | null;
}

export interface ProductDetails {
    id: number;
    name: string;
    slug: string;
    shortDescription: string;
    description: string;
    price: number;
    oldPrice: number | null;
    stock: number;
    isFeatured: boolean;
    isNew: boolean;
    categoryId: number;
    categoryName: string;
    categorySlug: string;
    createdAt: string;
    updatedAt: string;
    averageRating: number;
    reviewsCount: number;
    images: ProductImage[];
    variants: ProductVariant[];
    specs: ProductSpec[];
}

export interface ProductQuery {
    search?: string;
    category?: string;
    color?: string;
    minPrice?: number;
    maxPrice?: number;
    sort?: string;
    page?: number;
    pageSize?: number;
    inStockOnly?: boolean;
}

export interface CartItem {
    id: number;
    productId: number;
    productName: string;
    productSlug: string;
    imageUrl: string | null;
    variantId: number | null;
    color: string | null;
    storage: string | null;
    unitPrice: number;
    quantity: number;
    availableStock: number;
    lineTotal: number;
}

export interface Cart {
    id: number;
    items: CartItem[];
    totalPrice: number;
    totalQuantity: number;
}

export type OrderStatus =
    | 'Pending'
    | 'Paid'
    | 'Shipped'
    | 'Delivered'
    | 'Cancelled';

export interface OrderItem {
    id: number;
    productId: number;
    productName: string;
    productImageUrl: string | null;
    price: number;
    quantity: number;
    lineTotal: number;
}

export interface Order {
    id: number;
    status: OrderStatus;
    totalPrice: number;
    address: string;
    city: string;
    phone: string;
    createdAt: string;
    items: OrderItem[];
}

export interface CheckoutRequest {
    address: string;
    city: string;
    phone: string;
}

export interface Review {
    id: number;
    productId: number;
    userId: string;
    userName: string;
    userAvatarUrl: string | null;
    rating: number;
    comment: string;
    createdAt: string;
}

export interface ProductWrite {
    name: string;
    shortDescription: string;
    description: string;
    price: number;
    oldPrice: number | null;
    stock: number;
    categoryId: number;
    isFeatured: boolean;
    isNew: boolean;

    // Backend очікує JSON-рядки, а не масиви.
    specs: string;
    variants: string;
}