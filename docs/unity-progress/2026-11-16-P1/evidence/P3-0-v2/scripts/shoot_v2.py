import json, time, urllib.request, datetime, subprocess, sys, os
sys.path.insert(0,'/workspace/p3-0/tools')
from playwright.sync_api import sync_playwright
EV='/workspace/p3-0-v2/evidence/'; BASE='http://127.0.0.1:8911'; REPO='/workspace/p3-0-v2/repo'
MOCK=None
RESTART_TRIES=[]
def start_mock():
    global MOCK
    t0=time.time()
    while time.time()-t0<120:
        MOCK=subprocess.Popen(['python3','tools/mock_body_api.py','--port','8911'],cwd=REPO,stdout=open('/workspace/p3-0-v2/mock.log','a'),stderr=subprocess.STDOUT)
        for _ in range(30):
            if MOCK.poll() is not None: break
            try: urllib.request.urlopen(BASE+'/health',timeout=0.5); RESTART_TRIES.append(round(time.time()-t0,1)); return
            except Exception: time.sleep(0.1)
        time.sleep(1)
    raise SystemExit('mock restart failed')
def stop_mock():
    MOCK.terminate(); MOCK.wait()
def mode(m): return json.loads(urllib.request.urlopen(BASE+'/mock/mode?to='+m).read())
SEL={'1':'.side','2':'.tabs','3':'.search','4':'.card.a','5':'.card.b','6':'.card.c','7':'.card.d','8':'.dock'}
JS_BOX='''(a)=>{const [root,sel]=a;const R=document.querySelector(root).getBoundingClientRect();const o={};
for(const [k,s] of Object.entries(sel)){const e=document.querySelector(root+' '+s);if(!e){o[k]=null;continue}const r=e.getBoundingClientRect();
o[k]={x:r.left-R.left,y:r.top-R.top,w:r.width,h:r.height}};o._root={w:R.width,h:R.height};return o}'''
JS_ST='''()=>{const q=s=>document.querySelector(s);const t=s=>q(s)?q(s).innerText.trim():null;
let minfs=99,minel='';document.querySelectorAll('body *').forEach(e=>{if(!e.offsetParent)return;const own=[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim());if(!own)return;const f=parseFloat(getComputedStyle(e).fontSize);if(f<minfs){minfs=f;minel=e.className+':'+e.textContent.trim().slice(0,10)}});
const clip=[];document.querySelectorAll('#tl .r,.say').forEach(e=>{const r=e.getBoundingClientRect(),k=e.closest('.tl,.saybox').getBoundingClientRect();if(r.bottom>k.bottom+0.5)clip.push(e.innerText.slice(0,12))});
return {theme:document.documentElement.dataset.theme,dot_bad:q('#dot').classList.contains('bad'),conn_shown:getComputedStyle(q('#conn')).display!=='none',conn_text:t('#conn'),
conn_color:getComputedStyle(q('#conn')).color,day_head:t('.card.a h4'),day_num:t('.card.a .d'),tl_rows:[...document.querySelectorAll('#tl .r')].map(r=>r.className+'|'+r.innerText.replace(/\\n/g,' ')),tl:t('#tl'),
foot:t('#foot'),mood:t('#mood'),say_items:[...document.querySelectorAll('#say .say')].map(e=>e.innerText),say:t('#say'),ph:[...document.querySelectorAll('.ph')].map(e=>e.closest('.card').className),
min_font_px:minfs,min_font_el:minel,clipped:clip,search_top:q('.search').getBoundingClientRect().top,tabs_top:q('.tabs').getBoundingClientRect().top,
localStorage_len:localStorage.length,doc_w:document.documentElement.scrollWidth,doc_h:document.documentElement.scrollHeight}}'''
log={}
def newpage(b,vp,dpr=1):
    c=b.new_context(viewport={'width':vp[0],'height':vp[1]},device_scale_factor=dpr); pg=c.new_page()
    ent={'viewport':list(vp)+[dpr],'console':[],'pageerror':[],'failed':[],'http_err':[],'non_get':[],'requests':[]}
    pg.on('console',lambda m: ent['console'].append(f'{m.type}: {m.text}') if m.type in('error','warning') else None)
    pg.on('pageerror',lambda e: ent['pageerror'].append(str(e)))
    pg.on('requestfailed',lambda r: ent['failed'].append(f"{r.method} {r.url.replace(BASE,'')} {r.failure}"))
    pg.on('response',lambda r: ent['http_err'].append(f"{r.request.method} {r.url.replace(BASE,'')} -> {r.status}") if r.status>=400 else None)
    def onreq(r):
        ent['requests'].append(f"{r.method} {r.url.replace(BASE,'')} X-Token={'set' if r.headers.get('x-token') else 'none'}")
        if r.method!='GET': ent['non_get'].append(r.method+' '+r.url)
    pg.on('request',onreq)
    return c,pg,ent
