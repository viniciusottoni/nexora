const {Button,IconButton,Badge,Icon,Card,Input,SegmentedControl,StatusPill,OrderTimer,TableCard,OrderLine,SyncStatus,BrandMark,AlertBanner,NumericKeypad,StatTile,QuantityStepper,EmptyState}=window.NexoraDesignSystem_aa692a;

function Shell({title,sub,onBack,right,children,footer,pad=true}){
  return <div style={{display:'flex',flexDirection:'column',height:'100%',background:'var(--surface-page)'}}>
    <header style={{flex:'0 0 auto',background:'var(--nx-navy-900)',color:'#fff',padding:'12px 14px',display:'flex',alignItems:'center',gap:10}}>
      {onBack?<button onClick={onBack} aria-label="Voltar" style={{border:0,background:'rgba(255,255,255,.14)',color:'#fff',width:36,height:36,borderRadius:10,display:'flex',alignItems:'center',justifyContent:'center',cursor:'pointer'}}><Icon name="arrow_back" size={20}/></button>:null}
      <div style={{minWidth:0,flex:'1 1 auto'}}>
        <div style={{font:'var(--fw-bold) 17px/1.2 var(--font-sans)'}}>{title}</div>
        <div style={{font:'var(--type-caption)',color:'rgba(255,255,255,.66)',marginTop:1}}>{sub}</div></div>
      {right}
    </header>
    <main style={{flex:'1 1 auto',overflowY:'auto',padding:pad?14:0,display:'flex',flexDirection:'column',gap:12}}>{children}</main>
    {footer?<footer style={{flex:'0 0 auto',padding:'12px 14px 16px',background:'var(--surface-card)',borderTop:'1px solid var(--border-subtle)'}}>{footer}</footer>:null}
  </div>;
}

function Login({onEnter}){
  const [pin,setPin]=React.useState('');
  return <div style={{height:'100%',background:'var(--nx-navy-900)',display:'flex',flexDirection:'column',padding:'40px 24px 28px',color:'#fff'}}>
    <BrandMark center inverse size={30} subtitle="Salão · Terminal do garçom" style={{alignSelf:'center'}}/>
    <div style={{marginTop:32,textAlign:'center'}}>
      <div style={{font:'var(--fw-bold) 22px/1.3 var(--font-sans)'}}>Jonas Ribeiro</div>
      <div style={{font:'var(--type-caption)',color:'rgba(255,255,255,.6)',marginTop:4}}>Dispositivo registrado · PIN de 4 dígitos</div></div>
    <div data-surface="kds" style={{marginTop:32,background:'transparent'}}><NumericKeypad dark value={pin} onChange={setPin} onSubmit={onEnter} length={4} showDots/></div>
    <div style={{marginTop:'auto',display:'flex',justifyContent:'center'}}><SyncStatus state="local" queued={0}/></div>
  </div>;
}

function Mapa({onMesa}){
  const [amb,setAmb]=React.useState('Salão');
  const att=MESAS.filter(m=>m.att).length;
  return <Shell title="Mapa de mesas" sub="Jonas · turno das 18:00" right={<SyncStatus state="local" queued={4}/>}
    footer={<div style={{display:'flex',gap:10}}><Button variant="secondary" size="lg" block iconLeft="qr_code_scanner">Ler QR</Button>
      <Button variant="primary" size="lg" block iconLeft="add">Abrir mesa</Button></div>}>
    {att?<AlertBanner tone="warning" title={att+' mesas exigem ação agora'} actions={<Button size="sm" variant="secondary">Ver</Button>}>Mesa 03 com item pronto na janela · Mesa 08 pediu a conta.</AlertBanner>:null}
    <div style={{display:'flex',gap:10,alignItems:'center'}}>
      <SegmentedControl options={['Salão','Varanda','Balcão']} value={amb} onChange={setAmb}/>
      <span style={{marginLeft:'auto',font:'var(--type-caption)',color:'var(--text-muted)'}}>6 de 8 ocupadas</span></div>
    <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:10}}>
      {MESAS.map(m=><TableCard key={m.n} name={m.n} status={m.s} elapsed={m.t} guests={m.g} total={m.v} waiter={m.w} attention={m.att} onClick={()=>onMesa(m)}/>)}
    </div>
    <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:10}}>
      <StatTile label="Meu ticket médio" value="R$ 78" icon="receipt" delta="+6,2%" comparison="vs. turno anterior"/>
      <StatTile label="Mesas no turno" value="11" icon="table_restaurant" comparison="média 9"/></div>
  </Shell>;
}

