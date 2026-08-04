const {Button,IconButton,Badge,Icon,Card,SideNav,TopBar,SegmentedControl,StatTile,ProgressMeter,DataTable,StatusPill,OrderTimer,AlertBanner,SyncStatus,BrandMark,TableCard,Select}=window.NexoraDesignSystem_aa692a;

function Pulso(){
  return <div style={{display:'flex',flexDirection:'column',gap:20}}>
    <div style={{background:'var(--surface-inverse)',borderRadius:'var(--brand-radius)',padding:20}}>
      <div style={{display:'flex',alignItems:'center',gap:10,marginBottom:16}}>
        <Icon name="monitor_heart" size={20} color="var(--nx-green-400)"/>
        <span style={{font:'var(--fw-semibold) 15px/1 var(--font-sans)',color:'#fff'}}>Pulso — agora</span>
        <span style={{marginLeft:'auto'}}><SyncStatus state="online" lastSync="há 4 s"/></span></div>
      <div style={{display:'grid',gridTemplateColumns:'repeat(5,1fr)',gap:12}}>
        <StatTile variant="pulse" label="Faturamento hoje" value="R$ 4.180" delta="+12,4%" comparison="vs. mesma terça"/>
        <StatTile variant="pulse" label="Pedidos em atraso" value="3" icon="warning"/>
        <StatTile variant="pulse" label="Tempo médio · 1h" value="11:40" target="≤ 10 min"/>
        <StatTile variant="pulse" label="Mesas ocupadas" value="6/8" comparison="ocupação 75%"/>
        <StatTile variant="pulse" label="Alertas abertos" value="2" icon="notifications_active"/></div>
    </div>
    <div style={{display:'grid',gridTemplateColumns:'1fr 360px',gap:20,alignItems:'start'}}>
      <Card title="Pedidos em produção" subtitle="Cronômetro por pedido · toda linha abre até o evento de origem" padding="tight">
        <div style={{display:'flex',flexDirection:'column',gap:8}}>
          {[['38','Mesa 03',742],['39','Delivery #4821',611],['40','Mesa 07',412],['41','Balcão',238],['42','Mesa 11',96]].map(([c,w,s])=>
            <div key={c} style={{display:'flex',alignItems:'center',gap:14,padding:'10px 12px',borderRadius:'var(--radius-md)',background:'var(--surface-page)'}}>
              <span style={{font:'var(--fw-black) 20px/1 var(--font-mono)',color:'var(--text-secondary)',minWidth:28}}>{c}</span>
              <span style={{font:'var(--type-body)',flex:'1 1 auto'}}>{w}</span>
              <StatusPill status={s>600?'LATE':'IN_OVEN'}/>
              <OrderTimer seconds={s} size="sm"/>
              <IconButton icon="chevron_right" label="Abrir pedido" size="sm"/></div>)}
        </div></Card>
      <div style={{display:'flex',flexDirection:'column',gap:16}}>
        <AlertBanner tone="danger" title="3 pedidos acima da meta" actions={<Button size="sm" variant="secondary">Ver fila</Button>}>
          Pico das 21h com 1 pizzaiolo na montagem.</AlertBanner>
        <AlertBanner tone="warning" title="Forno ocioso com fila há 4 min" actions={<Button size="sm" variant="secondary">Ver KDS</Button>}>
          2 posições livres e 6 pedidos esperando — perda de capacidade.</AlertBanner>
        <Card title="Meta do dia" padding="tight">
          <ProgressMeter value={4180} max={6000} display="R$ 4.180" tone="brand" caption="de R$ 6.000 · faltam R$ 1.820" size="lg"/></Card>
        <Card title="Mesas" padding="tight">
          <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:10}}>
            <TableCard name="Mesa 08" status="BILL_REQUESTED" elapsed="1h 04" guests={3} total="R$ 186,40" attention/>
            <TableCard name="Mesa 03" status="READY" elapsed="26 min" guests={4} total="R$ 164,80"/></div></Card>
      </div>
    </div>
  </div>;
}

