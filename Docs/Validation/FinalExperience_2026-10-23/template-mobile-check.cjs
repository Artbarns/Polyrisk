const fs=require('fs'),vm=require('vm'),assert=require('assert');
const source=fs.readFileSync(process.argv[2]||'Assets/WebGLTemplates/PolyRiskResponsive/index.html','utf8');
const start=source.indexOf('      // iPadOS');const end=source.indexOf('#if BACKGROUND_FILENAME',start);
const testScript=source.slice(start,end).replace(/#if SHOW_DIAGNOSTICS[\s\S]*?#endif/g,'');
let checks=0;const results=[];
for(const v of [
  {name:'iPhone430',width:430,height:830,dpr:3,ua:'iPhone',platform:'iPhone',touches:5,mobile:true,expected:2},
  {name:'iPad desktop UA',width:1024,height:1366,dpr:2,ua:'Macintosh',platform:'MacIntel',touches:5,mobile:true,expected:Math.sqrt(2400000/(1024*1366))},
  {name:'Mac Retina',width:1200,height:900,dpr:2,ua:'Macintosh',platform:'MacIntel',touches:0,mobile:false,expected:Math.sqrt(2400000/(1200*900))},
  {name:'Desktop DPR1',width:1200,height:900,dpr:1,ua:'Desktop',platform:'Linux',touches:0,mobile:false,expected:1},
  {name:'Android',width:390,height:844,dpr:2.75,ua:'Android',platform:'Linux',touches:5,mobile:true,expected:2},
  {name:'4K CSS viewport',width:3840,height:2160,dpr:2,ua:'Macintosh',platform:'MacIntel',touches:0,mobile:false,expected:1}
]){
  let frameCallbacks=[],resizeCallback,mutationCallback,resizeObserverCallback;
  const canvas={width:Math.round(v.width*v.expected),height:Math.round(v.height*v.expected),dataset:{},style:{},getBoundingClientRect(){return {width:v.width,height:v.height}}};
  const ctx={canvas,container:{className:'unity-desktop'},config:{},navigator:{userAgent:v.ua,platform:v.platform,maxTouchPoints:v.touches},window:{innerWidth:v.width,innerHeight:v.height,devicePixelRatio:v.dpr,addEventListener(name,cb){assert.equal(name,'resize');resizeCallback=cb},requestAnimationFrame(cb){frameCallbacks.push(cb)},ResizeObserver:true},ResizeObserver:class {constructor(cb){resizeObserverCallback=cb}observe(target){assert.equal(target,canvas)}},MutationObserver:class {constructor(cb){mutationCallback=cb}observe(target,options){assert.equal(target,canvas);assert.deepEqual(options.attributeFilter,['width','height'])}}};
  vm.runInNewContext(testScript,ctx);
  assert(Math.abs(ctx.config.devicePixelRatio-v.expected)<1e-10);checks++;
  assert.equal(ctx.isMobileDevice,v.mobile);checks++;
  assert.equal(Number(canvas.dataset.cssWidth),v.width);assert.equal(Number(canvas.dataset.backingWidth),canvas.width);checks+=2;
  resizeCallback();mutationCallback();resizeObserverCallback();assert.equal(frameCallbacks.length,1);checks++;
  canvas.width+=7;frameCallbacks.shift()();assert.equal(Number(canvas.dataset.backingWidth),canvas.width);checks++;
  results.push({scenario:v.name,configuredDpr:ctx.config.devicePixelRatio,mobile:ctx.isMobileDevice,nominalPixels:Math.round(v.width*v.expected)*Math.round(v.height*v.expected)});
}
assert(fs.readFileSync('Assets/WebGLTemplates/PolyRiskResponsive/TemplateData/style.css','utf8').includes('touch-action: none'));checks++;
console.log(JSON.stringify({passed:true,checks,results},null,2));
