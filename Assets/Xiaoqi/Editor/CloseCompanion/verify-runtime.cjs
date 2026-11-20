const fs=require('fs'),path=require('path');
const sharp=require(path.resolve(path.dirname(process.execPath),'../node_modules/sharp'));
(async()=>{
 const directory=process.argv[2];if(!directory)throw new Error('Pass a production capture directory');
 const report=JSON.parse(fs.readFileSync(path.join(directory,'report.json')));
 const image=await sharp(path.join(directory,'close-ui.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let maxAlpha=0;for(let y=180;y<630;y++)for(let x=680;x<1240;x++)maxAlpha=Math.max(maxAlpha,image.data[(y*image.info.width+x)*4+3]);
 const frame=await sharp(path.join(directory,'close-frame.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const cornerAlpha=frame.data[(20*frame.info.width+20)*4+3];
 const near=(actual,expected)=>actual.every((v,i)=>Math.abs(v-expected[i])<=1);
 const checks={nativeViewport:report.actualViewport,compactOffscreenViewport:[1366,768],transparentHost:report.transparentHost,faceUiAlpha:maxAlpha,frameCornerAlpha:cornerAlpha,
  cameraRectFull:report.cameraRectFull,signaturePreserved:near(report.signature,[710,664.2,500,122.9]),wavePreserved:near(report.wave,[574.3,840,771.4,77.14]),
  nativeHitRegionsRespectCapabilities:Object.values(report.hitTargets).every(Boolean),compactHitRegionsRespectCapabilities:Object.values(report.compactNativeLayout.hitTargets).every(Boolean),
  centerHasNoAction:report.centerHasNoAction,usageModal:report.usageModal,musicModal:report.musicModal,replyModal:report.replyModal,switchBench:report.switchBench,switchPet:report.switchPet,switchClose:report.switchClose,todayUsageAvailable:report.todayUsageAvailable,
  operatingSystemPointerClicksVerified:false,mediaCommandsSentDuringReview:false,compactIsPhysicalMonitorResolution:false};
 const required=['transparentHost','cameraRectFull','signaturePreserved','wavePreserved','nativeHitRegionsRespectCapabilities','compactHitRegionsRespectCapabilities','centerHasNoAction','usageModal','musicModal','replyModal','switchBench','switchPet','switchClose','todayUsageAvailable'];
 if(maxAlpha!==0||cornerAlpha!==0||required.some(key=>!checks[key]))throw new Error('Native verification failed: '+JSON.stringify(checks));
 fs.writeFileSync(path.join(__dirname,'verification.json'),JSON.stringify(checks,null,2));console.log(JSON.stringify(checks));
})();
