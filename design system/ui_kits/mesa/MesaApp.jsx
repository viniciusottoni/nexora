const {Button,IconButton,Badge,Icon,Card,Checkbox,QuantityStepper,SegmentedControl,StatusPill,OrderTimer,MenuItemCard,OrderLine,SyncStatus,BrandMark,AlertBanner,ProgressMeter,EmptyState}=window.NexoraDesignSystem_aa692a;

function Chrome({children,footer,title,sub,onBack,right}){
  return <div style={{display:'flex',flexDirection:'column',height:'100%',background:'var(--brand-surface)'}}>
    <header style={{flex:'0 0 auto',background:'var(--brand-primary)',color:'var(--brand-on-primary)',padding:'12px 16px 14px',display:'flex',alignItems:'center',gap:12}}>
      {onBack?<button onClick={onBack} aria-label="Voltar" style={{border:0,background:'rgba(255,255,255,.16)',color:'#fff',width:36,height:36,borderRadius:10,display:'flex',alignItems:'center',justifyContent:'center',cursor:'pointer'}}><Icon name="arrow_back" size={20}/></button>:null}
      <div style={{minWidth:0,flex:'1 1 auto'}}>
        <div style={{font:'var(--fw-bold) 18px/1.2 var(--font-display)'}}>{title}</div>
        <div style={{font:'var(--type-caption)',color:'rgba(255,255,255,.78)',marginTop:2}}>{sub}</div>
      </div>{right}
    </header>
    <main style={{flex:'1 1 auto',overflowY:'auto',padding:'16px',display:'flex',flexDirection:'column',gap:12}}>{children}</main>
    {footer?<footer style={{flex:'0 0 auto',padding:'12px 16px 18px',background:'var(--surface-card)',borderTop:'1px solid var(--border-subtle)',boxShadow:'0 -6px 20px rgba(16,28,46,.06)'}}>{footer}</footer>:null}
  </div>;
}

function Cardapio({cat,setCat,onOpen,cart,onCart}){
  const itens=PRODUTOS.filter(p=>p.cat===cat);
  const total=cart.reduce((s,i)=>s+i.preco*i.qty,0);
  return <Chrome title="Dona Betinha" sub="Mesa 07 · 4 pessoas" right={<SyncStatus state="local" queued={3}/>}
    footer={cart.length?<Button variant="primary" size="touch" block iconLeft="shopping_cart" onClick={onCart}>Ver pedido · {brl(total)}</Button>
      :<div style={{display:'flex',gap:10}}><Button variant="secondary" size="lg" block iconLeft="notifications_active">Chamar garçom</Button></div>}>
    <div style={{display:'flex',flex:'0 0 auto',gap:8,overflowX:'auto',margin:'0 -16px',padding:'0 16px 4px'}}>
      {CATEGORIAS.map(c=><button key={c} onClick={()=>setCat(c)} style={{flex:'0 0 auto',height:38,padding:'0 16px',borderRadius:999,cursor:'pointer',
        border:'1px solid '+(c===cat?'var(--brand-primary)':'var(--border-default)'),background:c===cat?'var(--brand-primary)':'var(--surface-card)',
        color:c===cat?'var(--brand-on-primary)':'var(--text-secondary)',font:'var(--fw-semibold) 14px/1 var(--font-sans)',whiteSpace:'nowrap'}}>{c}</button>)}
    </div>
    <div style={{display:'flex',alignItems:'center',gap:8,font:'var(--type-caption)',color:'var(--text-muted)'}}>
      <Icon name="schedule" size={16}/> Fila da cozinha agora: <strong style={{color:'var(--text-primary)'}}>~14 min</strong> · prazo calculado pela fila
    </div>
    {itens.map(p=><MenuItemCard key={p.id} name={p.nome} description={p.desc} price={brl(p.preco)} prepMinutes={p.prep}
      unavailable={p.esgotado} badge={p.tag?<Badge tone="accent" size="sm">{p.tag}</Badge>:null} onClick={()=>onOpen(p)}/>)}
  </Chrome>;
}

