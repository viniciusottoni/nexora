const {Button,IconButton,Badge,Icon,Card,Input,Field,Select,Switch,Checkbox,SideNav,TopBar,SegmentedControl,StatTile,ProgressMeter,DataTable,StatusPill,AlertBanner,SyncStatus,BrandMark,EmptyState}=window.NexoraDesignSystem_aa692a;

const SAUDE={ok:['success','Saudável'],atencao:['warning','Atenção'],implantando:['info','Implantando']};

function Instancias({onOpen}){
  const cols=[
    {key:'n',header:'Estabelecimento',render:r=><div><div style={{font:'var(--fw-semibold) 14px/1.3 var(--font-sans)'}}>{r.n}</div>
      <div style={{font:'var(--type-caption)',color:'var(--text-muted)'}}>{r.t}</div></div>},
    {key:'pl',header:'Plano'},
    {key:'st',header:'Status',render:r=><Badge tone={r.st==='Ativa'?'success':r.st==='Piloto'?'info':'warning'} size="sm">{r.st}</Badge>},
    {key:'ver',header:'Versão',numeric:true},
    {key:'sync',header:'Sync',render:r=>r.sync==='—'?<span style={{color:'var(--text-muted)'}}>—</span>
      :<SyncStatus state={r.saude==='atencao'?'delayed':'online'} lastSync={r.sync}/>},
    {key:'ped',header:'Volume',numeric:true},
    {key:'saude',header:'Saúde',render:r=><Badge tone={SAUDE[r.saude][0]} size="sm">{SAUDE[r.saude][1]}</Badge>}];
  return <div style={{display:'flex',flexDirection:'column',gap:20}}>
    <div style={{display:'grid',gridTemplateColumns:'repeat(5,1fr)',gap:12}}>
      <StatTile label="Instâncias ativas" value="4" icon="storefront" comparison="1 em implantação"/>
      <StatTile label="Sync atrasada" value="1" icon="sync_problem"/>
      <StatTile label="Parque na última versão" value="80" unit="%" icon="upgrade" target="100%"/>
      <StatTile label="Tempo médio de implantação" value="4,2" unit="dias" icon="rocket_launch" target="≤ 5 dias"/>
      <StatTile label="Chamados abertos" value="3" icon="support_agent" delta="-2" comparison="vs. semana anterior"/></div>
    <AlertBanner tone="warning" title="Cantina Bella · sincronização atrasada há 18 min"
      actions={<><Button size="sm" variant="secondary">Diagnóstico</Button><Button size="sm" variant="primary">Solicitar acesso</Button></>}>
      862 eventos na fila local. A operação da loja continua; o painel do dono está defasado.</AlertBanner>
    <Card title="Instâncias" subtitle="Isolamento de dados por tenant — nenhuma consulta cruza fronteira (RN-015)" padding="none"
      actions={<><Input size="md" icon="search" placeholder="Buscar"/><Button variant="primary" size="sm" iconLeft="add">Provisionar</Button></>}>
      <DataTable columns={cols} rows={TENANTS} onRowClick={onOpen}/></Card>
  </div>;
}

