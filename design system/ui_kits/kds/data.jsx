const FILA=[
 {code:'38',where:'Mesa 03',ch:'DINE_IN',s:742,fire:'agora',itens:[
   {qty:2,name:'Pizza G · Mussarela',modifiers:'bem assada'},{qty:1,name:'Fritas com cheddar',done:true}]},
 {code:'39',where:'Delivery #4821',ch:'DELIVERY',s:611,fire:'agora',itens:[
   {qty:1,name:'Pizza G · Calabresa',modifiers:'sem cebola · borda catupiry'},{qty:1,name:'Refri 2L'}]},
 {code:'40',where:'Mesa 07',ch:'DINE_IN',s:412,fire:'em 2 min',itens:[
   {qty:1,name:'Pizza G · Frango c/ catupiry'},{qty:1,name:'Pizza G · Portuguesa',modifiers:'sem ovo'}]},
 {code:'41',where:'Balcão',ch:'COUNTER',s:238,fire:'em 4 min',itens:[
   {qty:1,name:'Pizza M · Mussarela',modifiers:'massa fina'}]},
 {code:'42',where:'Mesa 11',ch:'DINE_IN',s:96,fire:'em 6 min',itens:[
   {qty:1,name:'Pizza G · Romeu e Julieta'},{qty:2,name:'Suco de laranja'}]},
 {code:'43',where:'Delivery #4822',ch:'DELIVERY',s:41,fire:'em 8 min',itens:[
   {qty:2,name:'Pizza G · Calabresa',modifiers:'uma sem cebola'}]}];
const ALLDAY=[['Mussarela G',5],['Calabresa G',4],['Frango c/ catupiry G',2],['Portuguesa G',2],['Romeu e Julieta',1],['Fritas cheddar',3]];
const FORNO=[{c:'38',left:'0:40'},{c:'39',left:'1:20'},{c:'40',left:'3:10'},null,null];
Object.assign(window,{FILA,ALLDAY,FORNO});
