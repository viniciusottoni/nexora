const MESAS=[
 {n:'Mesa 01',s:'OPEN',t:'12 min',g:2,v:'R$ 58,00',w:'Jonas'},
 {n:'Mesa 03',s:'READY',t:'26 min',g:4,v:'R$ 164,80',w:'Jonas',att:true},
 {n:'Mesa 05',s:'FREE'},
 {n:'Mesa 07',s:'OPEN',t:'42 min',g:4,v:'R$ 120,90',w:'Jonas'},
 {n:'Mesa 08',s:'BILL_REQUESTED',t:'1h 04',g:3,v:'R$ 186,40',w:'Rita',att:true},
 {n:'Mesa 09',s:'FREE'},
 {n:'Mesa 11',s:'OPEN',t:'8 min',g:2,v:'R$ 34,00',w:'Rita'},
 {n:'Mesa 12',s:'PAID',t:'1h 18',g:6,v:'R$ 312,00',w:'Jonas'}];
const FAVORITOS=[
 {nome:'Calabresa G',preco:64.9},{nome:'Mussarela G',preco:58},{nome:'Frango c/ catupiry G',preco:69.9},
 {nome:'Refri lata',preco:7},{nome:'Suco laranja',preco:12},{nome:'Fritas cheddar',preco:34},
 {nome:'Cerveja 600ml',preco:16},{nome:'Água 500ml',preco:5}];
const COMANDA=[
 {qty:1,nome:'Pizza G · Calabresa / Mussarela',mods:'borda catupiry',obs:'sem cebola',preco:72.9,status:'IN_OVEN'},
 {qty:2,nome:'Refrigerante lata',preco:14,status:'SERVED'},
 {qty:1,nome:'Fritas com cheddar',preco:34,status:'READY'}];
const brl=v=>'R$ '+v.toFixed(2).replace('.',',');
Object.assign(window,{MESAS,FAVORITOS,COMANDA,brl});
