const fs=require('fs'),path=require('path');
const sharp=require(path.resolve(path.dirname(process.execPath),'../node_modules/sharp'));
(async()=>{
 const out=path.resolve(__dirname,'../../Resources/CloseCompanion');fs.mkdirSync(out,{recursive:true});
 for(const key of ['structure','status-surface','player-surface','pat','listen','screen','quiet'])fs.copyFileSync(path.join(__dirname,'assets/v3-'+key+'.png'),path.join(out,key+'.png'));
 for(const key of ['record','bow'])await sharp(path.join(__dirname,'assets/v3-'+key+'.svg')).resize(key==='bow'?288:228,key==='bow'?204:228).png().toFile(path.join(out,key+'.png'));
 console.log('Exported the approved 2x alpha artwork to Resources/CloseCompanion');
})();