function Produto({produto,onBack,onAdd}){
  const [qty,setQty]=React.useState(1);
  const [meio,setMeio]=React.useState(false);
  const [borda,setBorda]=React.useState(true);
  const extra=(borda?8:0)+(meio?4:0);
  return <Chrome title={produto.nome} sub={produto.prep+' min de preparo'} onBack={onBack}
    footer={<div style={{display:'flex',gap:12,alignItems:'center'}}>
      <QuantityStepper value={qty} onChange={setQty}/>
      <Button variant="primary" size="touch" block iconLeft="add_shopping_cart"
        onClick={()=>onAdd({...produto,qty,mods:[meio&&'meio a meio · Mussarela',borda&&'borda catupiry'].filter(Boolean).join(' · '),preco:produto.preco+extra})}>
        Adicionar · {brl((produto.preco+extra)*qty)}</Button></div>}>
    <div style={{height:168,borderRadius:'var(--brand-radius)',background:'var(--surface-sunken)',display:'flex',alignItems:'center',justifyContent:'center',color:'var(--text-disabled)',flexDirection:'column',gap:6}}>
      <Icon name="add_photo_alternate" size={34}/><span style={{font:'var(--type-caption)'}}>foto do produto — a fornecer pelo estabelecimento</span></div>
    <p style={{font:'var(--type-body-lg)',color:'var(--text-secondary)',margin:0}}>{produto.desc}</p>
    <Card title="Meio a meio" subtitle="Preço da fração de maior valor (RN-009)" padding="tight">
      <Checkbox label="Dividir em dois sabores" checked={meio} onChange={e=>setMeio(e.target.checked)}/>
      {meio?<div style={{marginTop:8,paddingTop:8,borderTop:'1px solid var(--border-subtle)'}}>
        {PRODUTOS.filter(p=>p.cat==='Pizzas salgadas'&&p.id!==produto.id&&!p.esgotado).map(p=>
          <Checkbox key={p.id} type="radio" name="metade" label={'2ª metade · '+p.nome} price={brl(p.preco)} defaultChecked={p.id==='p2'}/>)}
      </div>:null}
    </Card>
    {MODIFICADORES.map(g=><Card key={g.grupo} title={g.grupo} padding="tight">
      {g.opcoes.map(o=><Checkbox key={o.n} type={g.tipo==='radio'?'radio':'checkbox'} name={g.grupo}
        label={o.n} price={o.p?'+ '+brl(o.p):null}
        checked={g.grupo==='Borda'&&o.n==='Catupiry'?borda:undefined}
        defaultChecked={g.tipo==='radio'&&o.n==='Tradicional'?true:undefined}
        onChange={g.grupo==='Borda'&&o.n==='Catupiry'?e=>setBorda(e.target.checked):undefined}/>)}
    </Card>)}
    <Card title="Observação" padding="tight">
      <textarea placeholder="Ex.: massa bem assada, sem cebola" rows={2}
        style={{width:'100%',border:'1px solid var(--border-default)',borderRadius:'var(--radius-md)',padding:'10px 12px',font:'var(--type-body)',resize:'none',outline:'none'}}/>
    </Card>
  </Chrome>;
}

function Pedido({cart,onBack,onSend,onQty}){
  const total=cart.reduce((s,i)=>s+i.preco*i.qty,0);
  return <Chrome title="Seu pedido" sub={cart.length+' itens · Mesa 07'} onBack={onBack}
    footer={<Button variant="primary" size="touch" block iconLeft="send" onClick={onSend}>Enviar para a cozinha · {brl(total)}</Button>}>
    <Card padding="tight">{cart.map((i,x)=><OrderLine key={x} qty={i.qty} name={i.nome} modifiers={i.mods} price={brl(i.preco*i.qty)}
      actions={<QuantityStepper size="sm" value={i.qty} onChange={v=>onQty(x,v)}/>}/>)}</Card>
    <Card title="Sugestão para acompanhar" subtitle="Baseada no que está no pedido" padding="tight">
      <MenuItemCard name="Refrigerante lata 350ml" price={brl(7)} prepMinutes={1}/>
    </Card>
    <div style={{display:'flex',justifyContent:'space-between',font:'var(--type-body-lg)',padding:'0 4px'}}>
      <span style={{color:'var(--text-secondary)'}}>Subtotal</span><strong className="nx-tnum">{brl(total)}</strong></div>
    <div style={{display:'flex',justifyContent:'space-between',font:'var(--type-caption)',color:'var(--text-muted)',padding:'0 4px'}}>
      <span>Taxa de serviço 10% — opcional, aplicada no fechamento</span><span className="nx-tnum">{brl(total*.1)}</span></div>
  </Chrome>;
}

function Acompanhar({onConsumo}){
  const etapas=[['Recebido','check','done'],['Em produção','restaurant','done'],['No forno','local_fire_department','now'],['Pronto','room_service',''],['Na mesa','table_restaurant','']];
  return <Chrome title="Acompanhar" sub="Mesa 07 · pedido #42" right={<SyncStatus state="local" queued={1}/>}
    footer={<div style={{display:'flex',gap:10}}>
      <Button variant="secondary" size="lg" block iconLeft="notifications_active">Chamar garçom</Button>
      <Button variant="primary" size="lg" block iconLeft="receipt_long" onClick={onConsumo}>Ver consumo</Button></div>}>
    <Card padding="tight" style={{alignItems:'center',textAlign:'center',gap:6,padding:'20px 16px'}}>
      <div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)'}}>No forno agora</div>
      <OrderTimer seconds={318} warnAt={600} lateAt={900} size="lg" showIcon/>
      <div style={{font:'var(--type-caption)',color:'var(--text-muted)'}}>previsão de saída às 20:54 · recalculada pela fila</div>
      <div style={{width:'100%',marginTop:10}}><ProgressMeter value={318} max={720} tone="accent"/></div>
    </Card>
    <Card title="Etapas" padding="tight">
      <div style={{display:'flex',flexDirection:'column',gap:2}}>
        {etapas.map(([l,ic,st])=><div key={l} style={{display:'flex',alignItems:'center',gap:12,padding:'10px 0',borderBottom:'1px solid var(--border-subtle)'}}>
          <span style={{width:32,height:32,borderRadius:999,display:'flex',alignItems:'center',justifyContent:'center',flex:'0 0 auto',
            background:st==='done'?'var(--nx-success-100)':st==='now'?'var(--nx-warning-100)':'var(--surface-sunken)',
            color:st==='done'?'var(--nx-success-600)':st==='now'?'var(--nx-warning-600)':'var(--text-disabled)'}}><Icon name={ic} size={18} fill={st!==''}/></span>
          <span style={{font:st==='now'?'var(--fw-semibold) 16px/1.3 var(--font-sans)':'var(--type-body-lg)',color:st===''?'var(--text-disabled)':'var(--text-primary)'}}>{l}</span>
          {st==='now'?<span style={{marginLeft:'auto'}}><StatusPill status="IN_OVEN" live/></span>:st==='done'?<span style={{marginLeft:'auto',font:'var(--type-caption)',color:'var(--text-muted)'}}>20:41</span>:null}
        </div>)}
      </div>
    </Card>
  </Chrome>;
}

