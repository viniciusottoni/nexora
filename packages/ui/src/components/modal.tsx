import { useEffect, useId, type MouseEvent, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

export interface ModalProps {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly eyebrow?: ReactNode;
  readonly title: ReactNode;
  readonly actions?: ReactNode;
  readonly children?: ReactNode;
  readonly className?: string;
  /** `danger` para confirmações destrutivas (ex.: revogar dispositivo) — troca o acento superior. */
  readonly tone?: 'default' | 'danger';
}

/**
 * Diálogo modal genérico — consolida o padrão de backdrop + painel que se repetia, idêntico,
 * em cada página de gestão (praças, papéis, dispositivos, mesas, categorias, produtos,
 * modificadores). Fecha com Escape ou clique no backdrop, além da ação que o chamador fornecer.
 */
export function Modal({
  open,
  onClose,
  eyebrow,
  title,
  actions,
  children,
  className = '',
  tone = 'default',
}: Readonly<ModalProps>) {
  const titleId = useId();

  useEffect(() => {
    if (!open) return;
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  function handleBackdropClick(event: MouseEvent<HTMLDivElement>) {
    if (event.target === event.currentTarget) onClose();
  }

  /*
   * Portal para o `body`: o véu é `position: fixed`, e qualquer ancestral com animação de
   * `transform` (`.nx-anim-in`, `.nx-stagger`, as entradas de página) passa a ser o bloco
   * contentor do fixo — o véu ficava recortado dentro do cartão/seção que abriu o diálogo
   * em vez de cobrir a viewport. Fora da árvore da página isso não acontece, e o
   * empilhamento também deixa de depender do `z-index` de quem chamou.
   */
  return createPortal(
    <div className="db-modal-backdrop nx-anim-in" onClick={handleBackdropClick}>
      <section
        className={`db-modal ${tone === 'danger' ? 'db-modal--danger' : ''} nx-anim-scale-in ${className}`.trim()}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
      >
        {eyebrow ? <p className="db-modal__eyebrow">{eyebrow}</p> : null}
        <h2 id={titleId} className="db-modal__title">
          {title}
        </h2>
        <div className="db-modal__body">{children}</div>
        {actions ? <div className="db-modal__actions">{actions}</div> : null}
      </section>
    </div>,
    document.body,
  );
}
