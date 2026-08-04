const ETAPAS=[['Fila (T0→T1)',1.6,3,'min'],['Montagem (T1→T2)',3.2,4,'min'],['Cocção (T2→T3)',7.1,7,'min'],['Finalização (T3→T4)',1.4,2,'min'],['Expedição (T4→T5)',2.4,2,'min']];
const HORAS=['17h','18h','19h','20h','21h','22h','23h'];
const DEMANDA=[[4,6,9,14,22,18,7],[3,5,8,12,19,15,6],[5,8,12,19,28,24,11],[7,11,17,26,38,32,15],[9,14,21,31,44,39,19],[6,9,13,20,29,26,12],[4,6,10,15,21,17,8]];
const DIAS=['Seg','Ter','Qua','Qui','Sex','Sáb','Dom'];
const CARDAPIO=[
 {p:'Calabresa G',v:182,fat:'R$ 11.812',cst:'R$ 18,42',mg:71.6,cl:'Estrela'},
 {p:'Mussarela G',v:164,fat:'R$ 9.512',cst:'R$ 30,04',mg:48.2,cl:'Cavalo de batalha'},
 {p:'Frango c/ catupiry G',v:38,fat:'R$ 2.656',cst:'R$ 21,60',mg:69.1,cl:'Quebra-cabeça'},
 {p:'Portuguesa G',v:96,fat:'R$ 6.912',cst:'R$ 34,10',mg:52.6,cl:'Cavalo de batalha'},
 {p:'Camarão G',v:6,fat:'R$ 588',cst:'R$ 76,10',mg:22.4,cl:'Abacaxi'},
 {p:'Romeu e Julieta',v:24,fat:'R$ 1.152',cst:'R$ 12,80',mg:73.3,cl:'Quebra-cabeça'}];
const RESULTADO=[['Receita bruta','R$ 128.420',''],['(−) CMV','R$ 42.120','32,8%'],['(−) Pessoal','R$ 31.180','24,3%'],['(=) Prime cost','R$ 73.300','57,1%'],['(−) Custo fixo','R$ 28.400','22,1%'],['(−) Taxa de cartão','R$ 2.184','1,7%'],['(=) Resultado','R$ 24.536','19,1%']];
Object.assign(window,{ETAPAS,HORAS,DEMANDA,DIAS,CARDAPIO,RESULTADO});
