const fs=require('fs'),path=require('path');
const sharp=require(path.resolve(path.dirname(process.execPath),'../node_modules/sharp'));
const folder=process.argv[2];if(!folder)throw new Error('Pass the actual Unity capture directory.');
const assets=path.resolve(__dirname,'../../Resources/SweetNotebook');
(async()=>{
 const report=JSON.parse(fs.readFileSync(path.join(folder,'runtime-report.json')));
 const flags=['modal0','modal1','modal2','hitRegions0','hitRegions1','hitRegions2','pagination1','pagination2','historyRead','navigationToJournal','navigationRoundtrip'];
 for(const key of flags)if(report[key]!==true)throw new Error('Runtime check failed: '+key);
 for(const page of report.pages){
  if(Math.abs(page.stageW-1440)>.1||Math.abs(page.stageH-810)>.1)throw new Error('Unexpected stage dimensions: '+page.page);
  const critical=['sweet-clock','sweet-page','sweet-frameTitle','sweet-metaDate'];
  for(const label of page.nativeLabels)if(critical.includes(label.name)&&label.elided)throw new Error('Essential text truncated: '+label.name);
 }
 const pixels=[];
 for(const page of ['now','journal','memory']){
  const expected=await sharp(path.join(assets,page+'-surface.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const actual=await sharp(path.join(folder,page+'-decor.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  if(expected.info.width!==2880||actual.info.width!==1440||actual.info.height!==810)throw new Error('Unexpected capture dimensions.');
  let error=0,count=0,large=0;
  for(let y=2;y<808;y++)for(let x=2;x<1438;x++){
   const a=(y*1440+x)*4,s=((y*2)*2880+x*2)*4;
   if(actual.data[a+3]<254||[s,s+4,s+11520,s+11524].some(p=>expected.data[p+3]<254))continue;
   for(let c=0;c<3;c++){const value=(expected.data[s+c]+expected.data[s+4+c]+expected.data[s+11520+c]+expected.data[s+11524+c])/4;const d=Math.abs(value-actual.data[a+c]);error+=d;count++;if(d>16)large++;}
  }
  const result={page,meanChannelError:error/count,channelsOver16:large/count};pixels.push(result);
  if(result.meanChannelError>3.5)throw new Error('Artwork differs from approved export: '+JSON.stringify(result));
 }
 const result={runtimeChecks:flags.length,stage:[1440,810],viewport:[report.viewportWidth,report.viewportHeight],artworkComparison:pixels,usesActualRuntime:true};
 fs.writeFileSync(path.join(folder,'verification.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result,null,2));
})();
