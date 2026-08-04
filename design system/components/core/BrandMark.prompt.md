Assinatura de marca da plataforma ou do tenant. Sem `logoSrc` desenha a marca Nexora
vetorial; se o tenant não tem logo, mostra a inicial em bloco + nome em tipo.

**Regra de fundo:** fundo branco/claro → marca colorida (padrão); fundo navy ou azul da
marca → `inverse`, que troca para a versão branca. Em cartão de login e de primeiro
acesso use `center`, que empilha e centraliza.

```jsx
<BrandMark size={28} subtitle="Painel do dono" />        {/* fundo claro */}
<BrandMark inverse size={22} subtitle="Caixa" />         {/* SideNav navy */}
<BrandMark center size={40} />                           {/* cartão de login */}
<BrandMark tenantName="Dona Betinha" subtitle="Pizzaria" size={36} />
<BrandMark logoSrc={tenant.logoUrl} tenantName="Dona Betinha" size={30} />
```

Não misture as duas marcas na mesma tela: ferramenta interna e plataforma são Nexora;
PWA do cliente e canais públicos são do tenant.
