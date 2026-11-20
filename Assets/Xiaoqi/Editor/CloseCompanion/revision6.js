(()=>{
 const svg=document.querySelector('.composition-art svg');
 for(const path of svg.querySelectorAll('path'))
 {
  if((path.getAttribute('d')||'').startsWith('M206 541'))path.setAttribute('d','M359 584C382 628 307 625 278 645S235 665 240 690');
  if((path.getAttribute('d')||'').startsWith('m266 598'))path.setAttribute('d','m312 628 2 6 6 2-6 2-2 6-2-6-6-2 6-2Z');
 }
 const ring=svg.querySelector('circle[cx="207"]');if(ring){ring.setAttribute('cx','359');ring.setAttribute('cy','592');}
 const signature=document.querySelector('.signature'),source=window.signatureSource,r=source.sourceRect;
 const wordmark=document.createElementNS('http://www.w3.org/2000/svg','svg');wordmark.setAttribute('class','signature');wordmark.setAttribute('viewBox',`${r.x} ${r.y} ${r.width} ${r.height}`);wordmark.setAttribute('role','img');wordmark.setAttribute('aria-label','amamiya inori，同字号单行手写艺术签名');
 wordmark.innerHTML=`<image href="assets/signature-uniform.png" x="0" y="0" width="${source.naturalWidth}" height="${source.naturalHeight}"/>`;signature.replaceWith(wordmark);
 document.querySelector('.toolbar b').textContent='小祈 · 左侧排布与同字号签名 / 第六稿';
 document.querySelector('.note').innerHTML='第六稿：时钟从 58px 放大到 72px，日期与地点同步放大；标题、爱心、播放器分层错落。签名改成同字号、小写的 amamiya inori 单行艺术字，横向展开并下移。播放器、右侧互动和声浪沿用第五稿。本轮只做预览，未修改 Unity。';
})();
