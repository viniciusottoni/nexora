/**
 * Conversões de dinheiro para a máscara de moeda de variações/preços (US-011 §10: "Preço digitado
 * com máscara de moeda; armazenado em centavos"). O back-end trafega `decimal` como **string**
 * (ex.: `"45.90"`, ADR-017 — nunca `number`/`float`, para não perder precisão no JSON), então toda
 * conversão aqui é feita com aritmética inteira sobre centavos, nunca `parseFloat`/`Number` direto
 * sobre a string decimal — não há necessidade de trazer uma lib de precisão decimal nova
 * (`decimal.js` etc.) para um valor que é só dígitos + 2 casas fixas.
 */

/** `"45.90"` (contrato da API) -> `4590` (centavos, para a máscara de edição). */
export function decimalStringToCents(value: string): number {
  const negative = value.trim().startsWith('-');
  const [rawInt, rawDec = ''] = value.trim().replace('-', '').split('.');
  const intPart = rawInt === '' ? 0 : Number(rawInt);
  const decPart = (rawDec + '00').slice(0, 2);
  const cents = intPart * 100 + Number(decPart || '0');
  return negative ? -cents : cents;
}

/** `4590` (centavos) -> `"45.90"` (formato exigido pelo contrato da API, ADR-017). */
export function centsToDecimalString(cents: number): string {
  const negative = cents < 0;
  const absolute = Math.abs(Math.trunc(cents));
  const intPart = Math.floor(absolute / 100);
  const decPart = String(absolute % 100).padStart(2, '0');
  return `${negative ? '-' : ''}${intPart}.${decPart}`;
}

/** `4590` (centavos) -> `"45,90"` (exibição pt-BR, sem o símbolo — o prefixo "R$" fica no `Input`). */
export function centsToDisplay(cents: number): string {
  const negative = cents < 0;
  const absolute = Math.abs(Math.trunc(cents));
  const intPart = String(Math.floor(absolute / 100)).replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  const decPart = String(absolute % 100).padStart(2, '0');
  return `${negative ? '-' : ''}${intPart},${decPart}`;
}

/**
 * Extrai os centavos digitados por um `<input>` de máscara de moeda — todo caractere não
 * numérico é descartado e o restante é lido como centavos (padrão "digitar da direita para a
 * esquerda", igual a um campo de valor de POS/caixa): cada tecla redesenha o valor inteiro a
 * partir dos dígitos que sobraram, nunca insere no meio de um número já formatado.
 */
export function digitsToCents(rawInput: string): number {
  const digits = rawInput.replace(/\D/g, '');
  return digits === '' ? 0 : Number(digits);
}
