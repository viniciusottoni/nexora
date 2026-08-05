// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { Branding, UpdateBrandingRequest } from '@nexora/contracts';
import { BrandingManagementPage } from './branding-management-page.js';

const branding: Branding = {
  colors: { primary: '#1E3446', secondary: '#B8965A', surface: '#F7F2E8', onPrimary: '#FFFFFF' },
  logo: {},
  fonts: { body: 'Inter', display: 'EB Garamond' },
  radius: 8,
  texts: { welcome: 'Bem-vindo', orderConfirmed: '', thanks: '', terms: '' },
  pwa: { name: 'Casa do Bairro', shortName: 'Casa', themeColor: '#1E3446', icons: [] },
};

describe('BrandingManagementPage', () => {
  it('pré-visualiza a marca atual e desabilita salvar sem alteração', () => {
    const { container } = render(
      <BrandingManagementPage
        tenantName="Casa do Bairro"
        branding={branding}
        onSave={async (patch) => ({ ...branding, ...patch }) as Branding}
        onUploadLogo={async () => ({ assetId: 'a', publicUrl: 'https://cdn.example/logo.svg' })}
      />,
    );
    const page = within(container);

    expect(page.getByText('Contraste WCAG AA atendido.')).toBeInTheDocument();
    expect(page.getByRole('button', { name: 'Salvar identidade visual' })).toBeDisabled();
    expect(
      page.getByRole('region', { name: 'Pré-visualização da identidade visual' }),
    ).toHaveTextContent('Casa do Bairro');
  });

  it('avisa sobre contraste insuficiente ao escolher uma cor primária de baixo contraste', () => {
    const { container } = render(
      <BrandingManagementPage
        tenantName="Casa do Bairro"
        branding={branding}
        onSave={async (patch) => ({ ...branding, ...patch }) as Branding}
        onUploadLogo={async () => ({ assetId: 'a', publicUrl: 'https://cdn.example/logo.svg' })}
      />,
    );
    const page = within(container);

    fireEvent.change(page.getByLabelText('Primária (seletor de cor)'), {
      target: { value: '#F8F4EC' },
    });

    expect(page.getByText('Cor com contraste WCAG AA insuficiente.')).toBeInTheDocument();
  });

  it('salva as alterações e mostra aviso de propagação sem novo build', async () => {
    const onSave = vi.fn(
      async (patch: UpdateBrandingRequest): Promise<Branding> =>
        ({ ...branding, ...patch }) as Branding,
    );

    const { container } = render(
      <BrandingManagementPage
        tenantName="Casa do Bairro"
        branding={branding}
        onSave={onSave}
        onUploadLogo={async () => ({ assetId: 'a', publicUrl: 'https://cdn.example/logo.svg' })}
      />,
    );
    const page = within(container);

    fireEvent.change(page.getByLabelText('Boas-vindas'), { target: { value: 'Bem-vindo(a)!' } });
    fireEvent.click(page.getByRole('button', { name: 'Salvar identidade visual' }));

    await waitFor(() => expect(onSave).toHaveBeenCalledTimes(1));
    expect(await page.findByRole('status')).toHaveTextContent(/até 60 segundos, sem novo build/);
  });

  it('envia a logo escolhida e a inclui no próximo salvamento', async () => {
    const onUploadLogo = vi.fn(async () => ({
      assetId: 'a',
      publicUrl: 'https://cdn.example/logo-dark.svg',
    }));
    const onSave = vi.fn(
      async (patch: UpdateBrandingRequest): Promise<Branding> =>
        ({ ...branding, ...patch }) as Branding,
    );

    const { container } = render(
      <BrandingManagementPage
        tenantName="Casa do Bairro"
        branding={branding}
        onSave={onSave}
        onUploadLogo={onUploadLogo}
      />,
    );
    const page = within(container);

    const file = new File(['logo'], 'logo.svg', { type: 'image/svg+xml' });
    fireEvent.change(page.getByLabelText('Logo escura'), { target: { files: [file] } });

    await waitFor(() => expect(onUploadLogo).toHaveBeenCalledWith('LOGO_DARK', file));
    expect(await page.findByRole('status')).toHaveTextContent(/Salve para publicar/);

    fireEvent.click(page.getByRole('button', { name: 'Salvar identidade visual' }));
    await waitFor(() =>
      expect(onSave).toHaveBeenCalledWith(
        expect.objectContaining({
          logo: expect.objectContaining({ dark: 'https://cdn.example/logo-dark.svg' }),
        }),
      ),
    );
  });
});
