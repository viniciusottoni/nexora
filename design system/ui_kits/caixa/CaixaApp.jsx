const {Button,IconButton,Badge,Icon,Card,Input,Field,Select,Checkbox,SideNav,TopBar,SegmentedControl,StatusPill,OrderTimer,TableCard,OrderLine,SyncStatus,BrandMark,AlertBanner,DataTable,StatTile,NumericKeypad}=window.NexoraDesignSystem_aa692a;

function Conta({mesa,onPagar}){
  const sub=CONTA.filter(i=>!i.cancel).reduce((s,i)=>s+i.preco,0);
  const [taxa,setTaxa]=React.useState(true);
  const total=sub+(taxa?sub*.1:0);
  return <Card title={'Conta · '+mesa.n} subtitle={mesa.g+' pessoas · '+mesa.t+' · garçom '+mesa.w}
    actions={<><StatusPill status={mesa.s} live={mesa.att}/><IconButton icon="print" label="Imprimir" size="sm"/></>}
    footer={<><Button variant="secondary" iconLeft="call_split">Dividir</Button>
      <Button variant="secondary" iconLeft="percent">Desconto</Button>
      <Button variant="primary" iconLeft="point_of_sale" onClick={onPagar}>Receber {brl(total)}</Button></>}>
    <div style={{maxHeight:300,overflowY:'auto'}}>
      {CONTA.map((i,x)=><OrderLine key={x} qty={i.qty} name={i.nome} modifiers={i.mods} note={i.obs} price={brl(i.preco)}
        cancelled={i.cancel} status={<StatusPill status={i.status}/>}/>)}
    </div>
    <div style={{marginTop:14,paddingTop:14,borderTop:'1px solid var(--border-subtle)',display:'flex',flexDirection:'column',gap:8}}>
      <div style={{display:'flex',justifyContent:'space-between',font:'var(--type-body)'}}><span style={{color:'var(--text-secondary)'}}>Subtotal</span><span className="nx-tnum">{brl(sub)}</span></div>
      <div style={{display:'flex',justifyContent:'space-between',alignItems:'center'}}>
        <Checkbox compact label="Taxa de serviço 10%" checked={taxa} onChange={e=>setTaxa(e.target.checked)}/>
        <span className="nx-tnum" style={{font:'var(--type-numeric)'}}>{brl(sub*.1)}</span></div>
      <div style={{display:'flex',justifyContent:'space-between',paddingTop:10,borderTop:'2px solid var(--border-default)',font:'var(--fw-bold) 24px/1.1 var(--font-sans)'}}>
        <span>Total</span><span className="nx-tnum">{brl(total)}</span></div>
    </div>
  </Card>;
}

