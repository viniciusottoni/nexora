# UI kit — App do garçom

Recriação do fluxo do perfil **P2 · Garçom**: celular na mão, em pé, em movimento.
Autenticação por **PIN em dispositivo registrado** (RF-IAM-03), nunca e-mail e senha.

Ao contrário do kit da mesa, este usa a identidade da **Nexora** (navy), não a do
tenant: é ferramenta interna de operação.

## Telas
| Tela | Conteúdo | Requisitos |
|---|---|---|
| `Login` | Teclado numérico de 4 dígitos, dispositivo já identificado | RF-IAM-03/05 |
| `Mapa` | Mesas com status + tempo + valor, alerta de mesas que exigem ação, desempenho do turno | RF-SAL-05, RF-ALT-01 |
| `Mesa` | Comanda com status por item, item pronto na janela, tempos por etapa, transferência | RF-SAL-08/09, RF-PED-02 |
| `Lancamento` | Busca + 8 favoritos em grade — dois toques por item | doc otimização §5.2 |

## Decisões copiadas da especificação
- **Alerta vai até a pessoa**: "item pronto há 2 min" com o botão "Entreguei" no próprio alerta.
- Métrica individual visível **só para o próprio garçom** (§5.4, cuidado de gestão).
- Nenhum acesso a desconto, financeiro ou cancelamento de pagamento (matriz de permissões §5.1).
