(()=>{
 const old=document.querySelector('.signature');
 const mark=document.createElement('div');mark.className='signature';mark.setAttribute('role','img');mark.setAttribute('aria-label','amamiya inori，左侧灰紫色同字号手写落款');
 mark.innerHTML='<span class="signature-name">amamiya inori</span><svg class="signature-spark" viewBox="0 0 16 16" aria-hidden="true"><path d="m8 1.5 1.7 4.8L14.5 8l-4.8 1.7L8 14.5l-1.7-4.8L1.5 8l4.8-1.7Z" fill="none" stroke="currentColor" stroke-width="1" stroke-linejoin="round"/></svg>';
 old.replaceWith(mark);
 const svg=document.querySelector('.composition-art svg');
 for(const path of svg.querySelectorAll('path'))
 {
  const d=path.getAttribute('d')||'';
  if(d.startsWith('M359 584'))path.setAttribute('d','M359 584C360 621 192 608 190 641S208 681 240 690');
  if(d.startsWith('m312 628'))path.remove();
 }
 document.querySelector('.toolbar b').textContent='小祈 · 把名字收成轻落款 / 第七稿';
 document.querySelector('.note').innerHTML='第七稿：完整名字保留同字号、小写单行，改为灰紫色的手写落款，放在爱心与播放器之间。去掉渐变、粗描边和外发光；连线避开文字，人物胸前完整留空。其余布局沿用第六稿。本轮仅预览，未修改 Unity。';
})();
