(()=>{
 const art=document.querySelector('.composition-art');
 art.innerHTML=`<svg viewBox="0 0 1920 1080" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
 <defs><linearGradient id="v5-thread"><stop stop-color="#d4a9c8"/><stop offset="1" stop-color="#bfa0d3"/></linearGradient></defs>
 <!-- Left: a single relaxed lead; no tape crowding the player. -->
 <path d="M206 541C181 601 280 583 271 628S235 653 240 690" fill="none" stroke="#fffaf5" stroke-width="4.2"/>
 <path d="M206 541C181 601 280 583 271 628S235 653 240 690" fill="none" stroke="url(#v5-thread)" stroke-width="1.6"/>
 <circle cx="207" cy="549" r="3.6" fill="#fff9f2" stroke="#cda4c4" stroke-width="1"/>
 <path d="m266 598 2 6 6 2-6 2-2 6-2-6-6-2 6-2Z" fill="#f6e4ee" stroke="#cba9cf" stroke-width=".9"/>
 <!-- Right: existing screen/moon plus the relocated speaking cassette. -->
 <path d="M1569 570C1605 585 1622 638 1668 626" fill="none" stroke="#c6a5ce" stroke-width="1.5" stroke-dasharray="3 5"/>
 <circle cx="1668" cy="626" r="4" fill="#fff8f3" stroke="#c3a0cc" stroke-width="1"/>
 <path d="M1738 461V556M1742 467V551" fill="none" stroke="#d4b5cd" stroke-width="1.3"/>
 <circle cx="1738" cy="495" r="3" fill="#f3e0ee" stroke="#c7a6d2" stroke-width=".8"/>
 <circle cx="1738" cy="518" r="3" fill="#fff3db" stroke="#d5b9cb" stroke-width=".8"/>
 <path d="m1738 537 2 5 5 2-5 2-2 5-2-5-5-2 5-2Z" fill="#e9d6ee" stroke="#c3a1cf" stroke-width=".8"/>
 <path d="M1790 637C1857 645 1863 718 1792 751S1722 763 1738 808" fill="none" stroke="#fffaf5" stroke-width="3.8"/>
 <path d="M1790 637C1857 645 1863 718 1792 751S1722 763 1738 808" fill="none" stroke="#c9a7d2" stroke-width="1.4"/>
 <path d="m1825 730 2 5 5 2-5 2-2 5-2-5-5-2 5-2Z" fill="#f4e4ef" stroke="#c7a6d1" stroke-width=".8"/>
 </svg>`;
 const player=document.querySelector('.player');
 player.insertAdjacentHTML('afterbegin',`<div class="player-shell"><svg viewBox="-24 -24 516 348" xmlns="http://www.w3.org/2000/svg"><defs>
 <linearGradient id="v5-frame" x1="0" y1="0" x2=".85" y2="1"><stop stop-color="#fffdf8"/><stop offset=".48" stop-color="#e2cee9"/><stop offset="1" stop-color="#c5abd5"/></linearGradient>
 <linearGradient id="v5-enamel" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#fffdf8"/><stop offset=".58" stop-color="#f8f1fa"/><stop offset="1" stop-color="#eee1f4"/></linearGradient>
 <linearGradient id="v5-bottom" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#d7bfe0" stop-opacity="0"/><stop offset="1" stop-color="#c6abd4" stop-opacity=".34"/></linearGradient></defs>
 <rect x="2" y="11" width="464" height="289" rx="29" fill="#83648e" opacity=".06"/><rect x="-2" y="4" width="472" height="296" rx="31" fill="#b9a0c7" opacity=".07"/>
 <rect x="0" y="0" width="468" height="295" rx="28" fill="url(#v5-frame)" stroke="#fffaf5" stroke-width="1.6"/>
 <rect x="5" y="5" width="458" height="284" rx="24" fill="url(#v5-enamel)" stroke="#e8d9ed" stroke-width="1"/>
 <path d="M6 227H462V265Q462 289 438 289H30Q6 289 6 265Z" fill="url(#v5-bottom)"/>
 <path d="M28 10H439Q458 10 458 29" fill="none" stroke="#fffefa" stroke-width="1.4"/>
 <path d="M11 259Q11 284 32 284H437" fill="none" stroke="#cab1d5" stroke-width=".85" opacity=".65"/>
 <path d="M24 36H218" stroke="#e9dbef" stroke-width=".8"/>
 <circle cx="449" cy="25" r="2" fill="#efe2f4" stroke="#c9afd6" stroke-width=".6"/>
 </svg></div><svg class="hardware-jack" viewBox="0 0 16 17" aria-hidden="true"><rect x="3" y="0" width="10" height="14" rx="3" fill="#ddc9e7" stroke="#b799c8" stroke-width="1"/><path d="M5 3h6m-6 4h6" stroke="#fffaf7" stroke-width="1"/><rect x="2" y="12" width="12" height="4" rx="2" fill="#b69ac7" stroke="#fff9f6" stroke-width=".8"/></svg><span class="radio-tag">祈の放送室</span><svg class="tiny-bow"><use href="#bow"/></svg>`);
 player.insertAdjacentHTML('beforeend','<div class="player-seam"></div><div class="playback-state"><i class="led"></i><span>正在一起听</span></div>');
 document.querySelector('.together').textContent='LISTENING TOGETHER';
 const originalClick=document.querySelector('#music-play').onclick;
 document.querySelector('#music-play').onclick=()=>{originalClick();const paused=document.querySelector('#music-play use').getAttribute('href')==='#play';player.classList.toggle('paused',paused);document.querySelector('.playback-state span').textContent=paused?'先暂停一会儿':'正在一起听';};
 document.querySelector('.toolbar b').textContent='小祈 · 左右重排与陪伴电台 / 第五稿';
 document.querySelector('.note').innerHTML='第五稿：听我说移到右侧，左侧留下爱心与重点播放器。播放器恢复 468px 宽，重新设计封面、唱片、细包边、柔紫按键与信息层级。所有视觉仍可用透明贴图和普通二维控件复现；不依赖实时磨砂或新接口。声浪、签名与中央透明区保持不变。本轮仅预览，未改 Unity。';
})();
