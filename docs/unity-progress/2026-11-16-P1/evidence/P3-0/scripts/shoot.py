import json, time, urllib.request, datetime
from playwright.sync_api import sync_playwright
EV='/workspace/p3-0/evidence/'
BASE='http://127.0.0.1:8911'
def mode(m): return json.loads(urllib.request.urlopen(BASE+'/mock/mode?to='+m).read())
SEL={'1':'.side','2':'.tabs','3':'.search','4':'.card.a','5':'.card.b','6':'.card.c','7':'.card.d','8':'.dock'}
JS_BOX='''(a)=>{const [root,sel]=a;const R=document.querySelector(root).getBoundingClientRect();const o={};
for(const [k,s] of Object.entries(sel)){const e=document.querySelector(root+' '+s);if(!e){o[k]=null;continue}const r=e.getBoundingClientRect();
o[k]={x:r.left-R.left,y:r.top-R.top,w:r.width,h:r.height}};o._root={w:R.width,h:R.height,x:R.left,y:R.top};return o}'''
JS_FACTS='''()=>{const q=s=>document.querySelector(s);const t=s=>q(s)?q(s).innerText.trim():null;
let minfs=99,minel='';document.querySelectorAll('body *').forEach(e=>{if(!e.offsetParent&&e.tagName!=='BODY')return;const own=[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim());if(!own)return;const f=parseFloat(getComputedStyle(e).fontSize);if(f<minfs){minfs=f;minel=e.tagName+'.'+e.className+':'+e.textContent.trim().slice(0,12)}});
const ov=[];document.querySelectorAll('.card,.head,.side,.dock,.tl .r,.say,.rw,.foot').forEach(e=>{if(e.scrollWidth>e.clientWidth+1)ov.push('W:'+e.className+':'+e.innerText.slice(0,10));});
const clipped=[];document.querySelectorAll('.tl .r,.say,.rw,.foot,.head .t,.tabs span').forEach(e=>{const r=e.getBoundingClientRect();const c=e.closest('.card,.head');if(!c)return;const cr=c.getBoundingClientRect();if(r.bottom>cr.bottom+0.5||r.right>cr.right+0.5)clipped.push(e.className+':'+e.innerText.slice(0,14)+' bottom='+Math.round(r.bottom)+'/'+Math.round(cr.bottom))});
return {theme:document.documentElement.dataset.theme, dot_bad:q('#dot').classList.contains('bad'), conn_shown:getComputedStyle(q('#conn')).display!=='none', conn_text:t('#conn'),
 day_head:t('.card.a h4'), day_num:t('.card.a .d'), tl:t('#tl'), tl_rows:[...document.querySelectorAll('#tl .r')].map(r=>r.className+'|'+r.innerText.replace(/\\n/g,' ')), foot:t('#foot'),foot_cls:q('#foot').className,
 mood:t('#mood'), say:t('#say'), ph:[...document.querySelectorAll('.ph')].map(e=>e.closest('.card').className+':'+e.innerText),
 side_titles:[...document.querySelectorAll('.side .ic')].map(e=>(e.className)+':'+(e.title||'')), tab_titles:[...document.querySelectorAll('.tabs span')].map(e=>e.innerText+':'+e.className+':'+(e.title||'')),
 dock_titles:[...document.querySelectorAll('.dock span')].map(e=>e.className+':'+(e.title||'')), min_font_px:minfs, min_font_el:minel, h_overflow:ov, clipped:clipped,
 head_top:q('.head').getBoundingClientRect().top, search_top:q('.search').getBoundingClientRect().top, tabs_top:q('.tabs').getBoundingClientRect().top,
 body_has_token_text:document.body.innerText.includes('mock'), localStorage_len:localStorage.length, sessionStorage_len:sessionStorage.length,
 doc_w:document.documentElement.scrollWidth, doc_h:document.documentElement.scrollHeight}}'''
