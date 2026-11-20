(()=>{
 const stage=document.querySelector('.stage'),ui=document.querySelector('.ui');
 const symbols=document.querySelector('svg defs');
 symbols.insertAdjacentHTML('beforeend','<symbol id="expand" viewBox="0 0 24 24"><path d="M8 3H3v5m13-5h5v5M3 16v5h5m13-5v5h-5" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></symbol>');
 document.querySelector('.status .chibi').src='assets/chibi-sit.png';
 document.querySelector('.status .chibi').alt='高清小祈，侧坐在云朵边向你招手';
 document.querySelector('.composition-art svg').innerHTML=document.querySelector('.composition-art svg').innerHTML.replace('M504 650C568 679 525 720 455 718S345 728 344 763','M504 650C555 676 526 713 452 715S344 729 344 755').replaceAll('y="744"','y="740"');
 ui.insertAdjacentHTML('beforeend',`<button class="mode-trigger" id="mode-trigger" aria-label="切换形态" aria-expanded="false" aria-controls="mode-menu" title="切换形态"><svg><use href="#expand"/></svg></button>
 <nav class="mode-menu" id="mode-menu" aria-label="选择小祈的形态"><div class="menu-title"><span>切换形态</span><small>XIAOQI</small></div><button data-view="pet"><svg><use href="#pet"/></svg><span>桌宠</span><span class="current">当前</span><span class="arrow">›</span></button><button data-view="close" class="active"><svg><use href="#spark"/></svg><span>近景</span><span class="current">当前</span><span class="arrow">›</span></button><button data-view="bench"><svg><use href="#grid"/></svg><span>工作台</span><span class="current">当前</span><span class="arrow">›</span></button></nav>
 <div class="pet-only pet-ui"><div class="pet-speech"><small>A LITTLE MOMENT</small>你先忙，我就在旁边。<br>想说话的时候，叫我就好。</div><div class="pet-chip here"><svg><use href="#pet"/></svg><span><span class="muted">她的此刻</span><b>在宿舍，听歌放空</b></span></div><div class="pet-chip next"><svg><use href="#cloud"/></svg><span><span class="muted">下一件事 · 23:00</span><b>晚间洗漱</b></span></div></div><div class="preview-toast" aria-live="polite"></div>`);
 document.querySelector('.desktop').insertAdjacentHTML('beforeend',`<div class="pet-only pet-focus"><div class="app-head"><i></i><i></i><i></i><span>正在做的事 · 桌面环境示意</span></div><div class="workspace"><aside><b>我的一天</b>正在进行<br>今天的记录<br>留到明天</aside><section><small>ONE THING AT A TIME</small><h1>先把眼前这一小段写完。</h1><p>桌宠只占身边的一小块地方。<br>不需要的面板，收起来就好。</p><div class="line"></div><div class="line short"></div><div class="line"></div><div class="line tiny"></div></section></div></div><div class="pet-only taskbar"><i></i><i></i><i></i><i></i><i></i></div>`);
 const trigger=document.querySelector('#mode-trigger'),menu=document.querySelector('#mode-menu');
 window.setModeMenu=open=>{menu.classList.toggle('open',open);stage.classList.toggle('menu-is-open',open);trigger.setAttribute('aria-expanded',String(open));};
 trigger.onclick=e=>{e.stopPropagation();setModeMenu(!menu.classList.contains('open'));};
 window.setPreviewView=view=>{
   if(view==='bench'){document.querySelector('.preview-toast').textContent='工作台沿用已完成版本，这里仅评审切换入口。';setTimeout(()=>document.querySelector('.preview-toast').textContent='',2600);setModeMenu(false);return;}
   window.previewView=view;stage.classList.toggle('pet',view==='pet');setModeMenu(false);
   stage.classList.toggle('show-pet-info',view==='pet'&&!!document.querySelector('#pet-info')?.checked);
   document.querySelectorAll('.mode-menu button').forEach(b=>b.classList.toggle('active',b.dataset.view===view));
   document.querySelectorAll('[data-preview-view]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.previewView===view)));
   window.setAvatarView?.(view);
 };
 menu.querySelectorAll('button').forEach(b=>b.onclick=e=>{e.stopPropagation();setPreviewView(b.dataset.view);});
 stage.addEventListener('click',e=>{if(!trigger.contains(e.target)&&!menu.contains(e.target))setModeMenu(false);});
 document.addEventListener('keydown',e=>{if(e.key==='Escape')setModeMenu(false);});
 const toolbar=document.querySelector('.toolbar');toolbar.querySelector('b').textContent='小祈 · 配件与形态入口 / 第四稿';
 toolbar.insertAdjacentHTML('beforeend','<div class="view-choice"><button data-preview-view="close" aria-pressed="true">近景</button><button data-preview-view="pet" aria-pressed="false">桌宠</button></div><label><input id="pet-info" type="checkbox"> 桌宠靠近时的信息</label>');
 toolbar.querySelectorAll('[data-preview-view]').forEach(b=>b.onclick=()=>setPreviewView(b.dataset.previewView));
 document.querySelector('#pet-info').onchange=e=>stage.classList.toggle('show-pet-info',e.target.checked);
 const presence=document.createElement('div');presence.className='pet-only pet-presence';presence.setAttribute('aria-label','桌宠靠近区域');ui.insertBefore(presence,trigger);
 presence.onmouseenter=()=>stage.classList.add('show-pet-info');
 presence.onmouseleave=()=>{if(!document.querySelector('#pet-info').checked&&!menu.classList.contains('open'))stage.classList.remove('show-pet-info');};
 trigger.addEventListener('mouseenter',()=>{if(window.previewView==='pet')stage.classList.add('show-pet-info');});
 stage.addEventListener('pointermove',e=>{if(window.previewView==='pet')stage.classList.toggle('show-pet-info',document.querySelector('#pet-info').checked||menu.classList.contains('open')||!!e.target.closest('.pet-presence,.mode-trigger,.mode-menu,.pet-chip'));});
 document.querySelector('.toolbar small').textContent='可点击的布局预览 · 示例内容 · 尚未应用到 Unity';
 document.querySelector('.toolbar a').style.display='none';
 document.querySelector('.note').innerHTML='只评审配件与入口：新 Q 版图是可直接导入的透明素材；布局、圆角、图标与展开菜单均用普通二维控件呈现。声浪与签名位置、中央镂空保持不变。角色是相同 VRM 的浏览器占位，不代表修改了 Unity 角色。桌面窗口和任务栏只是尺寸参照。';
 window.previewView='close';window.setModeMenu(false);
})();
