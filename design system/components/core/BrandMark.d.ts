/**
 * Assinatura de marca. Sem `logoSrc` nem `tenantName` desenha a marca Nexora
 * (`NexoraLogo`): colorida sobre fundo claro, branca com `inverse`.
 */
export interface BrandMarkProps {
  /** Caminho do arquivo de logo do tenant. Omitido, cai na marca Nexora vetorial. */
  logoSrc?: string;
  /** Nome do estabelecimento — usado quando o tenant ainda não enviou logo. */
  tenantName?: string;
  subtitle?: string;
  /** Altura da marca em px (ou base de escala do nome do tenant). */
  size?: number;
  /** Sobre navy/azul da marca: troca a marca pela versão branca. */
  inverse?: boolean;
  /** Empilha e centraliza — arranjo de cartão de login e de primeiro acesso. */
  center?: boolean;
}
export function BrandMark(props: BrandMarkProps): JSX.Element;
