import {
    createApi,
    fetchBaseQuery,
    type BaseQueryFn,
    type FetchArgs,
    type FetchBaseQueryError,
} from '@reduxjs/toolkit/query/react';

import { API_URL } from '../utils';
import { logout } from '../features/authSlice';

import type {
    AuthResponse,
    Cart,
    Category,
    CheckoutRequest,
    LoginRequest,
    Order,
    OrderStatus,
    Paged,
    ProductCard,
    ProductDetails,
    ProductImage,
    ProductQuery,
    ProductWrite,
    Profile,
    RegisterRequest,
    Review,
} from '../types';

const rawQuery = fetchBaseQuery({
    baseUrl: API_URL,

    prepareHeaders: (headers, { getState }) => {
        const state = getState() as {
            auth: { token: string | null };
        };

        if (state.auth.token) {
            headers.set(
                'Authorization',
                `Bearer ${state.auth.token}`,
            );
        }

        return headers;
    },
});

const baseQuery: BaseQueryFn<
    string | FetchArgs,
    unknown,
    FetchBaseQueryError
> = async (args, queryApi, extraOptions) => {
    const result = await rawQuery(
        args,
        queryApi,
        extraOptions,
    );

    if (result.error?.status === 401) {
        queryApi.dispatch(logout());
    }

    return result;
};

export const api = createApi({
    reducerPath: 'appleApi',
    baseQuery,

    tagTypes: [
        'Products',
        'Categories',
        'Cart',
        'Profile',
        'Orders',
        'Favorites',
        'Reviews',
    ],

    endpoints: () => ({}),
});

