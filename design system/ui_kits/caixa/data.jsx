const MESAS_CX=[
 {n:'Mesa 01',s:'OPEN',t:'12 min',g:2,v:58.0,w:'Jonas'},
 {n:'Mesa 03',s:'READY',t:'26 min',g:4,v:164.8,w:'Jonas'},
 {n:'Mesa 07',s:'OPEN',t:'42 min',g:4,v:120.9,w:'Jonas'},
 {n:'Mesa 08',s:'BILL_REQUESTED',t:'1h 04',g:3,v:186.4,w:'Rita',att:true},
 {n:'Mesa 11',s:'OPEN',t:'8 min',g:2,v:34.0,w:'Rita'},
 {n:'Mesa 12',s:'PAID',t:'1h 18',g:6,v:312.0,w:'Jonas'}];
const CONTA=[
 {qty:1,nome:'Pizza G · Calabresa / Mussarela',mods:'borda catupiry',obs:'sem cebola',preco:72.9,status:'SERVED'},
 {qty:1,nome:'Pizza G · Frango com catupiry',preco:69.9,status:'SERVED'},
 {qty:3,nome:'Refrigerante lata',preco:21.0,status:'SERVED'},
 {qty:1,nome:'Fritas com cheddar',preco:34.0,status:'SERVED'},
 {qty:1,nome:'Porção de azeitona',preco:12.0,status:'CANCELLED',cancel:true}];
const FORMAS=[['Dinheiro','payments'],['Débito','credit_card'],['Crédito','credit_card'],['PIX','qr_code_2'],['Mercado Pago','smartphone']];
const brl=v=>'R$ '+v.toFixed(2).replace('.',',');
Object.assign(window,{MESAS_CX,CONTA,FORMAS,brl});