function Provisionar(){
  return <div style={{display:'grid',gridTemplateColumns:'1fr 380px',gap:20,alignItems:'start'}}>
    <Card title="Nova instância" subtitle="Sem alteração de código — só configuração (RF-PLT-05)"
      footer={<><Button variant="ghost">Cancelar</Button><Button variant="primary" iconLeft="rocket_launch">Provisionar e gerar install.sh</Button></>}>
      <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:16}}>
        <Field label="Nome do estabelecimento" required><Input defaultValue="Sabor Mineiro"/></Field>
        <Field label="Slug / subdomínio" required hint="cardapio.<slug>.nexora.app"><Input defaultValue="sabor-mineiro"/></Field>
        <Field label="Modelo de negócio" hint="Traz cardápio e configuração pré-montados"><Select options={['Pizzaria','Hamburgueria','Restaurante','Lanchonete']} defaultValue="Restaurante"/></Field>
        <Field label="Plano"><Select options={['Operação','Operação + Gestão','Completo']} defaultValue="Completo"/></Field>
      </div>
      <div style={{marginTop:20,paddingTop:16,borderTop:'1px solid var(--border-subtle)'}}>
        <div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)',marginBottom:12}}>Identidade visual</div>
        <div style={{display:'grid',gridTemplateColumns:'1fr 1fr 1fr',gap:16}}>
          <Field label="Cor primária"><Input prefix="#" defaultValue="C1121F"/></Field>
          <Field label="Cor secundária"><Input prefix="#" defaultValue="669BBC"/></Field>
          <Field label="Raio de borda"><Input numeric suffix="px" defaultValue="12"/></Field></div>
        <div style={{marginTop:16,padding:16,border:'1px dashed var(--border-default)',borderRadius:'var(--brand-radius)',textAlign:'center',color:'var(--text-muted)'}}>
          <Icon name="upload_file" size={26}/><div style={{font:'var(--type-caption)',marginTop:6}}>Logo claro e escuro · favicon · ícone do PWA</div></div>
      </div>
      <div style={{marginTop:20,paddingTop:16,borderTop:'1px solid var(--border-subtle)',display:'flex',flexDirection:'column',gap:14}}>
        <div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)'}}>Módulos ativos</div>
        <Switch label="KDS de cozinha" defaultChecked/>
        <Switch label="Delivery próprio" defaultChecked/>
        <Switch label="Estoque e ficha técnica" defaultChecked/>
        <Switch label="Financeiro de gestão"/>
      </div>
    </Card>
    <div style={{display:'flex',flexDirection:'column',gap:16}}>
      <Card title="Prévia do tenant" subtitle="Tokens aplicados em runtime" padding="tight">
        <div data-tenant="dona-betinha" style={{background:'var(--brand-surface)',borderRadius:'var(--brand-radius)',padding:16,display:'flex',flexDirection:'column',gap:12}}>
          <BrandMark tenantName="Sabor Mineiro" subtitle="Restaurante" size={32}/>
          <Button variant="primary" size="lg" block iconLeft="send">Enviar pedido</Button>
          <div style={{display:'flex',gap:8}}><Badge tone="neutral">Salão</Badge><Badge tone="neutral">Delivery</Badge></div></div>
      </Card>
      <Card title="Checklist de implantação" padding="tight">
        {[['Instância e domínio',1],['Identidade visual',1],['Cardápio e fichas',0],['Mesas, perfis e regras',0],['Servidor local + rede',0],['Meios de pagamento',0],['Treinamento e piloto',0]].map(([n,ok])=>
          <div key={n} style={{display:'flex',alignItems:'center',gap:10,padding:'8px 0',font:'var(--type-body)'}}>
            <Icon name={ok?'check_circle':'radio_button_unchecked'} size={18} color={ok?'var(--nx-success-500)':'var(--text-disabled)'} fill={!!ok}/>
            <span style={{color:ok?'var(--text-primary)':'var(--text-muted)'}}>{n}</span></div>)}
      </Card>
      <Card title="Importar" padding="tight">
        <Button variant="secondary" size="md" block iconLeft="table_view">Cardápio e ficha por planilha</Button></Card>
    </div>
  </div>;
}

function Auditoria(){
  return <Card title="Trilha da plataforma" subtitle="Imutável — nenhum usuário altera ou apaga (RF-AUD-04)" padding="none"
    actions={<><Select options={['Todas as instâncias','Dona Betinha','Cantina Bella']}/><Button variant="secondary" size="sm" iconLeft="download">Exportar</Button></>}>
    <DataTable columns={[
      {key:0,header:'Hora',render:r=><span style={{fontFamily:'var(--font-mono)'}}>{r[0]}</span>},
      {key:1,header:'Instância'},
      {key:2,header:'Evento',render:r=><Badge tone="neutral" size="sm" square>{r[2]}</Badge>},
      {key:3,header:'Detalhe'}]} rows={EVENTOS}/>
  </Card>;
}

function AdminApp(){
  const [view,setView]=React.useState('inst');
  return <div style={{display:'flex',height:'100vh',background:'var(--surface-page)'}}>
    <SideNav brand={<BrandMark inverse size={24} subtitle="Plataforma"/>} variant="dark" activeId={view} onSelect={setView}
      items={[{group:'Plataforma'},{id:'inst',label:'Instâncias',icon:'storefront',count:5},{id:'prov',label:'Provisionar',icon:'add_business'},
        {id:'saude',label:'Saúde do parque',icon:'health_and_safety',count:1},{group:'Produto'},{id:'mod',label:'Modelos de negócio',icon:'category'},
        {id:'ver',label:'Versões e rollout',icon:'upgrade'},{group:'Governança'},{id:'aud',label:'Auditoria',icon:'history'},{id:'sup',label:'Suporte',icon:'support_agent',count:3}]}
      footer={<div style={{font:'var(--type-caption)',color:'rgba(255,255,255,.5)'}}>Replay Studio · admin</div>}/>
    <div style={{flex:'1 1 auto',display:'flex',flexDirection:'column',minWidth:0}}>
      <TopBar title={view==='prov'?'Provisionar instância':view==='aud'?'Auditoria da plataforma':'Instâncias'}
        subtitle="Nexora · plataforma de gestão inteligente"
        right={<><SegmentedControl options={['Todas','Ativas','Piloto']} value="Todas" onChange={()=>{}}/>
          <IconButton icon="notifications" label="Alertas" badge={1}/></>}/>
      <div style={{flex:'1 1 auto',overflowY:'auto',padding:24}}>
        {view==='prov'?<Provisionar/>:view==='aud'?<Auditoria/>:<Instancias onOpen={()=>setView('prov')}/>}
      </div>
    </div>
  </div>;
}
window.AdminApp=AdminApp;
