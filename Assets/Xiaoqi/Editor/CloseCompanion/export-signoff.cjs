const fs=require('fs'),path=require('path');
const {chromium}=require(path.resolve(path.dirname(process.execPath),'../node_modules/playwright'));
const sharp=require(path.resolve(path.dirname(process.execPath),'../node_modules/sharp'));
(async()=>{
 const browser=await chromium.launch({channel:'msedge',headless:true});
 try{
 const page=await browser.newPage({viewport:{width:1920,height:1080},deviceScaleFactor:2});
 await page.goto('http://127.0.0.1:17950/?artonly=1',{waitUntil:'domcontentloaded'});await page.evaluate(()=>document.fonts.ready);
 await page.evaluate(()=>{document.body.classList.add('capture');document.querySelector('.stage').style.transform='none';setBackground('transparent');document.querySelector('.ui').style.visibility='hidden';});
 await page.locator('.stage').screenshot({path:path.join(__dirname,'assets/structure-2x.png'),omitBackground:true});
 const raw=await sharp(path.join(__dirname,'assets/structure-2x.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let alpha=0;for(let y=360;y<1260;y++)for(let x=1360;x<2480;x++)alpha=Math.max(alpha,raw.data[(y*raw.info.width+x)*4+3]);
 if(alpha!==0)throw new Error('Decorative art entered face area');
 await page.evaluate(()=>{document.querySelector('.composition-art').style.visibility='hidden';const sig=document.querySelector('.signature');sig.style.visibility='visible';sig.style.transform='none';});
 await page.screenshot({path:path.join(__dirname,'assets/signoff-2x.png'),clip:{x:212,y:620,width:316,height:64},omitBackground:true});
 const check=JSON.parse(fs.readFileSync(path.join(__dirname,'checks.json')));
 check.nativeAsset={signoffPixels:[632,128],logicalParent:[220,628,300,48],imagePadding:8,parentRotationDegrees:-1,faceAlpha:alpha,fontBakedForUnity:true};
 fs.writeFileSync(path.join(__dirname,'checks.json'),JSON.stringify(check,null,2));
 console.log('Exported the actual plain sign-off as a transparent 2x asset; central decoration alpha=0.');
 }finally{await browser.close();}
})();
