import json,time
from playwright.sync_api import sync_playwright
out={}
with sync_playwright() as p:
    b=p.chromium.launch()
    c=b.new_context(viewport={'width':1640,'height':680}); pg=c.new_page(); ts=[]
    pg.on('request', lambda r: ts.append((time.time(),r.url.split('/api/body/')[-1])) if '/api/body/' in r.url else None)
    t0=time.time(); pg.goto('http://127.0.0.1:8911/p/bench#t=mock&theme=pink'); pg.wait_for_timeout(16000)
    out['poll']=[(round(t-t0,1),u) for t,u in ts if u.startswith('stage')]
    # tooltip: hover off icon, check title attr; native tooltip not rendered in headless
    pg.hover('.side .ic.off >> nth=0'); out['hover_title']=pg.eval_on_selector('.side .ic.off >> nth=0','e=>e.title')
    # click a disabled tab -> anything change?
    before=pg.content(); pg.click('.tabs span.off >> nth=0'); pg.click('.dock span.off >> nth=0'); pg.wait_for_timeout(300)
    out['url_after_clicks']=pg.url.replace('t=mock','t=<mock>'); out['dom_changed_after_clicks']=(before!=pg.content())
    # computed glass styles
    out['styles']=pg.evaluate('''()=>{const g=s=>{const c=getComputedStyle(document.querySelector(s));return {bg:c.backgroundColor,border:c.border,shadow:c.boxShadow,backdrop:c.backdropFilter,radius:c.borderRadius}};return {card:g('.card.b'),side:g('.side'),dock:g('.dock')}}''')
    c.close()
    c=b.new_context(viewport={'width':820,'height':340},device_scale_factor=2); pg=c.new_page()
    pg.goto('http://127.0.0.1:8911/p/bench#t=mock&theme=pink'); pg.wait_for_timeout(1800)
    pg.screenshot(path='/workspace/p3-0/evidence/P3-0-page-pink-820x340@2x.png')
    out['boxes_820']=pg.evaluate('''()=>{const S={'1':'.side','2':'.tabs','3':'.search','4':'.card.a','5':'.card.b','6':'.card.c','7':'.card.d','8':'.dock'};const o={};for(const[k,s]of Object.entries(S)){const r=document.querySelector(s).getBoundingClientRect();o[k]=[r.left*2,r.top*2,r.width*2,r.height*2].map(Math.round)}return o}''')
    out['clipped_820']=pg.evaluate('''()=>{const c=[];document.querySelectorAll('.tl .r,.say,.rw,.foot').forEach(e=>{const r=e.getBoundingClientRect();const k=e.closest('.card').getBoundingClientRect();if(r.bottom>k.bottom-1)c.push(e.innerText.slice(0,12))});return c}''')
    b.close()
json.dump(out,open('/workspace/p3-0/extra.json','w'),ensure_ascii=False,indent=1); print(json.dumps(out,ensure_ascii=False))
