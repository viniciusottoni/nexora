import { useEffect, useMemo, useRef, useState } from 'react';
import { AlertBanner, Button, Modal } from '@nexora/ui';
import type { ProductImageContentType } from './products-api.js';

export interface ImageCropResult {
  readonly blob: Blob;
  readonly width: number;
  readonly height: number;
  readonly contentType: ProductImageContentType;
}

export interface ImageCropDialogProps {
  readonly file: File;
  readonly onCancel: () => void;
  readonly onConfirm: (result: ImageCropResult) => void;
}

/** Proporção fixa do recorte — mesma proporção quadrada (1:1) da foto de produto em `MenuItemCard` (`packages/ui`). */
const OUTPUT_SIZE = 800;
const DISPLAY_SIZE = 240;
const MIN_ZOOM = 1;
const MAX_ZOOM = 3;

/**
 * Recorte assistido de imagem no upload, com proporção fixa (US-010 §10) — o gestor escolhe qual
 * parte da foto entra no quadro fixo (arrastar para posicionar, controle deslizante para
 * aproximar/afastar) antes de confirmar o upload. Implementado com `<canvas>` puro, sem
 * biblioteca externa nova (decisão documentada no relatório da tarefa: a US não exige um
 * cropper pixel-perfect, e uma dependência nova para isto não se justificava).
 */
export function ImageCropDialog({ file, onCancel, onConfirm }: Readonly<ImageCropDialogProps>) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const imageRef = useRef<HTMLImageElement | null>(null);
  const dragRef = useRef<{
    startX: number;
    startY: number;
    offsetX: number;
    offsetY: number;
  } | null>(null);

  const [imageReady, setImageReady] = useState(false);
  const [zoom, setZoom] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [error, setError] = useState<string>();

  const objectUrl = useMemo(() => URL.createObjectURL(file), [file]);
  // Canvas não codifica HEIC/HEIF de forma interoperável. Esses formatos podem ser usados como
  // entrada quando o navegador os decodifica, mas o recorte normalizado sai como JPEG.
  const contentType = (
    file.type === 'image/png' || file.type === 'image/webp' ? file.type : 'image/jpeg'
  ) as ProductImageContentType;

  useEffect(() => {
    const image = new Image();
    image.onload = () => {
      imageRef.current = image;
      setImageReady(true);
    };
    image.onerror = () => setError('Não foi possível ler esta imagem.');
    image.src = objectUrl;
    return () => URL.revokeObjectURL(objectUrl);
  }, [objectUrl]);

  useEffect(() => {
    if (!imageReady) return;
    draw(canvasRef.current, imageRef.current, DISPLAY_SIZE, zoom, offset);
  }, [imageReady, zoom, offset]);

  function baseScale(image: HTMLImageElement, size: number): number {
    return Math.max(size / image.naturalWidth, size / image.naturalHeight);
  }

  function clampOffset(
    next: { x: number; y: number },
    image: HTMLImageElement,
    totalZoom: number,
  ): { x: number; y: number } {
    const scale = baseScale(image, DISPLAY_SIZE) * totalZoom;
    const renderedWidth = image.naturalWidth * scale;
    const renderedHeight = image.naturalHeight * scale;
    const maxX = Math.max(0, (renderedWidth - DISPLAY_SIZE) / 2);
    const maxY = Math.max(0, (renderedHeight - DISPLAY_SIZE) / 2);
    return {
      x: Math.min(maxX, Math.max(-maxX, next.x)),
      y: Math.min(maxY, Math.max(-maxY, next.y)),
    };
  }

  function handlePointerDown(event: React.PointerEvent<HTMLCanvasElement>) {
    event.currentTarget.setPointerCapture(event.pointerId);
    dragRef.current = {
      startX: event.clientX,
      startY: event.clientY,
      offsetX: offset.x,
      offsetY: offset.y,
    };
  }

  function handlePointerMove(event: React.PointerEvent<HTMLCanvasElement>) {
    const drag = dragRef.current;
    const image = imageRef.current;
    if (!drag || !image) return;
    const next = {
      x: drag.offsetX + (event.clientX - drag.startX),
      y: drag.offsetY + (event.clientY - drag.startY),
    };
    setOffset(clampOffset(next, image, zoom));
  }

  function handlePointerUp() {
    dragRef.current = null;
  }

  function handleZoomChange(nextZoom: number) {
    const image = imageRef.current;
    setZoom(nextZoom);
    if (image) {
      setOffset((current) => clampOffset(current, image, nextZoom));
    }
  }

  function confirm() {
    const image = imageRef.current;
    if (!image) return;

    const output = document.createElement('canvas');
    output.width = OUTPUT_SIZE;
    output.height = OUTPUT_SIZE;
    // `draw` já converte `offset` (sempre no espaço de exibição, DISPLAY_SIZE) para o espaço do
    // canvas de destino internamente — passar o offset bruto aqui, sem pré-multiplicar.
    draw(output, image, OUTPUT_SIZE, zoom, offset);

    output.toBlob(
      (blob) => {
        if (!blob) {
          setError('Não foi possível gerar a imagem recortada.');
          return;
        }
        onConfirm({ blob, width: OUTPUT_SIZE, height: OUTPUT_SIZE, contentType });
      },
      contentType,
      0.9,
    );
  }

  return (
    <Modal
      open
      onClose={onCancel}
      eyebrow="Foto do produto"
      title="Recortar foto"
      actions={
        <>
          <Button type="button" variant="ghost" onClick={onCancel}>
            Cancelar
          </Button>
          <Button type="button" onClick={confirm} disabled={!imageReady}>
            Usar recorte
          </Button>
        </>
      }
    >
      <p className="db-hint">Arraste para posicionar e use o controle para aproximar.</p>

      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      <canvas
        ref={canvasRef}
        width={DISPLAY_SIZE}
        height={DISPLAY_SIZE}
        className="catalog-crop-canvas"
        role="img"
        aria-label="Área de recorte da foto"
        onPointerDown={handlePointerDown}
        onPointerMove={handlePointerMove}
        onPointerUp={handlePointerUp}
        onPointerLeave={handlePointerUp}
      />

      <label className="catalog-crop-zoom">
        Zoom
        <input
          type="range"
          min={MIN_ZOOM}
          max={MAX_ZOOM}
          step={0.05}
          value={zoom}
          onChange={(event) => handleZoomChange(Number(event.target.value))}
          aria-label="Ajustar zoom da foto"
        />
      </label>
    </Modal>
  );
}

function draw(
  canvas: HTMLCanvasElement | null,
  image: HTMLImageElement | null,
  size: number,
  zoom: number,
  offset: { x: number; y: number },
): void {
  if (!canvas || !image) return;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  const scale = Math.max(size / image.naturalWidth, size / image.naturalHeight) * zoom;
  const renderedWidth = image.naturalWidth * scale;
  const renderedHeight = image.naturalHeight * scale;
  const x = (size - renderedWidth) / 2 + offset.x * (size / DISPLAY_SIZE);
  const y = (size - renderedHeight) / 2 + offset.y * (size / DISPLAY_SIZE);

  ctx.clearRect(0, 0, size, size);
  ctx.drawImage(image, x, y, renderedWidth, renderedHeight);
}