function Consumo({onBack}){
  const sub=CONSUMO.reduce((s,i)=>s+i.preco,0);
  const [taxa,setTaxa]=React.useState(true);
  const [pedindo,setPedindo]=React.useState(false);
  return <Chrome title="Consumo da mesa" sub="Mesa 07 · aberta às 20:12" onBack={onBack}
    footer={pedindo?<Button variant="secondary" size="touch" block iconLeft="hourglass_top" disabled>Conta solicitada — o caixa foi avisado</Button>
      :<Button variant="primary" size="touch" block iconLeft="request_quote" onClick={()=>setPedindo(true)}>Pedir a conta · {brl(sub+(taxa?sub*.1:0))}</Button>}>
    {pedindo?<AlertBanner tone="success" title="Conta solicitada">O caixa e o garçom Jonas foram avisados. Forma de pagamento pode ser escolhida na mesa.</AlertBanner>:null}
    <Card padding="tight">{CONSUMO.map((i,x)=><OrderLine key={x} qty={i.qty} name={i.nome} modifiers={i.mods} note={i.obs}
      price={brl(i.preco)} status={<StatusPill status={i.status}/>}/>)}</Card>
    <Card padding="tight">
      <div style={{display:'flex',justifyContent:'space-between',font:'var(--type-body)',padding:'4px 0'}}><span style={{color:'var(--text-secondary)'}}>Subtotal</span><span className="nx-tnum">{brl(sub)}</span></div>
      <div style={{display:'flex',justifyContent:'space-between',alignItems:'center',padding:'4px 0'}}>
        <Checkbox label="Taxa de serviço 10%" compact checked={taxa} onChange={e=>setTaxa(e.target.checked)}/>
        <span className="nx-tnum" style={{font:'var(--type-numeric)'}}>{brl(sub*.1)}</span></div>
      <div style={{display:'flex',justifyContent:'space-between',paddingTop:10,marginTop:6,borderTop:'2px solid var(--border-default)',font:'var(--fw-bold) 20px/1.2 var(--font-sans)'}}>
        <span>Total</span><span className="nx-tnum">{brl(sub+(taxa?sub*.1:0))}</span></div>
    </Card>
    <Card title="Dividir a conta" subtitle="Calculado pelo sistema (RF-SAL-10)" padding="tight">
      <SegmentedControl block size="lg" options={[{value:'p',label:'Por pessoa'},{value:'i',label:'Por item'},{value:'v',label:'Valor'}]} value="p" onChange={()=>{}}/>
      <div style={{marginTop:12,display:'flex',justifyContent:'space-between',font:'var(--type-body)'}}>
        <span style={{color:'var(--text-secondary)'}}>4 pessoas · cada uma</span><strong className="nx-tnum">{brl((sub+(taxa?sub*.1:0))/4)}</strong></div>
    </Card>
  </Chrome>;
}

function MesaApp(){
  const [tela,setTela]=React.useState('cardapio');
  const [cat,setCat]=React.useState('Pizzas salgadas');
  const [produto,setProduto]=React.useState(null);
  const [cart,setCart]=React.useState([]);
  const add=i=>{setCart(c=>[...c,i]);setTela('pedido');};
  const qty=(x,v)=>setCart(c=>v<=0?c.filter((_,j)=>j!==x):c.map((i,j)=>j===x?{...i,qty:v}:i));
  if(tela==='produto')return <Produto produto={produto} onBack={()=>setTela('cardapio')} onAdd={add}/>;
  if(tela==='pedido')return <Pedido cart={cart} onBack={()=>setTela('cardapio')} onQty={qty} onSend={()=>{setCart([]);setTela('acompanhar');}}/>;
  if(tela==='acompanhar')return <Acompanhar onConsumo={()=>setTela('consumo')}/>;
  if(tela==='consumo')return <Consumo onBack={()=>setTela('acompanhar')}/>;
  return <Cardapio cat={cat} setCat={setCat} cart={cart} onCart={()=>setTela('pedido')} onOpen={p=>{setProduto(p);setTela('produto');}}/>;
}
window.MesaApp=MesaApp;