function Pagamento({mesa,onVoltar}){
  const sub=CONTA.filter(i=>!i.cancel).reduce((s,i)=>s+i.preco,0),total=sub*1.1;
  const [pagos,setPagos]=React.useState([{f:'Débito',v:100}]);
  const [valor,setValor]=React.useState('');
  const pago=pagos.reduce((s,p)=>s+p.v,0);
  const falta=Math.max(0,total-pago);
  return <div style={{display:'grid',gridTemplateColumns:'1fr 380px',gap:20,alignItems:'start'}}>
    <Card title={'Recebimento · '+mesa.n} subtitle="Múltiplas formas na mesma conta (RF-CXA-03)"
      footer={<><Button variant="ghost" onClick={onVoltar}>Voltar</Button>
        <Button variant="accent" iconLeft="check_circle" disabled={falta>0} onClick={onVoltar}>Fechar conta</Button></>}>
      <div style={{display:'grid',gridTemplateColumns:'repeat(5,1fr)',gap:10}}>
        {FORMAS.map(([n,ic])=><button key={n} onClick={()=>{const v=parseFloat(valor.replace(',','.'))||falta;setPagos(p=>[...p,{f:n,v}]);setValor('');}}
          style={{minHeight:80,display:'flex',flexDirection:'column',alignItems:'center',justifyContent:'center',gap:6,borderRadius:'var(--brand-radius)',
          border:'1px solid var(--border-default)',background:'var(--surface-card)',cursor:'pointer',boxShadow:'var(--shadow-subtle)'}}>
          <Icon name={ic} size={24} color="var(--nx-navy-700)"/>
          <span style={{font:'var(--fw-semibold) 12px/1.2 var(--font-sans)',textAlign:'center'}}>{n}</span></button>)}
      </div>
      <div style={{marginTop:18,display:'flex',flexDirection:'column',gap:10}}>
        {pagos.map((p,i)=><div key={i} style={{display:'flex',alignItems:'center',gap:10,padding:'10px 12px',borderRadius:'var(--radius-md)',background:'var(--surface-sunken)'}}>
          <Icon name="check_circle" size={18} color="var(--nx-success-500)"/>
          <span style={{font:'var(--type-body)'}}>{p.f}</span>
          <span className="nx-tnum" style={{marginLeft:'auto',font:'var(--type-numeric)'}}>{brl(p.v)}</span>
          <IconButton icon="close" label="Remover" size="sm" onClick={()=>setPagos(x=>x.filter((_,j)=>j!==i))}/></div>)}
      </div>
      <div style={{marginTop:18,paddingTop:14,borderTop:'1px solid var(--border-subtle)',display:'flex',gap:24}}>
        <div><div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)'}}>Total</div>
          <div className="nx-tnum" style={{font:'var(--fw-bold) 22px/1.2 var(--font-mono)'}}>{brl(total)}</div></div>
        <div><div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)'}}>Recebido</div>
          <div className="nx-tnum" style={{font:'var(--fw-bold) 22px/1.2 var(--font-mono)',color:'var(--nx-success-600)'}}>{brl(pago)}</div></div>
        <div><div style={{font:'var(--type-overline)',letterSpacing:'var(--ls-caps)',textTransform:'uppercase',color:'var(--text-muted)'}}>Falta</div>
          <div className="nx-tnum" style={{font:'var(--fw-bold) 22px/1.2 var(--font-mono)',color:falta?'var(--nx-danger-600)':'var(--text-muted)'}}>{brl(falta)}</div></div>
      </div>
    </Card>
    <Card title="Valor" subtitle="Vazio = recebe o restante" padding="tight">
      <div style={{height:56,borderRadius:'var(--radius-md)',background:'var(--surface-sunken)',display:'flex',alignItems:'center',justifyContent:'flex-end',padding:'0 14px',
        font:'var(--fw-bold) 26px/1 var(--font-mono)',color:valor?'var(--text-primary)':'var(--text-disabled)',marginBottom:12}}>{valor||brl(falta)}</div>
      <NumericKeypad value={valor} onChange={setValor} onSubmit={()=>{}}/>
    </Card>
  </div>;
}

function Fechamento(){
  const cols=[{key:'f',header:'Forma'},{key:'sis',header:'Sistema',numeric:true},{key:'con',header:'Conferido',numeric:true},{key:'div',header:'Divergência',numeric:true,render:r=>
    <span style={{color:r.div==='—'?'var(--text-muted)':'var(--nx-danger-600)'}}>{r.div}</span>}];
  const rows=[{f:'Dinheiro',sis:'R$ 486,00',con:'R$ 474,00',div:'− R$ 12,00'},{f:'Débito',sis:'R$ 1.204,50',con:'R$ 1.204,50',div:'—'},
    {f:'Crédito',sis:'R$ 1.680,00',con:'R$ 1.680,00',div:'—'},{f:'PIX',sis:'R$ 812,40',con:'R$ 812,40',div:'—'}];
  return <div style={{display:'grid',gridTemplateColumns:'1fr 340px',gap:20,alignItems:'start'}}>
    <Card title="Fechamento de caixa" subtitle="Turno de 18:02 · operador Marcos" padding="none"
      footer={<><Button variant="secondary" iconLeft="download">Exportar</Button><Button variant="primary" iconLeft="lock">Fechar caixa</Button></>}>
      <DataTable columns={cols} rows={rows} footer={<tr><td>Total</td><td className="nxTb__num">R$ 4.182,90</td><td className="nxTb__num">R$ 4.170,90</td><td className="nxTb__num" style={{color:'var(--nx-danger-600)'}}>− R$ 12,00</td></tr>}/>
    </Card>
    <div style={{display:'flex',flexDirection:'column',gap:16}}>
      <AlertBanner tone="warning" title="Divergência de R$ 12,00 em dinheiro" actions={<Button size="sm" variant="secondary">Justificar</Button>}>
        Acima do limite configurado — exige justificativa e vai para a trilha de auditoria.</AlertBanner>
      <Card title="Movimentos" padding="tight">
        <div style={{display:'flex',flexDirection:'column',gap:10,font:'var(--type-body)'}}>
          <div style={{display:'flex',justifyContent:'space-between'}}><span style={{color:'var(--text-secondary)'}}>Abertura</span><span className="nx-tnum">R$ 200,00</span></div>
          <div style={{display:'flex',justifyContent:'space-between'}}><span style={{color:'var(--text-secondary)'}}>Suprimento 20:14</span><span className="nx-tnum">+ R$ 100,00</span></div>
          <div style={{display:'flex',justifyContent:'space-between'}}><span style={{color:'var(--text-secondary)'}}>Sangria 22:40</span><span className="nx-tnum">− R$ 800,00</span></div>
        </div></Card>
      <Card title="Taxa de cartão" subtitle="Despesa normalmente invisível" padding="tight">
        <div style={{display:'flex',justifyContent:'space-between',font:'var(--type-body)'}}><span style={{color:'var(--text-secondary)'}}>Débito 1,49% + Crédito 3,19%</span><span className="nx-tnum">R$ 71,55</span></div></Card>
    </div>
  </div>;
}

