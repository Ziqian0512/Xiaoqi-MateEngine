// Graphic composition is separate from live text and actual click regions.
// All surfaces can be baked into alpha textures; none requires real-time blur.
const stageRoot=document.querySelector('#stage'),uiRoot=document.querySelector('#ui');
const structure=document.createElement('div');structure.className='composition-art';
structure.innerHTML=`<svg viewBox="0 0 1920 1080" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
<defs><linearGradient id="editorial-paper" x1="0" y1="0" x2=".8" y2="1"><stop stop-color="#fffcf7" stop-opacity=".94"/><stop offset="1" stop-color="#ead9ef" stop-opacity=".85"/></linearGradient><linearGradient id="ribbon-paper" x2="1" y2="1"><stop stop-color="#fff7ee" stop-opacity=".82"/><stop offset="1" stop-color="#f1dbe9" stop-opacity=".78"/></linearGradient></defs>
<!-- Open segments stay outside the body so Unity's normal UI overlay can draw them. -->
<path class="orbit-light" d="M548 319C610 155 760 120 930 123M1112 125C1320 125 1410 270 1406 400M1437 674C1402 730 1375 757 1340 790M588 747C551 697 526 624 515 559"/>
<path class="orbit" d="M555 327C608 158 762 124 944 135M1120 137C1320 137 1400 280 1394 401M1427 677C1394 729 1368 752 1332 781M600 741C561 688 539 626 528 562"/>
<path class="fine" stroke-dasharray="3 10" d="M565 340C605 168 752 135 885 151M1129 151C1320 136 1400 260 1368 400M610 740C575 690 570 647 558 562"/>
<path class="fine" d="M77 243V278q0 13 13 13h12M1857 374v16q0 13-13 13h-12"/>
<path d="m561 285 4 13 13 4-13 4-4 13-4-13-13-4 13-4Zm774 429 3 10 10 3-10 3-3 10-3-10-10-3 10-3Z" fill="#eadce9" stroke="#c6a6cf" stroke-width="1"/>
<circle cx="1225" cy="171" r="5" fill="#f1dbe9" stroke="#c7a6d0" stroke-width="1"/>
<!-- Left interaction page: ruled margin, punched edge, quiet print. -->
<path d="M101 360 548 354Q567 353 565 373L552 682Q551 703 531 703L93 710Q73 709 75 689L87 380Q87 361 101 360Z" fill="#c8afd91a" transform="translate(0 7)"/>
<path class="cut-paper" d="M101 353 548 347Q567 346 565 366L552 675Q551 696 531 696L93 703Q73 702 75 682L87 373Q87 354 101 353Z"/>
<path class="paper-line" d="M112 366 538 360Q550 359 549 372L538 666Q538 681 524 681L103 688Q90 688 91 674L102 380Q102 367 112 366Z"/>
<path d="M105 404H539M103 464H537M101 524H535M98 584H533M96 644H531" fill="none" stroke="#d7c2df" stroke-width=".7" opacity=".28"/>
<path d="M128 371 117 679" stroke="#d7b8cc" stroke-width="1" opacity=".6"/>
<g class="punch"><circle cx="98" cy="392" r="5"/><circle cx="95" cy="465" r="5"/><circle cx="92" cy="538" r="5"/><circle cx="89" cy="611" r="5"/><circle cx="86" cy="674" r="5"/></g>
<text x="154" y="666" class="print" font-size="37" font-style="italic" letter-spacing="6" opacity=".22">WITH YOU.</text>
<!-- A headphone-like lead joins the listening sticker to the record player. -->
<path d="M504 650C568 679 525 720 455 718S345 728 344 763" fill="none" stroke="#fffaf7" stroke-width="4.3"/>
<path d="M504 650C568 679 525 720 455 718S345 728 344 763" fill="none" stroke="#c7a4d2" stroke-width="1.8"/>
<rect x="340" y="744" width="8" height="18" rx="3" fill="#d6bfdf" stroke="#b99bcc" stroke-width="1"/>
<!-- Right companion ticket. This local paper is clear of the central face. -->
<path d="M1404 447 1818 436Q1840 436 1840 458L1831 718Q1830 739 1810 738L1393 750Q1374 750 1375 729L1384 469Q1385 448 1404 447Z" fill="#d6b9d218" transform="translate(0 7)"/>
<path d="M1404 440 1818 429Q1840 429 1840 451L1831 711Q1830 732 1810 731L1393 743Q1374 743 1375 722L1384 462Q1385 441 1404 440Z" fill="url(#ribbon-paper)" stroke="#fffaf6" stroke-width="2"/>
<path d="M1408 455 1810 444Q1824 444 1824 457L1816 704Q1816 715 1804 715L1400 727Q1389 727 1389 715L1398 468Q1398 455 1408 455Z" fill="none" stroke="#d5b6d2" stroke-width="1.2"/>
<path d="M1467 628C1499 681 1591 695 1668 654" fill="none" stroke="#c8a5c6" stroke-width="1.4" stroke-dasharray="3 6"/>
<path d="m1555 677 2 6 6 2-6 2-2 6-2-6-6-2 6-2Z" fill="#f7e8f0" stroke="#c9a2c0" stroke-width="1"/>
<text x="1410" y="719" class="print" font-size="11" letter-spacing="2" opacity=".65" transform="rotate(-2 1410 719)">A SMALL CORNER, JUST FOR US</text>
</svg>`;
stageRoot.insertBefore(structure,document.querySelector('#avatar'));
// Static safety opening is baked into the decoration texture, not a live character mask.
const graphic=structure.querySelector('svg'),defs=graphic.querySelector('defs');
defs.insertAdjacentHTML('beforeend','<clipPath id="composition-face-clear"><path clip-rule="evenodd" fill-rule="evenodd" d="M0 0H1920V1080H0Z M680 180V630H1240V180Z"/></clipPath>');
const safeGroup=document.createElementNS('http://www.w3.org/2000/svg','g');safeGroup.setAttribute('clip-path','url(#composition-face-clear)');
for(const child of [...graphic.children])if(child!==defs)safeGroup.appendChild(child);graphic.appendChild(safeGroup);
uiRoot.insertAdjacentHTML('afterbegin',`<div class="composition-title left"><small>THE LITTLE THINGS, WITH YOU</small>把今天，讲给我听。</div><div class="composition-title right"><small>INORI / BESIDE YOU</small>这会儿，想陪你。</div><div class="paper-tab left">一起待一会儿</div><div class="paper-tab right">小祈的陪伴备忘</div><svg class="composition-bow left"><use href="#bow"/></svg><svg class="composition-bow right"><use href="#bow"/></svg><div class="daily-label">小祈の日常</div><div class="little-mark">不着急，我在这儿。</div>`);
const cluster=document.createElement('section');cluster.className='status-cluster';
cluster.innerHTML=`<div class="status-board"><svg viewBox="0 0 470 284" xmlns="http://www.w3.org/2000/svg"><defs><linearGradient id="status-paper" x1="0" y1="0" x2=".7" y2="1"><stop stop-color="#fffaf4"/><stop offset="1" stop-color="#e9d9f0"/></linearGradient></defs><path d="M36 12H437Q458 12 458 34V252Q458 271 438 271H28Q10 271 10 253V44Q10 12 36 12Z" fill="#c6addc26" transform="translate(0 6)"/><path d="M36 12H437Q458 12 458 34V252Q458 271 438 271H28Q10 271 10 253V44Q10 12 36 12Z" fill="url(#status-paper)" fill-opacity=".94" stroke="#fffaf7" stroke-width="2"/><path d="M38 24H429Q445 24 445 39V246Q445 258 432 258H35Q23 258 23 246V48Q23 24 38 24Z" fill="none" stroke="#d6bddf" stroke-width="1.1"/><path d="M345 15h55v15l-7-3-7 3-7-3-7 3-7-3-7 3-7-3-6 3Z" fill="#ecd2df" fill-opacity=".75"/><path d="M45 197h378" stroke="#fffaf7" stroke-width="1.5"/></svg></div><div class="stamp"><svg><use href="#heart"/></svg>WITH YOU</div>`;
const status=document.querySelector('.status'),token=document.querySelector('.token');
cluster.appendChild(status);cluster.appendChild(token);uiRoot.appendChild(cluster);
status.querySelector('.kicker').textContent='她的此刻';status.insertAdjacentHTML('beforeend','<div class="whisper">慢慢来，我在这里哦。</div>');
token.querySelector('.top>span').textContent='TOKEN / 今日消耗';
const record=document.createElement('div');record.className='record';
record.innerHTML='<svg viewBox="0 0 114 114" xmlns="http://www.w3.org/2000/svg"><defs><linearGradient id="record-disc" x2="1" y2="1"><stop stop-color="#bea0d0"/><stop offset=".5" stop-color="#dcc5e7"/><stop offset="1" stop-color="#b998ce"/></linearGradient></defs><circle cx="57" cy="57" r="54" fill="url(#record-disc)" stroke="#fffaf7" stroke-width="1.5"/><g fill="none" stroke="#9d79b4" stroke-width=".7" opacity=".52"><circle cx="57" cy="57" r="48"/><circle cx="57" cy="57" r="43"/><circle cx="57" cy="57" r="38"/><circle cx="57" cy="57" r="33"/><circle cx="57" cy="57" r="28"/></g><circle cx="57" cy="57" r="18" fill="#f6e7f2" stroke="#c8a6d4" stroke-width="1"/><circle cx="57" cy="57" r="3" fill="#a881ba"/><path d="M79 16 92 33M80 23l8 10" stroke="#fff8f4" stroke-width="2" opacity=".65"/></svg>';
document.querySelector('.player').insertBefore(record,document.querySelector('.player .album'));
// Reuse original vector paths, but bring their finish into the richer shared palette.
for(const [id,top,bottom]of [['heart-bg','#fff1f4','#e5afc9'],['tape-bg','#f4e9fc','#c9afdF'],['window-bg','#fff9fc','#dbcaeb'],['moon-bg','#fff8df','#efd298']]){
 const gradient=document.querySelector('#'+id);if(gradient){gradient.children[0].setAttribute('stop-color',top);gradient.children[1].setAttribute('stop-color',bottom);}
}
document.querySelector('.toolbar b').textContent='小祈 · 近景精修 / 第二稿';
document.querySelector('#detail-player h2').textContent='01　陪伴电台 / 468 × 256 px';
document.querySelector('#detail-status h2').textContent='02　此刻与用量 / 470 × 284 px，一组呈现';
document.querySelector('#detail-stickers h2').textContent='03　放大的互动贴纸 / 共同材质，依附局部纸框';
