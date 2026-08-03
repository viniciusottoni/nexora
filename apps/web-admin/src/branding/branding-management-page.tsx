import { useId, useState, type ChangeEvent } from 'react';
import {
  AlertBanner,
  BrandingPreview,
  Button,
  Card,
  ContrastAdvisor,
  Field,
  Input,
} from '@nexora/ui';
import type { Branding, UpdateBrandingRequest } from '@nexora/contracts';
import { LOGO_CONTENT_TYPE_BY_MIME, type LogoUploadResult } from './branding-api.js';
import './branding.css';

export interface BrandingManagementPageProps {
  readonly tenantName: string;
  readonly branding: Branding;
  readonly onSave: (patch: UpdateBrandingRequest) => Promise<Branding>;
  readonly onUploadLogo: (
    kind: 'LOGO_LIGHT' | 'LOGO_DARK',
    file: File,
  ) => Promise<LogoUploadResult>;
}

type Draft = Branding;

/**
 * Tela de administração de marca (US-003, gap "não existe tela de administração de marca") —
 * gestor edita cores/logo/textos com pré-visualização ao vivo (`BrandingPreview`) e aviso de
 * contraste WCAG AA (`ContrastAdvisor`, cenário Gherkin "Contraste mínimo garantido") ANTES de
 * salvar, sem depender de nenhum build por tenant (ADR-010): tudo aqui é `PATCH
 * /v1/tenant/branding`, nunca código condicional por cliente (ADR-013).
 */
