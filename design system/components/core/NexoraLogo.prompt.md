Marca Nexora em SVG inline. Use quando precisar da marca crua; para assinatura com
subtítulo ou marca de tenant, use `BrandMark`.

**Regra de fundo, sem exceção:** fundo branco/claro → `tone="color"`; fundo navy ou azul
da marca → `tone="white"`. Nunca colorido sobre navy (o wordmark navy desaparece).

`variant="lockup"` (símbolo + NEXORA) é o padrão de interface. `variant="symbol"` é para
espaço apertado, favicon e para a animação de carregamento (`NexoraLoader`).

```jsx
<NexoraLogo height={40} />                          {/* cartão de login, fundo claro */}
<NexoraLogo tone="white" height={22} />             {/* SideNav navy, header do garçom */}
<NexoraLogo variant="symbol" height={44} />         {/* selo, avatar de app */}
<NexoraLogo height={42} shine />                    {/* dentro de um NexoraSplash */}
```

`shine`: um brilho, uma vez, da esquerda para a direita, mascarado no contorno exato do
traço (não um retângulo por cima). Só toca quando um `.is-open` ancestral aparece — é o
que `NexoraSplash` faz quando o cartão termina de abrir; fora desse contexto o brilho
fica parado, invisível. Use só na marca do cartão de login/primeiro acesso — não em toda
ocorrência de `NexoraLogo` (custa uma máscara SVG extra por instância).

Os arquivos equivalentes, para exportar para fora do kit (slides, e-mail, favicon), estão
em `assets/`: `logo-nexora-horizontal[-white].svg` (com assinatura),
`logo-nexora-lockup[-white].svg` e `logo-nexora-symbol[-white].svg`.