function Desempenho(){
  const max=Math.max(...DEMANDA.flat());
  return <div style={{display:'flex',flexDirection:'column',gap:20}}>
    <div style={{display:'grid',gridTemplateColumns:'repeat(4,1fr)',gap:12}}>
      <StatTile label="Tempo total médio" value="15:40" unit="min" icon="timer" delta="-1,2 min" deltaDirection="up" target="≤ 10 min"/>
      <StatTile label="Percentil 90" value="23:10" unit="min" icon="show_chart" delta="+2,4 min" deltaDirection="down" comparison="o cliente insatisfeito"/>
      <StatTile label="Aderência ao prazo" value="82" unit="%" icon="task_alt" delta="+4 p.p." target="≥ 85%"/>
      <StatTile label="Pizzas por hora (real)" value="31" icon="local_pizza" comparison="teto teórico 42"/></div>
    <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:20,alignItems:'start'}}>
      <Card title="Tempo por etapa" subtitle="Onde está o gargalo — média × padrão da ficha">
        <div style={{display:'flex',flexDirection:'column',gap:16}}>
          {ETAPAS.map(([n,v,alvo,u])=><ProgressMeter key={n} label={n} value={v} max={9} display={v.toFixed(1)+' '+u}
            target={alvo} tone={v>alvo?'warning':'accent'} caption={'padrão '+alvo+' '+u}/>)}
        </div></Card>
      <Card title="Mapa de calor da demanda" subtitle="Pedidos por dia da semana e faixa horária">
        <div style={{display:'grid',gridTemplateColumns:'40px repeat(7,1fr)',gap:3,font:'var(--type-caption)'}}>
          <span></span>{HORAS.map(h=><span key={h} style={{textAlign:'center',color:'var(--text-muted)'}}>{h}</span>)}
          {DIAS.map((d,i)=><React.Fragment key={d}>
            <span style={{color:'var(--text-muted)',alignSelf:'center'}}>{d}</span>
            {DEMANDA[i].map((v,j)=><span key={j} title={v+' pedidos'} style={{height:30,borderRadius:4,display:'flex',alignItems:'center',justifyContent:'center',
              background:'color-mix(in oklab, var(--nx-navy-800) '+Math.round(v/max*100)+'%, var(--nx-gray-100))',
              color:v/max>.5?'#fff':'var(--text-secondary)',font:'var(--fw-medium) 11px/1 var(--font-mono)'}}>{v}</span>)}
          </React.Fragment>)}
        </div>
        <div style={{marginTop:14,font:'var(--type-caption)',color:'var(--text-muted)'}}>Pico sustentado: sexta, 21h — base para escala de pessoal e promessa de prazo.</div>
      </Card>
    </div>
    <div style={{display:'grid',gridTemplateColumns:'1fr 1fr 1fr',gap:20}}>
      <Card title="Venda por canal" padding="tight">
        {[['Salão','R$ 78.240',61,'brand'],['Delivery próprio','R$ 32.180',25,'accent'],['iFood','R$ 18.000',14,'warning']].map(([n,v,p,t])=>
          <div key={n} style={{marginBottom:14}}><ProgressMeter label={n} value={p} display={v} tone={t} caption={p+'% do faturamento'}/></div>)}</Card>
      <Card title="Pessoas" padding="tight">
        <DataTable compact columns={[{key:'n',header:'Garçom'},{key:'m',header:'Mesas',numeric:true},{key:'t',header:'Ticket',numeric:true}]}
          rows={[{n:'Jonas',m:'128',t:'R$ 96'},{n:'Rita',m:'116',t:'R$ 88'},{n:'Pedro',m:'74',t:'R$ 71'}]}/></Card>
      <Card title="Qualidade" padding="tight">
        <div style={{display:'flex',flexDirection:'column',gap:14}}>
          <ProgressMeter label="Nota média" value={4.6} max={5} display="4,6" tone="success"/>
          <ProgressMeter label="Retrabalho (re-fire)" value={2.1} max={10} display="2,1%" tone="warning"/>
          <ProgressMeter label="Ruptura de item" value={1.4} max={10} display="1,4%" tone="accent"/></div></Card>
    </div>
  </div>;
}

