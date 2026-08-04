const CATEGORIAS=['Pizzas salgadas','Pizzas doces','Porções','Bebidas','Sobremesas'];
const PRODUTOS=[
 {id:'p1',cat:'Pizzas salgadas',nome:'Calabresa G',desc:'Molho de tomate, mussarela, calabresa fatiada, cebola',preco:64.9,prep:12,tag:'Mais vendida'},
 {id:'p2',cat:'Pizzas salgadas',nome:'Mussarela G',desc:'Molho de tomate, mussarela, orégano',preco:58.0,prep:11},
 {id:'p3',cat:'Pizzas salgadas',nome:'Frango com catupiry G',desc:'Frango desfiado, catupiry, milho',preco:69.9,prep:13},
 {id:'p4',cat:'Pizzas salgadas',nome:'Portuguesa G',desc:'Presunto, ovo, cebola, azeitona, mussarela',preco:72.0,prep:13,esgotado:true},
 {id:'p5',cat:'Porções',nome:'Fritas com cheddar',desc:'Porção 400g, cheddar e bacon',preco:34.0,prep:8},
 {id:'p6',cat:'Bebidas',nome:'Refrigerante lata 350ml',desc:'Cola, guaraná ou laranja',preco:7.0,prep:1},
 {id:'p7',cat:'Bebidas',nome:'Suco de laranja 500ml',desc:'Natural, sem açúcar',preco:12.0,prep:3},
 {id:'p8',cat:'Sobremesas',nome:'Pizza doce Romeu e Julieta',desc:'Goiabada cremosa e queijo minas',preco:48.0,prep:10}];
const MODIFICADORES=[
 {grupo:'Ponto da massa',tipo:'radio',opcoes:[{n:'Tradicional',p:0},{n:'Fina',p:0},{n:'Bem assada',p:0}]},
 {grupo:'Borda',tipo:'check',opcoes:[{n:'Catupiry',p:8},{n:'Cheddar',p:8},{n:'Chocolate',p:10}]},
 {grupo:'Remover',tipo:'check',opcoes:[{n:'Sem cebola',p:0},{n:'Sem azeitona',p:0},{n:'Sem orégano',p:0}]}];
const CONSUMO=[
 {qty:1,nome:'Pizza G · Calabresa / Mussarela',mods:'meio a meio · borda catupiry',obs:'sem cebola',preco:72.9,status:'IN_OVEN'},
 {qty:2,nome:'Refrigerante lata 350ml',preco:14.0,status:'SERVED'},
 {qty:1,nome:'Fritas com cheddar',preco:34.0,status:'READY'}];
const brl=v=>'R$ '+v.toFixed(2).replace('.',',');
Object.assign(window,{CATEGORIAS,PRODUTOS,MODIFICADORES,CONSUMO,brl});