function CaixaApp(){
  const [view,setView]=React.useState('mesas');
  const [mesa,setMesa]=React.useState(MESAS_CX[3]);
  const abertas=MESAS_CX.filter(m=>m.s!=='PAID');
  const total=abertas.reduce((s,m)=>s+m.v,0);
  return <div style={{display:'flex',height:'100vh',background:'var(--surface-page)'}}>
    <SideNav brand={<BrandMark inverse size={22} subtitle="Caixa · Terminal 1"/>} activeId={view} onSelect={setView}
      items={[{group:'Operação'},{id:'mesas',label:'Mesas e comandas',icon:'table_restaurant',count:abertas.length},
        {id:'pagamento',label:'Recebimento',icon:'point_of_sale'},{id:'fechamento',label:'Fechamento de caixa',icon:'lock_clock'},
        {group:'Consulta'},{id:'hist',label:'Contas do turno',icon:'receipt_long'},{id:'aud',label:'Auditoria',icon:'history'}]}
      footer={<SyncStatus state="local" queued={12}/>}/>
    <div style={{flex:'1 1 auto',display:'flex',flexDirection:'column',minWidth:0}}>
      <TopBar title={view==='fechamento'?'Fechamento de caixa':view==='pagamento'?'Recebimento':'Mesas e comandas abertas'}
        subtitle="Dona Betinha · terça, 22:48 · turno aberto às 18:02"
        right={<><SegmentedControl options={['Salão','Delivery','Balcão']} value="Salão" onChange={()=>{}}/>
          <IconButton icon="notifications" label="Alertas" badge={2}/><SyncStatus state="local" queued={12}/></>}/>
      <div style={{flex:'1 1 auto',overflowY:'auto',padding:24}}>
        {view==='fechamento'?<Fechamento/>
        :view==='pagamento'?<Pagamento mesa={mesa} onVoltar={()=>setView('mesas')}/>
        :<div style={{display:'grid',gridTemplateColumns:'1fr 460px',gap:20,alignItems:'start'}}>
          <div style={{display:'flex',flexDirection:'column',gap:16}}>
            <div style={{display:'grid',gridTemplateColumns:'repeat(4,1fr)',gap:12}}>
              <StatTile label="Em aberto" value={brl(total)} icon="hourglass_top"/>
              <StatTile label="Recebido no turno" value="R$ 4.182" icon="payments" delta="+8,1%" comparison="vs. mesma terça"/>
              <StatTile label="Ticket médio" value="R$ 96" icon="receipt" comparison="média 89"/>
              <StatTile label="Contas fechadas" value="42" icon="task_alt"/></div>
            <Card title="Mesas abertas" subtitle="Valor e tempo de cada uma (RF-CXA-01)" padding="tight">
              <div style={{display:'grid',gridTemplateColumns:'repeat(auto-fill,minmax(196px,1fr))',gap:12}}>
                {MESAS_CX.map(m=><TableCard key={m.n} name={m.n} status={m.s} elapsed={m.t} guests={m.g} total={brl(m.v)} waiter={m.w}
                  attention={m.att} onClick={()=>setMesa(m)}/>)}
              </div></Card>
          </div>
          <Conta mesa={mesa} onPagar={()=>setView('pagamento')}/>
        </div>}
      </div>
    </div>
  </div>;
}
window.CaixaApp=CaixaApp;
