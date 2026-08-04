Tabela densa. Toda coluna de valor recebe `numeric` (mono tabular à direita).

```jsx
<DataTable columns={[{key:'produto',header:'Produto'},{key:'cmv',header:'CMV',numeric:true}]} rows={rows} onRowClick={abrir} />
```

Coloque dentro de `<Card padding="none">`.
