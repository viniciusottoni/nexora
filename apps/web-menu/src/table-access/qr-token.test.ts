import { describe, expect, it } from 'vitest';
import { extractQrTokenFromLocation } from './qr-token.js';

describe('extractQrTokenFromLocation', () => {
  it('extrai o token do caminho /t/{token}', () => {
    expect(extractQrTokenFromLocation({ pathname: '/t/abc123', search: '' })).toBe('abc123');
  });

  it('extrai o token do caminho com barra final', () => {
    expect(extractQrTokenFromLocation({ pathname: '/t/abc123/', search: '' })).toBe('abc123');
  });

  it('decodifica o token do caminho quando vem url-encoded', () => {
    expect(extractQrTokenFromLocation({ pathname: '/t/abc%2F123', search: '' })).toBe('abc/123');
  });

  it('cai para a query ?t= quando nao ha caminho /t/', () => {
    expect(extractQrTokenFromLocation({ pathname: '/', search: '?t=xyz789' })).toBe('xyz789');
  });

  it('devolve null quando nao ha token em nenhum formato', () => {
    expect(extractQrTokenFromLocation({ pathname: '/', search: '' })).toBeNull();
  });

  it('prioriza o caminho sobre a query quando os dois estao presentes', () => {
    expect(extractQrTokenFromLocation({ pathname: '/t/do-caminho', search: '?t=da-query' })).toBe('do-caminho');
  });
});
