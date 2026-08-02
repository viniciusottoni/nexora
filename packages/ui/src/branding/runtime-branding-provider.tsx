import { createContext, useContext, useEffect, useState, type PropsWithChildren } from 'react';
import type { BrandingResponse } from '@nexora/contracts';

import {
  applyBrandingToDocument,
  createLocalStorageBrandingCache,
  loadRuntimeBranding,
} from './runtime-branding.js';

const BrandingContext = createContext<BrandingResponse | undefined>(undefined);

export interface RuntimeBrandingProviderProps extends PropsWithChildren {
  readonly fallback: BrandingResponse;
  readonly endpoint?: string;
}

export function RuntimeBrandingProvider({
  children,
  fallback,
  endpoint = '/v1/public/branding',
}: Readonly<RuntimeBrandingProviderProps>) {
  const [branding, setBranding] = useState(fallback);
  useEffect(() => {
    const cache = createLocalStorageBrandingCache(window.localStorage);
    void loadRuntimeBranding({
      host: window.location.host,
      endpoint,
      fallback,
      cache,
      fetch: window.fetch.bind(window),
      apply: (value) => {
        applyBrandingToDocument(value);
        setBranding(value);
      },
    });
  }, [endpoint, fallback]);
  return <BrandingContext.Provider value={branding}>{children}</BrandingContext.Provider>;
}

export function useRuntimeBranding(): BrandingResponse {
  const value = useContext(BrandingContext);
  if (!value) throw new Error('RuntimeBrandingProvider não configurado');
  return value;
}
