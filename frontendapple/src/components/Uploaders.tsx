import { useEffect, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { Camera, Star, Trash2, Upload } from 'lucide-react';
import toast from 'react-hot-toast';

import {
    useDeleteAvatarMutation,
    useDeleteImageMutation,
    useMainImageMutation,
    useReorderImagesMutation,
    useUploadAvatarMutation,
    useUploadImagesMutation,
} from '../api/api';

import type {
    ProductImage,
    Profile,
} from '../types';

import {
    errorText,
    imageUrl,
    initials,
} from '../utils';

import { Button } from './UI';

const accept = {
    'image/jpeg': ['.jpg', '.jpeg'],
    'image/png': ['.png'],
    'image/webp': ['.webp'],
};

const maxSize = 5 * 1024 * 1024;

function usePreview(file: File | null) {
    const [preview, setPreview] = useState<string>();

    useEffect(() => {
        if (!file) {
            setPreview(undefined);
            return;
        }

        const url = URL.createObjectURL(file);
        setPreview(url);

        return () => URL.revokeObjectURL(url);
    }, [file]);

    return preview;
}

function FileThumbnail({
                           file,
                       }: {
    file: File;
}) {
    const preview = usePreview(file);

    return (
        <img
            src={preview}
            alt={file.name}
            className="aspect-square w-full rounded-xl object-cover"
        />
    );
}

export function FilePicker({
                               files,
                               onChange,
                           }: {
    files: File[];
    onChange: (files: File[]) => void;
}) {
    const dropzone = useDropzone({
        accept,
        maxSize,
        maxFiles: 10,

        onDrop: (accepted, rejected) => {
            if (rejected.length) {
                toast.error(
                    'Дозволено JPG, PNG, WebP до 5 МБ.',
                );
            }

            onChange([...files, ...accepted].slice(0, 10));
        },
    });

    return (
        <div>
            <div
                {...dropzone.getRootProps()}
                className={`dropzone ${
                    dropzone.isDragActive ? 'active' : ''
                }`}
            >
                <input {...dropzone.getInputProps()} />

                <Upload className="text-accent" size={25} />

                <p className="mt-3 text-sm">
                    Перетягни фото або натисни для вибору
                </p>

                <span className="mt-1 text-xs text-muted">
          JPG / PNG / WebP · до 5 МБ · до 10 файлів
        </span>
            </div>

            <div className="mt-4 grid grid-cols-3 gap-3">
                {files.map((file, index) => (
                    <div key={`${file.name}-${index}`}>
                        <FileThumbnail file={file} />

                        <button
                            type="button"
                            className="mt-2 text-xs text-red-300"
                            onClick={() =>
                                onChange(
                                    files.filter((_, i) => i !== index),
                                )
                            }
                        >
                            Прибрати
                        </button>
                    </div>
                ))}
            </div>
        </div>
    );
}

export function AvatarUploader({
                                   profile,
                               }: {
    profile: Profile;
}) {
    const [file, setFile] = useState<File | null>(null);
    const preview = usePreview(file);

    const [upload, uploadState] = useUploadAvatarMutation();
    const [remove, removeState] = useDeleteAvatarMutation();

    const dropzone = useDropzone({
        accept,
        maxSize,
        multiple: false,

        onDrop: (accepted, rejected) => {
            if (rejected.length) {
                toast.error('Оберіть зображення до 5 МБ.');
            }

            if (accepted[0]) setFile(accepted[0]);
        },
    });

    async function save() {
        if (!file) return;

        const body = new FormData();
        body.append('file', file);

        try {
            await upload(body).unwrap();
            setFile(null);
            toast.success('Аватар оновлено');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    async function deleteAvatar() {
        try {
            await remove().unwrap();
            setFile(null);
            toast.success('Аватар видалено');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    const source = preview ?? imageUrl(profile.avatarUrl);

    return (
        <div className="flex flex-col items-center gap-4">
            <div
                {...dropzone.getRootProps()}
                className="avatar-picker"
            >
                <input {...dropzone.getInputProps()} />

                {source ? (
                    <img src={source} alt="Аватар" />
                ) : (
                    <span>
            {initials(
                profile.firstName,
                profile.lastName,
            )}
          </span>
                )}

                <div className="avatar-overlay">
                    <Camera size={25} />
                    <span>Змінити фото</span>
                </div>
            </div>

            {file && (
                <div className="flex gap-2">
                    <Button
                        type="button"
                        onClick={save}
                        loading={uploadState.isLoading}
                    >
                        Зберегти фото
                    </Button>

                    <Button
                        type="button"
                        secondary
                        onClick={() => setFile(null)}
                    >
                        Скасувати
                    </Button>
                </div>
            )}

            {profile.avatarUrl && (
                <button
                    type="button"
                    className="text-xs text-red-300"
                    disabled={removeState.isLoading}
                    onClick={deleteAvatar}
                >
                    Видалити фото
                </button>
            )}
        </div>
    );
}

export function ProductImages({
                                  id,
                                  images,
                              }: {
    id: number;
    images: ProductImage[];
}) {
    const [files, setFiles] = useState<File[]>([]);
    const [dragged, setDragged] = useState<number | null>(null);

    const [upload, uploadState] = useUploadImagesMutation();
    const [remove] = useDeleteImageMutation();
    const [main] = useMainImageMutation();
    const [reorder] = useReorderImagesMutation();

    async function perform(
        action: () => Promise<unknown>,
    ) {
        try {
            await action();
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    async function saveImages() {
        const body = new FormData();
        files.forEach((file) => body.append('files', file));

        try {
            await upload({ id, body }).unwrap();
            setFiles([]);
            toast.success('Фото завантажені');
        } catch (error) {
            toast.error(errorText(error));
        }
    }

    async function drop(target: number) {
        if (dragged === null || dragged === target) return;

        const next = [...images];
        const [moved] = next.splice(dragged, 1);
        next.splice(target, 0, moved);
        setDragged(null);

        await perform(() =>
            reorder({
                id,
                imageIds: next.map((image) => image.id),
            }).unwrap(),
        );
    }

    return (
        <div className="space-y-5">
            <p className="text-xs text-muted">
                Перетягуй існуючі фото для зміни порядку.
            </p>

            <div className="grid grid-cols-3 gap-3">
                {images.map((image, index) => (
                    <div
                        key={image.id}
                        draggable
                        onDragStart={() => setDragged(index)}
                        onDragOver={(event) => event.preventDefault()}
                        onDrop={() => void drop(index)}
                        className="cursor-grab"
                    >
                        <img
                            src={imageUrl(image.thumbnailUrl)}
                            alt=""
                            className={`aspect-square rounded-xl object-cover ${
                                image.isMain ? 'ring-2 ring-accent' : ''
                            }`}
                        />

                        <div className="mt-2 flex justify-between">
                            <button
                                type="button"
                                aria-label="Зробити головним"
                                onClick={() =>
                                    void perform(() =>
                                        main({
                                            id,
                                            imageId: image.id,
                                        }).unwrap(),
                                    )
                                }
                            >
                                <Star
                                    size={17}
                                    className={
                                        image.isMain
                                            ? 'fill-accent text-accent'
                                            : 'text-muted'
                                    }
                                />
                            </button>

                            <button
                                type="button"
                                aria-label="Видалити фото"
                                onClick={() =>
                                    void perform(() =>
                                        remove({
                                            id,
                                            imageId: image.id,
                                        }).unwrap(),
                                    )
                                }
                            >
                                <Trash2 size={17} className="text-red-300" />
                            </button>
                        </div>
                    </div>
                ))}
            </div>

            <FilePicker files={files} onChange={setFiles} />

            {!!files.length && (
                <Button
                    type="button"
                    onClick={saveImages}
                    loading={uploadState.isLoading}
                >
                    Завантажити фото
                </Button>
            )}
        </div>
    );
}