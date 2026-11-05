import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';

import {
    useLoginMutation,
    useRegisterMutation,
} from '../api/api';

import { useAppDispatch } from '../app/hooks';
import { setCredentials } from '../features/authSlice';

import { Button } from '../components/UI';
import { errorText } from '../utils';

const schema = z.object({
    email: z.string().email('Введи коректний email'),
    password: z.string().min(8, 'Мінімум 8 символів'),
    firstName: z.string(),
    lastName: z.string(),
});

type Values = z.infer<typeof schema>;

export function AuthPage({
                             registration = false,
                         }: {
    registration?: boolean;
}) {
    const dispatch = useAppDispatch();
    const navigate = useNavigate();

    const [login, loginState] = useLoginMutation();
    const [registerUser, registerState] = useRegisterMutation();

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<Values>({
        resolver: zodResolver(schema),

        defaultValues: {
            email: '',
            password: '',
            firstName: '',
            lastName: '',
        },
    });

    async function submit(values: Values) {
        if (
            registration &&
            (!values.firstName.trim() || !values.lastName.trim())
        ) {
            toast.error('Вкажи ім’я та прізвище');
            return;
        }

        try {
            const result = registration
                ? await registerUser(values).unwrap()
                : await login({
                    email: values.email,
                    password: values.password,
                }).unwrap();

            dispatch(setCredentials(result));
            toast.success(`Вітаємо, ${result.user.firstName}!`);
            navigate('/profile');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    return (
        <section className="page mx-auto max-w-md">
            <div className="panel p-7 md:p-9">
        <span className="eyebrow">
          {registration ? 'JOIN THE WORLD' : 'WELCOME BACK'}
        </span>

                <h1 className="mt-5 text-3xl font-semibold">
                    {registration ? 'Створи' : 'Відкрий'} свій{' '}
                    <span className="script">Apple.</span>
                </h1>

                <form
                    onSubmit={handleSubmit(submit)}
                    className="mt-7 space-y-4"
                >
                    {registration && (
                        <div className="grid gap-3 sm:grid-cols-2">
                            <input
                                required
                                className="field"
                                placeholder="Ім’я"
                                {...register('firstName')}
                            />

                            <input
                                required
                                className="field"
                                placeholder="Прізвище"
                                {...register('lastName')}
                            />
                        </div>
                    )}

                    <div>
                        <input
                            type="email"
                            className="field"
                            placeholder="Email"
                            autoComplete="email"
                            {...register('email')}
                        />

                        <p className="mt-2 text-xs text-red-300">
                            {errors.email?.message}
                        </p>
                    </div>

                    <div>
                        <input
                            type="password"
                            className="field"
                            placeholder="Пароль"
                            autoComplete={
                                registration
                                    ? 'new-password'
                                    : 'current-password'
                            }
                            {...register('password')}
                        />

                        <p className="mt-2 text-xs text-red-300">
                            {errors.password?.message}
                        </p>
                    </div>

                    <Button
                        className="w-full"
                        loading={
                            loginState.isLoading ||
                            registerState.isLoading
                        }
                    >
                        {registration ? 'Зареєструватися' : 'Увійти'}
                    </Button>
                </form>

                <p className="mt-6 text-center text-sm text-muted">
                    {registration ? 'Вже є акаунт?' : 'Ще немає акаунту?'}{' '}

                    <Link
                        to={registration ? '/login' : '/register'}
                        className="text-accent"
                    >
                        {registration ? 'Увійти' : 'Реєстрація'}
                    </Link>
                </p>
            </div>
        </section>
    );
}