function Resultado(){
  const cls={'Estrela':'success','Cavalo de batalha':'warning','Quebra-cabeça':'info','Abacaxi':'danger'};
  return <div style={{display:'flex',flexDirection:'column',gap:20}}>
    <div style={{display:'grid',gridTemplateColumns:'repeat(4,1fr)',gap:12}}>
      <StatTile label="CMV" value="32,8" unit="%" icon="inventory_2" delta="+2,1 p.p." deltaDirection="down" target="≤ 30%"/>
      <StatTile label="Custo de pessoal" value="24,3" unit="%" icon="badge" comparison="folha + encargos"/>
      <StatTile label="Prime cost" value="57,1" unit="%" icon="functions" delta="-1,4 p.p." target="≤ 65%"/>
      <StatTile label="Ponto de equilíbrio" value="R$ 3.940" unit="/dia" icon="balance" comparison="média realizada R$ 4.280"/></div>
    <div style={{display:'grid',gridTemplateColumns:'1.4fr 1fr',gap:20,alignItems:'start'}}>
      <Card title="Engenharia de cardápio" subtitle="Volume × margem de contribuição — gerado da ficha técnica" padding="none"
        actions={<Button variant="secondary" size="sm" iconLeft="download">Exportar</Button>}>
        <DataTable onRowClick={()=>{}} columns={[
          {key:'p',header:'Produto'},{key:'v',header:'Vendidos',numeric:true},{key:'fat',header:'Faturamento',numeric:true},
          {key:'cst',header:'Custo/un',numeric:true},{key:'mg',header:'Margem',numeric:true,render:r=>r.mg.toFixed(1)+'%'},
          {key:'cl',header:'Classe',render:r=><Badge tone={cls[r.cl]} size="sm">{r.cl}</Badge>}]} rows={CARDAPIO}/>
      </Card>
      <div style={{display:'flex',flexDirection:'column',gap:16}}>
        <Card title="Resultado do período" subtitle="Julho · composição" padding="tight">
          <div style={{display:'flex',flexDirection:'column'}}>
            {RESULTADO.map(([n,v,p],i)=><div key={n} style={{display:'flex',alignItems:'baseline',gap:10,padding:'9px 0',
              borderTop:i?'1px solid var(--border-subtle)':0,font:n.startsWith('(=)')?'var(--fw-bold) 15px/1.3 var(--font-sans)':'var(--type-body)'}}>
              <span style={{color:n.startsWith('(−)')?'var(--text-secondary)':'var(--text-primary)'}}>{n}</span>
              <span style={{marginLeft:'auto',fontFamily:'var(--font-mono)',fontVariantNumeric:'tabular-nums',
                color:n==='(=) Resultado'?'var(--nx-success-600)':'var(--text-primary)'}}>{v}</span>
              <span style={{width:52,textAlign:'right',font:'var(--type-caption)',color:'var(--text-muted)',fontFamily:'var(--font-mono)'}}>{p}</span></div>)}
          </div></Card>
        <AlertBanner tone="danger" title="Camarão G com margem de 22,4%" actions={<Button size="sm" variant="secondary">Reprecificar</Button>}>
          6 unidades no mês. Reformular a ficha ou tirar do cardápio.</AlertBanner>
        <AlertBanner tone="warning" title="Divergência CMV teórico × real: 6,2%" actions={<Button size="sm" variant="secondary">Abrir contagem</Button>}>
          Mussarela: teórico 41,2 kg × real 36,8 kg. Porcionamento ou perda.</AlertBanner>
      </div>
    </div>
  </div>;
}

function PainelApp(){
  const [view,setView]=React.useState('pulso');
  const [per,setPer]=React.useState('Mês');
  return <div style={{display:'flex',height:'100vh',background:'var(--surface-page)'}}>
    <SideNav brand={<BrandMark inverse size={22} subtitle="Painel do dono"/>} activeId={view} onSelect={setView}
      items={[{group:'Tempo real'},{id:'pulso',label:'Pulso',icon:'monitor_heart',count:2},
        {group:'Gestão'},{id:'desemp',label:'Desempenho',icon:'insights'},{id:'result',label:'Resultado e custo',icon:'account_balance_wallet'},
        {id:'estoque',label:'Estoque e ficha',icon:'inventory_2'},{id:'fin',label:'Financeiro',icon:'savings'},
        {group:'Configuração'},{id:'metas',label:'Metas e limiares',icon:'flag'},{id:'aud',label:'Auditoria',icon:'history'}]}
      footer={<SyncStatus state="online" lastSync="há 4 s"/>}/>
    <div style={{flex:'1 1 auto',display:'flex',flexDirection:'column',minWidth:0}}>
      <TopBar title={view==='desemp'?'Desempenho operacional':view==='result'?'Resultado e custo':'Pulso da operação'}
        subtitle="Dona Betinha · terça, 22:48"
        right={<><SegmentedControl options={['Hoje','7 dias','Mês']} value={per} onChange={setPer}/>
          <Select options={['Todos os canais','Salão','Delivery próprio','iFood']}/>
          <IconButton icon="download" label="Exportar"/><IconButton icon="notifications" label="Alertas" badge={2}/></>}/>
      <div style={{flex:'1 1 auto',overflowY:'auto',padding:24}}>
        {view==='desemp'?<Desempenho/>:view==='result'?<Resultado/>:<Pulso/>}
      </div>
    </div>
  </div>;
}
window.PainelApp=PainelApp;