def fin(name,c,pg,ent,shot=True):
    if shot: pg.screenshot(path=EV+name+'.png')
    ent['state']=pg.evaluate(JS_ST); ent['boxes']=pg.evaluate(JS_BOX,['.wrap',SEL]); ent['time']=datetime.datetime.now().strftime('%H:%M:%S')
    for k in ('requests','console','http_err','failed'): ent[k+'_uniq']=sorted(set(ent[k]))
    ent['requests']=sorted(set(ent['requests'])); log[name]=ent; c.close()
def simple(b,name,vp,m,theme='pink',tok='mock',dpr=1):
    mode(m); c,pg,ent=newpage(b,vp,dpr); ent['mode']=m; ent['theme']=theme
    pg.goto(f'{BASE}/p/bench#t={tok}&theme={theme}'); ent['url']=f'/p/bench#t=<{"mock" if tok=="mock" else "wrong"}>&theme={theme}'
    pg.wait_for_timeout(1800); fin(name,c,pg,ent)
def watch(pg,cond,timeout=12):
    t0=time.time()
    while time.time()-t0<timeout:
        if pg.evaluate("()=>getComputedStyle(document.querySelector('#conn')).display!=='none'")==cond: return round(time.time()-t0,2)
        time.sleep(0.1)
    return None
def disconnect_cycle(b,kind):
    mode('data'); c,pg,ent=newpage(b,(1640,680)); ent['mode']='data→'+kind+'→data'
    pg.goto(f'{BASE}/p/bench#t=mock&theme=pink'); pg.wait_for_timeout(1800)
    ent['before']=pg.evaluate(JS_ST)
    t=datetime.datetime.now().strftime('%H:%M:%S.%f')[:-3]
    if kind=='503': mode('offline')
    else: stop_mock()
    ent['t_disconnect']=t; ent['sec_until_conn_shown']=watch(pg,True)
    pg.wait_for_timeout(500); pg.screenshot(path=EV+f'P3-0v2-data-then-{kind}-disconnected-1640x680.png')
    ent['during']=pg.evaluate(JS_ST)
    t=datetime.datetime.now().strftime('%H:%M:%S.%f')[:-3]
    if kind=='503': mode('data')
    else: start_mock(); t=datetime.datetime.now().strftime('%H:%M:%S.%f')[:-3]; ent['restart_wait_s']=RESTART_TRIES[-1]
    ent['t_restore']=t; ent['sec_until_conn_hidden']=watch(pg,False)
    pg.wait_for_timeout(500)
    fin(f'P3-0v2-data-then-{kind}-restored-1640x680',c,pg,ent)
start_mock()
try:
  with sync_playwright() as p:
    b=p.chromium.launch(); ver=b.version
    for th in ['pink','blue','violet']: simple(b,f'P3-0v2-page-{th}-1640x680',(1640,680),'data',th)
    simple(b,'P3-0v2-page-pink-820x340@2x',(820,340),'data',dpr=2)
    simple(b,'P3-0v2-small-900x560',(900,560),'data')
    simple(b,'P3-0v2-empty-1640x680',(1640,680),'empty')
    simple(b,'P3-0v2-offline-firstload-1640x680',(1640,680),'offline')
    simple(b,'P3-0v2-401-1640x680',(1640,680),'data',tok='wrongtoken')
    disconnect_cycle(b,'503'); disconnect_cycle(b,'netdown')
    mode('data')
    # fixture: what mock serves (same calls page makes)
    def g(u):
        r=urllib.request.Request(BASE+u,headers={'X-Token':'mock'}); return json.loads(urllib.request.urlopen(r).read())
    boot=g('/api/body/feed'); win=g('/api/body/feed?since=%d&limit=50'%max(0,boot['last_id']-50)); full=g('/api/body/feed?since=0&limit=50')
    log['_fixture']={'boot_last_id':boot['last_id'],'boot_items':len(boot['items']),
      'window_ids':[win['items'][0]['id'],win['items'][-1]['id']] if win['items'] else [],
      'window_said':[(x['id'],x['text']) for x in win['items'] if x['kind']=='said' and x['text']],
      'since0_said':[(x['id'],x['text']) for x in full['items'] if x['kind']=='said' and x['text']]}
    log['_browser']=ver
    b.close()
finally:
    try: stop_mock()
    except Exception: pass
json.dump(log,open('/workspace/p3-0-v2/shots.json','w'),ensure_ascii=False,indent=1); print('done')
