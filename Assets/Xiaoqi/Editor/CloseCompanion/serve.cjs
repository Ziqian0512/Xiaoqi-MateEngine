const http=require('http'),fs=require('fs'),path=require('path');
const base=__dirname,root='E:/dsh-pet-projects';
const mounts=[['/vendor/',root+'/dspet-main/app/desk/web/vendor/']];
const model=root+'/Xiaoqi-Tonight/Xiaoqi/XiaoqiData/16.vrm';
http.createServer((req,res)=>{
 let url;try{url=decodeURIComponent(new URL(req.url,'http://localhost').pathname);}catch{res.writeHead(400).end();return;}
 let file;if(url==='/model.vrm')file=model;else{
  const mount=mounts.find(m=>url.startsWith(m[0]));const prefix=mount?mount[1]:base;
  file=path.resolve(prefix,mount?url.slice(mount[0].length):'.'+(url==='/'?'/index.html':url));
  if(!file.toLowerCase().startsWith(path.resolve(prefix).toLowerCase()+path.sep)){res.writeHead(403).end();return;}
 }
 fs.stat(file,(error,stat)=>{if(error||!stat.isFile()){res.writeHead(404).end();return;}
  const type={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.png':'image/png','.css':'text/css','.json':'application/json','.vrm':'application/octet-stream'}[path.extname(file)]||'application/octet-stream';
  res.writeHead(200,{'Content-Type':type,'Content-Length':stat.size,'Cache-Control':'no-store'});fs.createReadStream(file).pipe(res);
 });
}).listen(17942,'127.0.0.1',()=>console.log('Close refinement preview http://127.0.0.1:17942'));
