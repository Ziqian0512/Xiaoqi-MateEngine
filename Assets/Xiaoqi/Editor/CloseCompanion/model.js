import * as THREE from 'three';
import {GLTFLoader} from 'three/addons/loaders/GLTFLoader.js';
import {VRMLoaderPlugin,VRMUtils} from '@pixiv/three-vrm';
const width=1920,height=1080;
const renderer=new THREE.WebGLRenderer({alpha:true,antialias:true,preserveDrawingBuffer:true});
renderer.setSize(width,height);renderer.setPixelRatio(1);renderer.setClearColor(0,0);
renderer.outputColorSpace=THREE.SRGBColorSpace;renderer.toneMapping=THREE.ACESFilmicToneMapping;renderer.toneMappingExposure=.92;
document.querySelector('#avatar').appendChild(renderer.domElement);
const scene=new THREE.Scene();scene.add(new THREE.HemisphereLight(0xffffff,0xb4b0c5,1.6));
const key=new THREE.DirectionalLight(0xfff8f2,1.15);key.position.set(-1,3,3);scene.add(key);
const span=.95,aspect=width/height;
const camera=new THREE.OrthographicCamera(-span*aspect/2,span*aspect/2,span/2,-span/2,.01,20);
let vrm;
window.modelReady=false;window.modelError='';window.modelFacts={};
const loader=new GLTFLoader();loader.register(p=>new VRMLoaderPlugin(p));
try{
 const gltf=await loader.loadAsync('/model.vrm');vrm=gltf.userData.vrm;
 if(!vrm)throw new Error('Model did not contain VRM data');
 if(String(vrm.meta?.metaVersion??'0').startsWith('0'))VRMUtils.rotateVRM0(vrm);
 // The source VRM uses KHR_materials_unlit. Apply preview-only toon lighting so
 // folds and face volume remain legible; Unity's existing materials are untouched.
 const ramp=new THREE.DataTexture(new Uint8Array([170,205,233,255]),4,1,THREE.RedFormat);
 ramp.minFilter=ramp.magFilter=THREE.NearestFilter;ramp.needsUpdate=true;
 const materials=new Map();
 vrm.scene.traverse(node=>{if(!node.isMesh)return;const shade=m=>{
  if(!materials.has(m))materials.set(m,new THREE.MeshToonMaterial({map:m.map,color:m.color?.clone()??new THREE.Color(1,1,1),alphaMap:m.alphaMap,transparent:m.transparent,opacity:m.opacity,alphaTest:m.alphaTest,side:m.side,depthWrite:m.depthWrite,gradientMap:ramp}));
  return materials.get(m);
 };node.material=Array.isArray(node.material)?node.material.map(shade):shade(node.material);});
 scene.add(vrm.scene);vrm.scene.updateMatrixWorld(true);
 const initial=new THREE.Box3().setFromObject(vrm.scene);vrm.scene.scale.setScalar(1.58/(initial.max.y-initial.min.y));
 let box=new THREE.Box3().setFromObject(vrm.scene);vrm.scene.position.y-=box.min.y;
 const bone=n=>vrm.humanoid?.getNormalizedBoneNode(n);
 if(bone('leftUpperArm'))bone('leftUpperArm').rotation.z=1.12;
 if(bone('rightUpperArm'))bone('rightUpperArm').rotation.z=-1.12;
 if(bone('leftLowerArm'))bone('leftLowerArm').rotation.z=.08;
 if(bone('rightLowerArm'))bone('rightLowerArm').rotation.z=-.08;
 vrm.expressionManager?.setValue('happy',.12);vrm.expressionManager?.setValue('joy',.12);
 vrm.update(0);vrm.scene.updateMatrixWorld(true);
 const eye=vrm.humanoid?.getRawBoneNode('leftEye')||vrm.humanoid?.getRawBoneNode('head');
 const eyeWorld=eye.getWorldPosition(new THREE.Vector3());
 const rightEye=vrm.humanoid?.getRawBoneNode('rightEye');
 if(rightEye)eyeWorld.lerp(rightEye.getWorldPosition(new THREE.Vector3()),.5);
 if(!vrm.humanoid?.getRawBoneNode('leftEye'))eyeWorld.y+=.08;
 const centerY=eyeWorld.y-(.5-.43)*span;
 camera.position.set(eyeWorld.x,centerY,3);camera.lookAt(eyeWorld.x,centerY,0);
 if(vrm.lookAt){const target=new THREE.Object3D();target.position.set(eyeWorld.x,eyeWorld.y,3);scene.add(target);vrm.lookAt.target=target;}
 vrm.update(0);renderer.render(scene,camera);
 window.modelFacts={source:'Tonight/Xiaoqi/XiaoqiData/16.vrm',renderer:'Three.js preview, not Unity',eyeTarget:[.5,.43],verticalSpan:span};
 window.modelReady=true;
 window.previewExpression=kind=>{
  const em=vrm.expressionManager;if(!em)return;
  for(const name of ['happy','joy','blink','blinkLeft','blinkRight'])try{em.setValue(name,0);}catch{}
  for(const name of kind==='pat'?['happy','joy']:kind==='wink'?['blinkLeft']:[])try{em.setValue(name,kind==='pat'?.45:1);}catch{}
  vrm.update(0);renderer.render(scene,camera);
 };
}catch(error){window.modelError=String(error);console.error(error);document.querySelector('#avatar-error').hidden=false;}
function render(){if(vrm)renderer.render(scene,camera);requestAnimationFrame(render);}render();