function Mesa({mesa,onBack,onLancar}){
  const sub=COMANDA.reduce((s,i)=>s+i.preco,0);
  return <Shell title={mesa.n} sub={(mesa.g||0)+' pessoas · aberta há '+(mesa.t||'—')} onBack={onBack}
    right={<StatusPill status={mesa.s} size="lg"/>}
    footer={<div style={{display:'flex',gap:10}}>
      <Button variant="secondary" size="lg" block iconLeft="request_quote">Pedir conta</Button>
      <Button variant="primary" size="lg" block iconLeft="add" onClick={onLancar}>Lançar item</Button></div>}>
    <AlertBanner tone="success" title="1 item pronto na janela há 2 min" actions={<Button size="sm" variant="secondary" iconLeft="check">Entreguei</Button>}>
      Fritas com cheddar — comida esperando é qualidade perdida.</AlertBanner>
    <Card title="Comanda" subtitle={COMANDA.length+' itens'} padding="tight"
      actions={<IconButton icon="swap_horiz" label="Transferir item" size="sm"/>}>
      {COMANDA.map((i,x)=><OrderLine key={x} qty={i.qty} name={i.nome} modifiers={i.mods} note={i.obs} price={brl(i.preco)}
        status={<StatusPill status={i.status}/>} actions={i.status==='READY'?<Button size="sm" variant="accent" iconLeft="check">Entregar</Button>:null}/>)}
      <div style={{display:'flex',justifyContent:'space-between',paddingTop:12,marginTop:6,borderTop:'2px solid var(--border-default)',font:'var(--fw-bold) 18px/1.2 var(--font-sans)'}}>
        <span>Consumo</span><span className="nx-tnum">{brl(sub)}</span></div>
    </Card>
    <Card title="Tempos desta mesa" padding="tight">
      <div style={{display:'flex',gap:16,flexWrap:'wrap'}}>
        <div><div style={{font:'var(--type-overline)',textTransform:'uppercase',letterSpacing:'var(--ls-caps)',color:'var(--text-muted)'}}>Na fila</div><OrderTimer seconds={92} size="sm"/></div>
        <div><div style={{font:'var(--type-overline)',textTransform:'uppercase',letterSpacing:'var(--ls-caps)',color:'var(--text-muted)'}}>Produção</div><OrderTimer seconds={318} size="sm"/></div>
        <div><div style={{font:'var(--type-overline)',textTransform:'uppercase',letterSpacing:'var(--ls-caps)',color:'var(--text-muted)'}}>Na janela</div><OrderTimer seconds={132} warnAt={120} lateAt={240} size="sm"/></div>
      </div></Card>
  </Shell>;
}

function Lancamento({mesa,onBack}){
  const [sel,setSel]=React.useState([]);
  const total=sel.reduce((s,i)=>s+i.preco*i.qty,0);
  const add=p=>setSel(s=>{const i=s.findIndex(x=>x.nome===p.nome);return i>=0?s.map((x,j)=>j===i?{...x,qty:x.qty+1}:x):[...s,{...p,qty:1}];});
  return <Shell title={'Lançar · '+mesa.n} sub="Favoritos = 8 itens mais vendidos" onBack={onBack}
    footer={<Button variant="primary" size="touch" block iconLeft="send" disabled={!sel.length} onClick={onBack}>
      Enviar {sel.length?'· '+brl(total):''}</Button>}>
    <Input size="lg" icon="search" placeholder="Buscar produto ou código"/>
    <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:10}}>
      {FAVORITOS.map(p=><button key={p.nome} onClick={()=>add(p)} style={{minHeight:72,textAlign:'left',padding:'12px 14px',borderRadius:'var(--brand-radius)',
        border:'1px solid var(--border-subtle)',background:'var(--surface-card)',cursor:'pointer',boxShadow:'var(--shadow-subtle)'}}>
        <div style={{font:'var(--fw-semibold) 15px/1.25 var(--font-sans)',color:'var(--text-primary)'}}>{p.nome}</div>
        <div style={{font:'var(--type-numeric)',color:'var(--text-secondary)',marginTop:4}}>{brl(p.preco)}</div></button>)}
    </div>
    {sel.length?<Card title="A enviar" padding="tight">{sel.map((i,x)=><OrderLine key={x} qty={i.qty} name={i.nome} price={brl(i.preco*i.qty)}
      actions={<QuantityStepper size="sm" value={i.qty} onChange={v=>setSel(s=>v<=0?s.filter((_,j)=>j!==x):s.map((y,j)=>j===x?{...y,qty:v}:y))}/>}/>)}</Card>
      :<EmptyState icon="touch_app" title="Toque num favorito">Dois toques por item — sem digitação em ambiente de pressão.</EmptyState>}
  </Shell>;
}

function GarcomApp(){
  const [tela,setTela]=React.useState('login');
  const [mesa,setMesa]=React.useState(null);
  if(tela==='login')return <Login onEnter={()=>setTela('mapa')}/>;
  if(tela==='mesa')return <Mesa mesa={mesa} onBack={()=>setTela('mapa')} onLancar={()=>setTela('lancar')}/>;
  if(tela==='lancar')return <Lancamento mesa={mesa} onBack={()=>setTela('mesa')}/>;
  return <Mapa onMesa={m=>{setMesa(m);setTela(m.s==='FREE'?'mapa':'mesa');}}/>;
}
window.GarcomApp=GarcomApp;