log={}
def shot(p, name, vp, m, theme='pink', tok='mock', after=None, wait=1800):
    if m: mode(m)
    ctx=p.chromium.launch(); c=ctx.new_context(viewport={'width':vp[0],'height':vp[1]}, device_scale_factor=1)
    pg=c.new_page(); ent={'viewport':vp,'mode':m,'theme':theme,'console':[],'failed':[],'http_err':[],'non_get':[],'requests':[]}
    pg.on('console', lambda msg: ent['console'].append(f'{msg.type}: {msg.text}'))
    pg.on('pageerror', lambda e: ent['console'].append(f'pageerror: {e}'))
    pg.on('requestfailed', lambda r: ent['failed'].append(f'{r.method} {r.url} {r.failure}'))
    pg.on('response', lambda r: ent['http_err'].append(f'{r.request.method} {r.url} -> {r.status}') if r.status>=400 else None)
    def onreq(r):
        ent['requests'].append(f"{r.method} {r.url.replace(BASE,'')} X-Token={'set' if r.headers.get('x-token') else 'none'}")
        if r.method!='GET': ent['non_get'].append(r.method+' '+r.url)
    pg.on('request', onreq)
    url=f'{BASE}/p/bench#t={tok}&theme={theme}'
    ent['url']=url.replace('t='+tok,'t=<'+('mock' if tok=='mock' else 'wrong')+'>')
    pg.goto(url); pg.wait_for_timeout(wait)
    if after: after(pg, ent)
    pg.screenshot(path=EV+name+'.png')
    ent['facts']=pg.evaluate(JS_FACTS); ent['boxes']=pg.evaluate(JS_BOX,['.wrap',SEL])
    ent['time']=datetime.datetime.now().strftime('%H:%M:%S')
    ent['requests']=sorted(set(ent['requests']))
    log[name]=ent; ctx.close()
with sync_playwright() as p:
    for th in ['pink','blue','violet']:
        shot(p,f'P3-0-page-{th}-1640x680',(1640,680),'data',th)
    shot(p,'P3-0-page-pink-1640x740',(1640,740),'data')
    shot(p,'P3-0-small-900x560',(900,560),'data')
    shot(p,'P3-0-small-900x500',(900,500),'data')
    shot(p,'P3-0-empty-1640x680',(1640,680),'empty')
    shot(p,'P3-0-offline-1640x680',(1640,680),'offline')
    def go_off(pg,ent):
        mode('offline'); pg.wait_for_timeout(6000)
    shot(p,'P3-0-data-then-offline-1640x680',(1640,680),'data',after=go_off)
    shot(p,'P3-0-401-1640x680',(1640,680),'data',tok='wrongtoken')
    def abort(pg,ent):
        pg.route('**/api/body/**', lambda r: r.abort()); pg.wait_for_timeout(6000)
    shot(p,'P3-0-data-then-netdown-1640x680',(1640,680),'data',after=abort)
    def search_none(pg,ent):
        pg.fill('#q','不存在的词'); pg.wait_for_timeout(300); ent['search_none']=pg.inner_text('#say')
        pg.fill('#q','面'); pg.wait_for_timeout(300); ent['search_hit']=pg.inner_text('#say')
        pg.fill('#q','不存在的词'); pg.wait_for_timeout(300)
    shot(p,'P3-0-search-nohit-1640x680',(1640,680),'data',after=search_none)
    def hover(pg,ent):
        pg.hover('.side .ic.off >> nth=0'); pg.wait_for_timeout(300)
        ent['tick_count_10s']=None
    mode('data')
    # reference: regenerate std image from 样式稿 (?only=p3&nomk=1)
    b=p.chromium.launch(); c=b.new_context(viewport={'width':1920,'height':1080}); pg=c.new_page()
    errs=[]; pg.on('console', lambda m: errs.append(m.type+': '+m.text))
    pg.goto('http://127.0.0.1:8920/'+urllib.request.quote('XQ_样式稿_桌宠_近景_工作台_20261116.html')+'?only=p3&nomk=1'); pg.wait_for_timeout(1500)
    pg.screenshot(path='/workspace/p3-0/std_regen_P3_纯净_1920x1080.png')
    ref=pg.evaluate(JS_BOX,['#p3 .pg',SEL]); fr=pg.evaluate("()=>{const r=document.querySelector('#p3 .win').getBoundingClientRect();const h=document.querySelector('#p3 .her').getBoundingClientRect();return {win:[r.left,r.top,r.width,r.height],her:[h.left,h.top,h.width,h.height]}}")
    log['_ref']={'boxes':ref,'frame':fr,'console':errs}
    b.close()
json.dump(log,open('/workspace/p3-0/shots.json','w'),ensure_ascii=False,indent=1)
print('done')
