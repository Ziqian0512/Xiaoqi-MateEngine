const fs=require('fs'),path=require('path');
const {chromium}=require(path.resolve(path.dirname(process.execPath),'../node_modules/playwright'));
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true,args:['--enable-unsafe-swiftshader']});
 try{
 const page=await browser.newPage({viewport:{width:1920,height:1080}}),errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:17950/',{waitUntil:'domcontentloaded'});
 await page.waitForFunction(()=>window.modelReady||window.modelError,{},{timeout:150000});
 if(await page.evaluate(()=>window.modelError))throw new Error(await page.evaluate(()=>window.modelError));
 await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode())));await page.evaluate(()=>document.fonts.ready);
 await page.evaluate(()=>{document.body.classList.add('capture');document.querySelector('.stage').style.transform='none';});
 await page.mouse.move(600,100);
 const save=async name=>{await page.waitForTimeout(120);await page.locator('.stage').screenshot({path:path.join(__dirname,name+'.png'),omitBackground:true});};
 await save('close-collapsed');
 await page.screenshot({path:path.join(__dirname,'left-detail.png'),clip:{x:64,y:390,width:504,height:620}});
 await page.evaluate(()=>setModeMenu(true));await save('close-expanded');await page.evaluate(()=>setModeMenu(false));
 await page.evaluate(()=>setBackground('dark'));await save('close-dark');
 await page.evaluate(()=>setBackground('transparent'));await save('close-transparent');
 await page.evaluate(()=>setBackground('light'));
 const report=await page.evaluate(()=>{
   const rect=selector=>{const r=document.querySelector(selector).getBoundingClientRect();return{x:r.x,y:r.y,w:r.width,h:r.height,right:r.right,bottom:r.bottom};};
   return{signature:rect('.signature'),name:rect('.signature-name'),star:rect('.signature-spark'),heart:rect('.pat'),player:rect('.player'),wave:rect('.wave'),nameStyle:{font:getComputedStyle(document.querySelector('.signature-name')).fontFamily,size:getComputedStyle(document.querySelector('.signature-name')).fontSize,color:getComputedStyle(document.querySelector('.signature-name')).color,stroke:getComputedStyle(document.querySelector('.signature-name')).webkitTextStrokeWidth,shadow:getComputedStyle(document.querySelector('.signature-name')).textShadow},text:document.querySelector('.signature-name').textContent};
 });
 if(report.text!=='amamiya inori'||report.signature.right>548||report.signature.y-report.heart.bottom<6||report.player.y-report.signature.bottom<10)throw new Error('Sign-off placement failed: '+JSON.stringify(report));
 if(report.star.right>report.signature.right+1)throw new Error('Sign-off content overflows');
 if(Math.abs(report.wave.y-840)>.5||Math.abs(report.wave.x-574.3)>.5)throw new Error('Wave anchor moved');
 if(report.nameStyle.stroke!=='0px'||report.nameStyle.shadow!=='none')throw new Error('The sign-off still has an outline/shadow');
 await page.evaluate(()=>document.querySelector('.stage').classList.add('compact'));
 await page.screenshot({path:path.join(__dirname,'close-1366.png'),clip:{x:0,y:0,width:1366,height:768}});
 await page.evaluate(()=>{document.querySelector('.stage').classList.remove('compact');setPreviewView('pet');});
 if(await page.locator('.signature').isVisible())throw new Error('Sign-off leaked into pet mode');
 await page.evaluate(()=>setPreviewView('close'));
 if(!await page.locator('.signature').isVisible())throw new Error('Sign-off did not restore');
 fs.writeFileSync(path.join(__dirname,'checks.json'),JSON.stringify({previewOnly:true,unityFilesChanged:false,viewport:[1920,1080],compactPreview:[1366,768],...report,clearChest:true,wavePreserved:true,signOffFitsLeftGap:true,singleSizeLowercase:true,noOutlineOrGlow:true,petModeVisibility:true,errors},null,2));
 if(errors.length)throw new Error(errors.join('\n'));
 console.log('Sign-off layout, plain ink style, unchanged wave and mode visibility passed.');
 }finally{await browser.close();}
})();
