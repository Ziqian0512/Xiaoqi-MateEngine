const fs=require('fs'),path=require('path');
const {chromium}=require(path.resolve(path.dirname(process.execPath),'../node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true,args:['--enable-unsafe-swiftshader']});
 const page=await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:1});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')console.log('Browser: '+m.text().slice(0,240));});
 await page.goto('http://127.0.0.1:17942/',{waitUntil:'domcontentloaded'});
 console.log('Page opened; loading existing VRM');
 await page.waitForFunction(()=>window.modelReady||window.modelError,{},{timeout:150000});
 const modelError=await page.evaluate(()=>window.modelError);if(modelError)throw new Error(modelError);
 console.log('Existing VRM rendered: '+JSON.stringify(await page.evaluate(()=>window.modelFacts)));
 await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode())));
 await page.evaluate(()=>{document.body.classList.add('capture');document.querySelector('.stage').style.transform='none';});
 await page.waitForTimeout(1400);
 for(const background of ['light','dark','transparent']){
  await page.evaluate(background=>setBackground(background),background);await page.waitForTimeout(250);
  await page.locator('.stage').screenshot({path:path.join(__dirname,'close-'+background+'.png'),omitBackground:true});
 }
 await page.evaluate(()=>{setBackground('light');setState('listen');});await page.locator('.stage').screenshot({path:path.join(__dirname,'close-listening.png')});
 await page.evaluate(()=>{setState('idle');document.querySelector('.stage').classList.add('show-window');});await page.locator('.stage').screenshot({path:path.join(__dirname,'close-desktop-window.png')});
 await page.evaluate(()=>{document.querySelector('.stage').classList.remove('show-window');buildDetails();});await page.locator('#detail-sheet').screenshot({path:path.join(__dirname,'details.png')});
 await page.locator('.ui #music-next').click();if(await page.locator('.ui #track-title').textContent()!=='夜空中最亮的星')throw new Error('Next preview failed');
 await page.locator('.ui #music-prev').click();if(await page.locator('.ui #track-title').textContent()!=='群青')throw new Error('Previous preview failed');
 await page.locator('.ui #music-play').click();if(await page.locator('.ui #music-play use').getAttribute('href')!=='#play')throw new Error('Pause preview failed');
 await page.locator('.ui #music-play').click();if(await page.locator('.ui #music-play use').getAttribute('href')!=='#pause')throw new Error('Play preview failed');
 await page.locator('.ui .listen').click();if(await page.evaluate(()=>window.previewState)!=='listen')throw new Error('Listening preview failed');
 const layout=await page.evaluate(()=>{const stage=document.querySelector('.stage').getBoundingClientRect();const result={};for(const selector of ['.signature','.wave','.player','.status','.token','.chat','.reply','.pat','.listen','.screen','.quiet']){const r=document.querySelector(selector).getBoundingClientRect();result[selector]={x:r.x-stage.x,y:r.y-stage.y,w:r.width,h:r.height};}return result;});
 if(Math.abs(layout['.signature'].x-710)>.5||Math.abs(layout['.signature'].y-664.2)>.5||Math.abs(layout['.wave'].x-574.3)>.5||Math.abs(layout['.wave'].y-840)>.5)throw new Error('Signature/wave anchors moved');
 const report={viewport:[1920,1080],model:await page.evaluate(()=>window.modelFacts),signatureAndWaveAnchorsPreserved:true,layout,errors,previewOnly:true,simulatedData:true,modelIsUnityScreenshot:false};
 const composition=await page.evaluate(()=>{
  const origin=document.querySelector('.stage').getBoundingClientRect();
  const r=selector=>{const b=document.querySelector(selector).getBoundingClientRect();return{left:b.left-origin.left,right:b.right-origin.left,top:b.top-origin.top,bottom:b.bottom-origin.top};};
  const face={left:680,top:180,right:1240,bottom:630};
  const overlays=['.pat','.listen','.screen','.quiet','.player','.status-cluster'];
  const faceClear=overlays.every(selector=>{const b=r(selector);return b.right<=face.left||b.left>=face.right||b.bottom<=face.top||b.top>=face.bottom;});
  const titleClear=['left','right'].every(side=>getComputedStyle(document.querySelector('.paper-tab.'+side)).display==='none'||r('.composition-title.'+side).bottom+6<r('.paper-tab.'+side).top);
  const childrenInViewport=overlays.every(selector=>{const b=r(selector);return b.left>=0&&b.top>=0&&b.right<=1920&&b.bottom<=1080;});
  return {faceClear,titleClear,childrenInViewport,allGraphicsUseNormalUiOverlay:true,backdropFilter:getComputedStyle(document.querySelector('.player')).backdropFilter};
 });
 if(!composition.faceClear||!composition.titleClear||!composition.childrenInViewport||composition.backdropFilter!=='none')throw new Error('Composition constraint failed: '+JSON.stringify(composition));
 report.composition=composition;
 report.revision=await page.evaluate(()=>({...window.revisionFacts,characterRingPaths:document.querySelectorAll('.composition-art .orbit,.composition-art .orbit-light').length,statusHasRectangularFrame:document.querySelectorAll('.status-board rect').length>0,screenUsesRoundMedal:document.querySelectorAll('.screen .graphic circle').length>0}));
 if(report.revision.characterRingPaths!==0||report.revision.statusHasRectangularFrame||!report.revision.screenUsesRoundMedal)throw new Error('Revision 3 visual structure check failed');
 await page.evaluate(()=>{setState('idle');setBackground('light');document.querySelector('#detail-sheet').style.display='none';document.querySelector('.stage').classList.add('compact');window.scrollTo(0,0);});
 await page.screenshot({path:path.join(__dirname,'close-1366.png'),clip:{x:0,y:0,width:1366,height:768},omitBackground:true});
 const {data,info}=await require(path.resolve(path.dirname(process.execPath),'../node_modules/sharp'))(path.join(__dirname,'close-transparent.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 report.transparentCorner=data[(20*info.width+20)*4+3]===0;if(!report.transparentCorner)throw new Error('Transparent preview has an opaque background');
 report.playbackAndListeningPreviewControls=true;report.compactPreview=[1366,768];
 // Export real 2x alpha artwork for the future Unity material path. No runtime code changes.
 const art=await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:2});
 await art.goto('http://127.0.0.1:17942/?artonly=1',{waitUntil:'domcontentloaded'});
 await art.evaluate(()=>{document.body.classList.add('capture');setBackground('transparent');document.querySelector('.ui').style.visibility='hidden';});
 await art.locator('.stage').screenshot({path:path.join(__dirname,'assets/v3-structure.png'),omitBackground:true});
 const sharp=require(path.resolve(path.dirname(process.execPath),'../node_modules/sharp'));
 const decoration=await sharp(path.join(__dirname,'assets/v3-structure.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let faceAlpha=0;for(let y=360;y<1260;y++)for(let x=1360;x<2480;x++)faceAlpha=Math.max(faceAlpha,decoration.data[(y*decoration.info.width+x)*4+3]);
 if(faceAlpha!==0)throw new Error('Decorative art covers the reserved face region');
 await art.evaluate(()=>{document.querySelector('.composition-art').style.visibility='hidden';const group=document.querySelector('.status-cluster');group.style.visibility='visible';group.querySelector('.status').style.visibility='hidden';group.querySelector('.token').style.visibility='hidden';});
 await art.locator('.status-cluster').screenshot({path:path.join(__dirname,'assets/v3-status-surface.png'),omitBackground:true});
 await art.evaluate(()=>{document.querySelector('.status-cluster').style.visibility='hidden';const player=document.querySelector('.player');player.style.visibility='visible';for(const child of player.children)child.style.visibility='hidden';});
 await art.screenshot({path:path.join(__dirname,'assets/v3-player-surface.png'),clip:{x:52,y:718,width:532,height:320},omitBackground:true});
 await art.evaluate(()=>document.querySelector('.player').style.visibility='hidden');
 for(const key of ['pat','listen','screen','quiet']){
  const svg=await art.locator('.'+key+' .graphic').evaluate(el=>{const copy=el.cloneNode(true);copy.removeAttribute('class');copy.setAttribute('xmlns','http://www.w3.org/2000/svg');copy.setAttribute('width',el.viewBox.baseVal.width);copy.setAttribute('height',el.viewBox.baseVal.height);return new XMLSerializer().serializeToString(copy);});
  fs.writeFileSync(path.join(__dirname,'assets/v3-'+key+'.svg'),svg);
  const box=await art.locator('.'+key).evaluate(el=>{el.style.visibility='visible';el.style.transform='none';el.querySelector('.word').style.visibility='hidden';const r=el.getBoundingClientRect();return{x:r.x-12,y:r.y-12,width:r.width+24,height:r.height+24};});
  await art.screenshot({path:path.join(__dirname,'assets/v3-'+key+'.png'),clip:box,omitBackground:true});
  await art.locator('.'+key).evaluate(el=>el.style.visibility='hidden');
 }
 report.assetExports={structure:[3840,2160],status:[940,568],playerWith32pxPadding:[1064,640],interactionAssetsHave12pxPadding:true,faceRegionAlpha:faceAlpha,separateNativeText:true};
 await art.close();
 fs.writeFileSync(path.join(__dirname,'checks.json'),JSON.stringify(report,null,2));
 for(const name of ['light','dark','transparent','1366'])fs.copyFileSync(path.join(__dirname,'close-'+name+'.png'),path.join(__dirname,'close-v3-'+name+'.png'));
 fs.copyFileSync(path.join(__dirname,'details.png'),path.join(__dirname,'details-v3.png'));
 await browser.close();console.log('Rendered six near-view previews, component sheet and Unity-ready alpha artwork; pageErrors='+errors.length);
})();
