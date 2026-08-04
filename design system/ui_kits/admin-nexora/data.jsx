const TENANTS=[
 {n:'Dona Betinha',t:'Pizzaria',pl:'Completo',st:'Ativa',ver:'1.8.2',sync:'há 4 s',ped:'1.284/mês',saude:'ok'},
 {n:'Burger do Vale',t:'Hamburgueria',pl:'Operação + Gestão',st:'Ativa',ver:'1.8.2',sync:'há 12 s',ped:'2.140/mês',saude:'ok'},
 {n:'Cantina Bella',t:'Restaurante',pl:'Operação',st:'Ativa',ver:'1.7.4',sync:'há 18 min',ped:'860/mês',saude:'atencao'},
 {n:'Pastel da Feira',t:'Lanchonete',pl:'Operação',st:'Piloto',ver:'1.8.2',sync:'há 6 s',ped:'214/mês',saude:'ok'},
 {n:'Sabor Mineiro',t:'Restaurante',pl:'Completo',st:'Implantação',ver:'—',sync:'—',ped:'—',saude:'implantando'}];
const EVENTOS=[
 ['22:41','Dona Betinha','support.access_granted','Acesso de suporte por 60 min — autorizado por Sáskia'],
 ['22:18','Cantina Bella','sync.delayed','Atraso de sincronização acima de 5 min (18 min)'],
 ['21:52','Pastel da Feira','tenant.config_changed','Taxa de serviço 10% → 12%'],
 ['20:30','Burger do Vale','install.updated','Edge server 1.7.4 → 1.8.2, rollback disponível'],
 ['19:04','Sabor Mineiro','tenant.provisioned','Instância criada a partir do modelo RESTAURANTE']];
Object.assign(window,{TENANTS,EVENTOS});
