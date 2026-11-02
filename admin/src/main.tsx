import { StrictMode, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './style.css';

type Status = 'open' | 'in_progress' | 'resolved';
type Issue = { id: string; title: string; description: string; priority: 'low' | 'medium' | 'high'; status: Status; position: {x:number;y:number;z:number}; createdAt: string; updatedAt: string };
const base = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080').replace(/\/$/, '');
const statusNames: Record<Status,string> = {open:'待处理',in_progress:'处理中',resolved:'已解决'};
const priorityNames = {low:'低',medium:'中',high:'高'};
async function request<T>(path:string, init?:RequestInit):Promise<T> {
  const controller = new AbortController();
  const timer = setTimeout(()=>controller.abort(),10000);
  try {
    const response = await fetch(base + path, {...init, signal:controller.signal});
    const payload = await response.json().catch(()=>null);
    if (!response.ok) throw new Error(payload?.error || `HTTP ${response.status}`);
    if (payload === null) throw new Error('后端返回的内容不是有效 JSON');
    return payload as T;
  } catch (e) {
    if (e instanceof DOMException && e.name === 'AbortError') throw new Error('请求超时，请检查后端服务');
    if (e instanceof TypeError) throw new Error('无法连接后端，请检查服务、地址和跨域配置');
    throw e;
  } finally {clearTimeout(timer);}
}
function App(){
  const [issues,setIssues]=useState<Issue[]>([]);
  const [loading,setLoading]=useState(true);
  const [error,setError]=useState('');
  const [notice,setNotice]=useState('');
  const [saving,setSaving]=useState<string|null>(null);
  const [filter,setFilter]=useState('all');
  async function reload(){setLoading(true);setError('');try{const data=await request<Issue[]>('/api/issues');if(!Array.isArray(data))throw new Error('问题列表格式错误');setIssues(data);}catch(e){setError((e as Error).message);}finally{setLoading(false);}}
  useEffect(()=>{void reload();},[]);
  async function update(id:string,status:Status){
    setSaving(id);setError('');setNotice('');
    try{const item=await request<Issue>(`/api/issues/${encodeURIComponent(id)}/status`,{method:'PATCH',headers:{'Content-Type':'application/json'},body:JSON.stringify({status})});setIssues(items=>items.map(i=>i.id===id?item:i));setNotice('处理状态已保存');}
    catch(e){setError((e as Error).message + '；状态未确认，请刷新核对');}finally{setSaving(null);}
  }
  const visible=issues.filter(i=>filter==='all'||i.status===filter).slice().reverse();
  return <main>
    <header><div><p className="eyebrow">PARK / FIELD OPERATIONS</p><h1>园区巡检</h1><p className="muted">现场发现，及时跟进。统一管理 AR 巡检上报的问题。</p></div><button disabled={loading||!!saving} onClick={()=>void reload()}>{loading?'加载中…':'刷新列表'}</button></header>
    <section className="stats" aria-label="问题概况">{(['open','in_progress','resolved'] as Status[]).map(s=><div key={s}><span>{statusNames[s]}</span><strong>{issues.filter(i=>i.status===s).length}</strong></div>)}</section>
    {error&&<div role="alert" className="error">{error} <button disabled={loading||!!saving} onClick={()=>void reload()}>重试</button></div>}
    {notice&&<p role="status" className="success">{notice}</p>}
    <section className="board"><div className="toolbar"><h2>问题列表 <small>{visible.length}</small></h2><label>状态筛选 <select value={filter} onChange={e=>setFilter(e.target.value)}><option value="all">全部状态</option>{Object.entries(statusNames).map(([s,n])=><option key={s} value={s}>{n}</option>)}</select></label></div>
    {loading?<p className="empty" role="status">正在获取问题…</p>:visible.length===0?<p className="empty">{error?'暂时无法获取问题，请重试。':'暂无问题。使用手机放置 AR 标记并上报后，点击刷新。'}</p>:<div className="table-wrap"><table><thead><tr><th>问题</th><th>优先级</th><th>处理状态</th><th>现场坐标 / 米</th><th>上报时间</th></tr></thead><tbody>{visible.map(i=><tr key={i.id}><td><strong>{i.title}</strong><p className="description">{i.description||'未填写描述'}</p><small className="muted">{i.id.slice(0,8)}</small></td><td><span className={`badge ${i.priority}`}>{priorityNames[i.priority]}</span></td><td><select aria-label={`${i.title}的处理状态`} disabled={!!saving||loading} value={i.status} onChange={e=>void update(i.id,e.target.value as Status)}>{Object.entries(statusNames).map(([s,n])=><option key={s} value={s}>{n}</option>)}</select>{saving===i.id&&<small> 保存中…</small>}</td><td className="coords">{[i.position.x,i.position.y,i.position.z].map(n=>n.toFixed(2)).join(' / ')}</td><td>{new Date(i.createdAt).toLocaleString('zh-CN')}</td></tr>)}</tbody></table></div>}
    </section><footer>坐标属于上报时的 AR 会话，不代表园区统一地图位置。列表需手动刷新。</footer>
  </main>;
}
createRoot(document.getElementById('root')!).render(<StrictMode><App/></StrictMode>);