export function BrandingManagementPage({
  tenantName,
  branding,
  onSave,
  onUploadLogo,
}: Readonly<BrandingManagementPageProps>) {
  const [draft, setDraft] = useState<Draft>(branding);
  const [busy, setBusy] = useState(false);
  const [uploadingLogo, setUploadingLogo] = useState<'LOGO_LIGHT' | 'LOGO_DARK'>();
  const [notice, setNotice] = useState<string>();
  const [error, setError] = useState<string>();
  const fontBodyId = useId();
  const fontDisplayId = useId();
  const radiusId = useId();
  const welcomeId = useId();
  const orderConfirmedId = useId();
  const thanksId = useId();
  const termsId = useId();

  const dirty = JSON.stringify(draft) !== JSON.stringify(branding);

  function updateColor(field: keyof Draft['colors'], value: string) {
    setDraft((current) => ({ ...current, colors: { ...current.colors, [field]: value } }));
    setNotice(undefined);
  }

  function updateFont(field: keyof Draft['fonts'], value: string) {
    setDraft((current) => ({ ...current, fonts: { ...current.fonts, [field]: value } }));
  }

  function updateText(field: keyof Draft['texts'], value: string) {
    setDraft((current) => ({ ...current, texts: { ...current.texts, [field]: value } }));
  }

  function updateRadius(value: number) {
    setDraft((current) => ({ ...current, radius: value }));
  }

  async function handleLogoChange(
    kind: 'LOGO_LIGHT' | 'LOGO_DARK',
    event: ChangeEvent<HTMLInputElement>,
  ) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    const contentType = LOGO_CONTENT_TYPE_BY_MIME[file.type];
    if (!contentType) {
      setError('Formato de imagem não suportado. Use SVG, PNG, JPEG ou WEBP.');
      return;
    }

    setUploadingLogo(kind);
    setError(undefined);
    try {
      const uploaded = await onUploadLogo(kind, file);
      setDraft((current) => ({
        ...current,
        logo: {
          ...current.logo,
          [kind === 'LOGO_LIGHT' ? 'light' : 'dark']: uploaded.publicUrl,
        },
      }));
      setNotice('Logo enviada. Salve para publicar a alteração.');
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Não foi possível enviar a logo.');
    } finally {
      setUploadingLogo(undefined);
    }
  }

  async function save() {
    setBusy(true);
    setError(undefined);
    try {
      const saved = await onSave({
        colors: { ...draft.colors },
        logo: { ...draft.logo },
        fonts: { ...draft.fonts },
        radius: draft.radius,
        texts: { ...draft.texts },
      });
      setDraft(saved);
      setNotice(
        'Identidade visual salva. A mudança chega a todas as telas em até 60 segundos, sem novo build.',
      );
    } catch (reason) {
      setError(
        reason instanceof Error ? reason.message : 'Não foi possível salvar a identidade visual.',
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="branding-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Identidade visual</p>
          <h1 className="db-page__title" id="branding-title">
            Marca do estabelecimento
          </h1>
          <p className="db-page__lead">
            Cores, logo e textos aplicados em runtime em todas as telas — cardápio, caixa e cozinha.
            Nenhum build novo é gerado ao salvar.
          </p>
        </div>
        <div className="db-page__actions">
          <Button type="button" busy={busy} disabled={!dirty} onClick={() => void save()}>
            Salvar identidade visual
          </Button>
        </div>
      </header>

      {notice ? (
        <AlertBanner tone="success" title="Identidade visual salva">
          {notice}
        </AlertBanner>
      ) : null}
      {error ? (
        <AlertBanner tone="danger" title="Falha ao salvar">
          {error}
        </AlertBanner>
      ) : null}

      <div className="db-workbench db-workbench--rail">
        <div className="db-stack nx-stagger">
          <Card
            className="db-form-card"
            title="Cores"
            subtitle="A cor primária veste botões e destaques; contraste é verificado aqui mesmo."
          >
            <ContrastAdvisor
              primary={draft.colors.primary}
              surface={draft.colors.surface}
              onPrimary={draft.colors.onPrimary}
            />
            <div className="branding-color-grid">
              <ColorField
                label="Primária"
                value={draft.colors.primary}
                onChange={(value) => updateColor('primary', value)}
              />
              <ColorField
                label="Secundária"
                value={draft.colors.secondary}
                onChange={(value) => updateColor('secondary', value)}
              />
              <ColorField
                label="Superfície"
                value={draft.colors.surface}
                onChange={(value) => updateColor('surface', value)}
              />
              <ColorField
                label="Sobre a primária"
                hint="Cor do texto sobre a cor primária"
                value={draft.colors.onPrimary}
                onChange={(value) => updateColor('onPrimary', value)}
              />
            </div>
          </Card>

          <Card
            className="db-form-card"
            title="Logo"
            subtitle="Envie versões clara e escura — a aplicação escolhe pelo tema do dispositivo do cliente."
          >
            <div className="branding-logo-grid">
              <LogoField
                label="Logo clara"
                kind="LOGO_LIGHT"
                previewSurface="var(--nx-gray-50)"
                currentUrl={draft.logo.light}
                uploading={uploadingLogo === 'LOGO_LIGHT'}
                onChange={handleLogoChange}
              />
              <LogoField
                label="Logo escura"
                kind="LOGO_DARK"
                previewSurface="var(--nx-navy-900)"
                currentUrl={draft.logo.dark}
                uploading={uploadingLogo === 'LOGO_DARK'}
                onChange={handleLogoChange}
              />
            </div>
          </Card>

          <Card className="db-form-card" title="Tipografia e forma">
            <div className="branding-color-grid">
              <Field label="Fonte de texto" htmlFor={fontBodyId}>
                <Input
                  id={fontBodyId}
                  value={draft.fonts.body}
                  onChange={(event) => updateFont('body', event.target.value)}
                />
              </Field>
              <Field label="Fonte de destaque" htmlFor={fontDisplayId}>
                <Input
                  id={fontDisplayId}
                  value={draft.fonts.display}
                  onChange={(event) => updateFont('display', event.target.value)}
                />
              </Field>
              <Field label="Raio de borda (px)" htmlFor={radiusId}>
                <Input
                  id={radiusId}
                  type="number"
                  min={0}
                  max={32}
                  value={draft.radius}
                  onChange={(event) => updateRadius(Number(event.target.value))}
                />
              </Field>
            </div>
          </Card>

          <Card
            className="db-form-card"
            title="Textos públicos"
            subtitle="O que o cliente lê no cardápio da mesa."
          >
            <Field label="Boas-vindas" htmlFor={welcomeId}>
              <Input
                id={welcomeId}
                value={draft.texts.welcome}
                onChange={(event) => updateText('welcome', event.target.value)}
              />
            </Field>
            <Field label="Confirmação de pedido" htmlFor={orderConfirmedId}>
              <Input
                id={orderConfirmedId}
                value={draft.texts.orderConfirmed}
                onChange={(event) => updateText('orderConfirmed', event.target.value)}
              />
            </Field>
            <Field label="Agradecimento" htmlFor={thanksId}>
              <Input
                id={thanksId}
                value={draft.texts.thanks}
                onChange={(event) => updateText('thanks', event.target.value)}
              />
            </Field>
            <Field label="Termos" htmlFor={termsId} hint="Exibido no rodapé do cardápio público">
              <textarea
                id={termsId}
                className="branding-terms"
                value={draft.texts.terms}
                onChange={(event) => updateText('terms', event.target.value)}
                rows={4}
              />
            </Field>
          </Card>
        </div>

        <aside className="branding-preview-column" aria-label="Pré-visualização">
          <p className="db-page__eyebrow">Pré-visualização</p>
          <BrandingPreview
            tenantName={tenantName}
            welcome={draft.texts.welcome}
            primary={draft.colors.primary}
            onPrimary={draft.colors.onPrimary}
            surface={draft.colors.surface}
            radius={draft.radius}
            {...(draft.logo.light ? { logo: draft.logo.light } : {})}
          />
        </aside>
      </div>
    </main>
  );
}

function ColorField({
  label,
  hint,
  value,
  onChange,
}: Readonly<{
  label: string;
  hint?: string;
  value: string;
  onChange: (value: string) => void;
}>) {
  const id = useId();
  const isValidHex = /^#[0-9a-fA-F]{6}$/.test(value);
  return (
    <Field label={label} htmlFor={id} {...(hint ? { hint } : {})}>
      <div className="branding-color-input">
        {/* O seletor nativo só aceita #rrggbb — enquanto o gestor digita um valor incompleto no
            campo de texto, ele fica ausente em vez de mostrar uma cor arbitrária de preenchimento
            (evitaria sugerir uma cor que o gestor não escolheu). */}
        {isValidHex ? (
          <input
            type="color"
            aria-label={`${label} (seletor de cor)`}
            value={value}
            onChange={(event) => onChange(event.target.value.toUpperCase())}
          />
        ) : null}
        <Input
          id={id}
          value={value}
          onChange={(event) => onChange(event.target.value.toUpperCase())}
        />
      </div>
    </Field>
  );
}

function LogoField({
  label,
  kind,
  previewSurface,
  currentUrl,
  uploading,
  onChange,
}: Readonly<{
  label: string;
  kind: 'LOGO_LIGHT' | 'LOGO_DARK';
  previewSurface: string;
  currentUrl?: string | undefined;
  uploading: boolean;
  onChange: (
    kind: 'LOGO_LIGHT' | 'LOGO_DARK',
    event: ChangeEvent<HTMLInputElement>,
  ) => Promise<void>;
}>) {
  const id = useId();
  return (
    <Field label={label} htmlFor={id}>
      <div className="branding-logo-preview" style={{ background: previewSurface }}>
        {currentUrl ? <img src={currentUrl} alt="" /> : <span>Sem logo enviada</span>}
      </div>
      <input
        id={id}
        type="file"
        accept="image/svg+xml,image/png,image/jpeg,image/webp"
        disabled={uploading}
        onChange={(event) => void onChange(kind, event)}
      />
      {uploading ? <span className="branding-logo-uploading">Enviando…</span> : null}
    </Field>
  );
}