const endpoints = api.injectEndpoints({
    endpoints: (builder) => ({
        login: builder.mutation<
            AuthResponse,
            LoginRequest
        >({
            query: (body) => ({
                url: '/auth/login',
                method: 'POST',
                body,
            }),
        }),

        register: builder.mutation<
            AuthResponse,
            RegisterRequest
        >({
            query: (body) => ({
                url: '/auth/register',
                method: 'POST',
                body,
            }),
        }),

        profile: builder.query<Profile, void>({
            query: () => '/profile',
            providesTags: ['Profile'],
        }),

        updateProfile: builder.mutation<
            Profile,
            {
                firstName: string;
                lastName: string;
                phone?: string;
            }
        >({
            query: (body) => ({
                url: '/profile',
                method: 'PUT',
                body,
            }),
            invalidatesTags: ['Profile'],
        }),

        uploadAvatar: builder.mutation<
            { avatarUrl: string },
            FormData
        >({
            query: (body) => ({
                url: '/profile/avatar',
                method: 'POST',
                body,
            }),
            invalidatesTags: ['Profile'],
        }),

        deleteAvatar: builder.mutation<void, void>({
            query: () => ({
                url: '/profile/avatar',
                method: 'DELETE',
            }),
            invalidatesTags: ['Profile'],
        }),

        categories: builder.query<Category[], void>({
            query: () => '/categories',
            providesTags: ['Categories'],
        }),

        createCategory: builder.mutation<
            Category,
            FormData
        >({
            query: (body) => ({
                url: '/categories',
                method: 'POST',
                body,
            }),
            invalidatesTags: ['Categories'],
        }),

        updateCategory: builder.mutation<
            Category,
            { id: number; body: FormData }
        >({
            query: ({ id, body }) => ({
                url: `/categories/${id}`,
                method: 'PUT',
                body,
            }),
            invalidatesTags: ['Categories'],
        }),

        deleteCategory: builder.mutation<void, number>({
            query: (id) => ({
                url: `/categories/${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['Categories'],
        }),

        products: builder.query<
            Paged<ProductCard>,
            ProductQuery
        >({
            query: (params) => ({
                url: '/products',
                params,
            }),
            providesTags: ['Products'],
        }),

        featured: builder.query<ProductCard[], void>({
            query: () => '/products/featured?take=8',
            providesTags: ['Products'],
        }),

        arrivals: builder.query<ProductCard[], void>({
            query: () => '/products/new-arrivals?take=4',
            providesTags: ['Products'],
        }),

        product: builder.query<ProductDetails, string>({
            query: (slug) =>
                `/products/${encodeURIComponent(slug)}`,
            providesTags: ['Products'],
        }),

        createProduct: builder.mutation<
            ProductDetails,
            FormData
        >({
            query: (body) => ({
                url: '/products',
                method: 'POST',
                body,
            }),
            invalidatesTags: ['Products', 'Categories'],
        }),

        updateProduct: builder.mutation<
            ProductDetails,
            { id: number; body: ProductWrite }
        >({
            query: ({ id, body }) => ({
                url: `/products/${id}`,
                method: 'PUT',
                body,
            }),
            invalidatesTags: ['Products', 'Categories'],
        }),

        deleteProduct: builder.mutation<void, number>({
            query: (id) => ({
                url: `/products/${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: [
                'Products',
                'Categories',
                'Cart',
                'Favorites',
            ],
        }),

        uploadImages: builder.mutation<
            ProductImage[],
            { id: number; body: FormData }
        >({
            query: ({ id, body }) => ({
                url: `/products/${id}/images`,
                method: 'POST',
                body,
            }),
            invalidatesTags: ['Products'],
        }),

        deleteImage: builder.mutation<
            void,
            { id: number; imageId: number }
        >({
            query: ({ id, imageId }) => ({
                url: `/products/${id}/images/${imageId}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['Products'],
        }),

        mainImage: builder.mutation<
            void,
            { id: number; imageId: number }
        >({
            query: ({ id, imageId }) => ({
                url: `/products/${id}/images/${imageId}/main`,
                method: 'PUT',
            }),
            invalidatesTags: ['Products'],
        }),

        reorderImages: builder.mutation<
            ProductImage[],
            { id: number; imageIds: number[] }
        >({
            query: ({ id, imageIds }) => ({
                url: `/products/${id}/images/reorder`,
                method: 'PUT',
                body: { imageIds },
            }),
            invalidatesTags: ['Products'],
        }),

        cart: builder.query<Cart, void>({
            query: () => '/cart',
            providesTags: ['Cart'],
        }),

        addCart: builder.mutation<
            Cart,
            {
                productId: number;
                variantId?: number;
                quantity: number;
            }
        >({
            query: (body) => ({
                url: '/cart/items',
                method: 'POST',
                body,
            }),
            invalidatesTags: ['Cart'],
        }),

        updateCart: builder.mutation<
            Cart,
            { id: number; quantity: number }
        >({
            query: ({ id, quantity }) => ({
                url: `/cart/items/${id}`,
                method: 'PUT',
                body: { quantity },
            }),
            invalidatesTags: ['Cart'],
        }),

        deleteCart: builder.mutation<Cart, number>({
            query: (id) => ({
                url: `/cart/items/${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['Cart'],
        }),

        checkout: builder.mutation<
            Order,
            CheckoutRequest
        >({
            query: (body) => ({
                url: '/orders',
                method: 'POST',
                body,
            }),
            invalidatesTags: [
                'Cart',
                'Orders',
                'Products',
                'Profile',
            ],
        }),

        orders: builder.query<
            Paged<Order>,
            { page: number; admin?: boolean }
        >({
            query: ({ page, admin }) => ({
                url: admin ? '/orders/all' : '/orders',
                params: { page, pageSize: 10 },
            }),
            providesTags: ['Orders'],
        }),

        orderStatus: builder.mutation<
            Order,
            { id: number; status: OrderStatus }
        >({
            query: ({ id, status }) => ({
                url: `/orders/${id}/status`,
                method: 'PUT',
                body: { status },
            }),
            invalidatesTags: ['Orders'],
        }),

        favorites: builder.query<
            Paged<ProductCard>,
            number
        >({
            query: (page) => ({
                url: '/favorites',
                params: { page, pageSize: 12 },
            }),
            providesTags: ['Favorites'],
        }),

        addFavorite: builder.mutation<void, number>({
            query: (id) => ({
                url: `/favorites/${id}`,
                method: 'POST',
            }),
            invalidatesTags: ['Favorites', 'Profile'],
        }),

        removeFavorite: builder.mutation<void, number>({
            query: (id) => ({
                url: `/favorites/${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['Favorites', 'Profile'],
        }),

        reviews: builder.query<
            Paged<Review>,
            { id: number; page: number }
        >({
            query: ({ id, page }) => ({
                url: `/products/${id}/reviews`,
                params: { page, pageSize: 10 },
            }),
            providesTags: ['Reviews'],
        }),

        addReview: builder.mutation<
            Review,
            {
                id: number;
                rating: number;
                comment: string;
            }
        >({
            query: ({ id, ...body }) => ({
                url: `/products/${id}/reviews`,
                method: 'POST',
                body,
            }),
            invalidatesTags: ['Reviews', 'Products'],
        }),
    }),
});

export const {
    useLoginMutation,
    useRegisterMutation,
    useProfileQuery,
    useUpdateProfileMutation,
    useUploadAvatarMutation,
    useDeleteAvatarMutation,
    useCategoriesQuery,
    useCreateCategoryMutation,
    useUpdateCategoryMutation,
    useDeleteCategoryMutation,
    useProductsQuery,
    useFeaturedQuery,
    useArrivalsQuery,
    useProductQuery,
    useCreateProductMutation,
    useUpdateProductMutation,
    useDeleteProductMutation,
    useUploadImagesMutation,
    useDeleteImageMutation,
    useMainImageMutation,
    useReorderImagesMutation,
    useCartQuery,
    useAddCartMutation,
    useUpdateCartMutation,
    useDeleteCartMutation,
    useCheckoutMutation,
    useOrdersQuery,
    useOrderStatusMutation,
    useFavoritesQuery,
    useAddFavoriteMutation,
    useRemoveFavoriteMutation,
    useReviewsQuery,
    useAddReviewMutation,
} = endpoints;