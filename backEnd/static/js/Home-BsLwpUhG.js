const __vite__mapDeps=(i,m=__vite__mapDeps,d=(m.f||(m.f=["js/ServerStatus-DPbroWXL.js","js/index-BMnoWLTH.js","js/index-BVYfK-K8.js","js/index-CurZRE90.js","css/index-C6WvNaKL.css","js/status-CZo9rwVA.js","js/ClientInfo-sCFCsd3F.js","js/index-CbIDIgY1.js","js/NodeStats-BCSWKE92.js","js/node-XxXxMLNn.js","js/node-rrHiEOCh.js","js/QuickConfig-B-pPanNC.js","js/configDownload-DBEnllgD.js","js/user-Bfv-Uxkn.js","js/UserStats-5exQ0Ols.js","js/user-CTt0FKav.js"])))=>i.map(i=>d[i]);
import{B as P,b as v,o as i,m as w,i as E,u as R,k as A,C as L,e as d,l as o,v as b,p as n,c,d as C,w as a,S as p,D as f,_ as m,q as D}from"./index-CurZRE90.js";import{s as V,f as $,a as B}from"./index-BVYfK-K8.js";var I=`
    .p-skeleton {
        display: block;
        overflow: hidden;
        background: dt('skeleton.background');
        border-radius: dt('skeleton.border.radius');
    }

    .p-skeleton::after {
        content: '';
        animation: p-skeleton-animation 1.2s infinite;
        height: 100%;
        left: 0;
        position: absolute;
        right: 0;
        top: 0;
        transform: translateX(-100%);
        z-index: 1;
        background: linear-gradient(90deg, rgba(255, 255, 255, 0), dt('skeleton.animation.background'), rgba(255, 255, 255, 0));
    }

    [dir='rtl'] .p-skeleton::after {
        animation-name: p-skeleton-animation-rtl;
    }

    .p-skeleton-circle {
        border-radius: 50%;
    }

    .p-skeleton-animation-none::after {
        animation: none;
    }

    @keyframes p-skeleton-animation {
        from {
            transform: translateX(-100%);
        }
        to {
            transform: translateX(100%);
        }
    }

    @keyframes p-skeleton-animation-rtl {
        from {
            transform: translateX(100%);
        }
        to {
            transform: translateX(-100%);
        }
    }
`,O={root:{position:"relative"}},T={root:function(t){var r=t.props;return["p-skeleton p-component",{"p-skeleton-circle":r.shape==="circle","p-skeleton-animation-none":r.animation==="none"}]}},z=P.extend({name:"skeleton",style:I,classes:T,inlineStyles:O}),N={name:"BaseSkeleton",extends:V,props:{shape:{type:String,default:"rectangle"},size:{type:String,default:null},width:{type:String,default:"100%"},height:{type:String,default:"1rem"},borderRadius:{type:String,default:null},animation:{type:String,default:"wave"}},style:z,provide:function(){return{$pcSkeleton:this,$parentInstance:this}}};function _(e){"@babel/helpers - typeof";return _=typeof Symbol=="function"&&typeof Symbol.iterator=="symbol"?function(t){return typeof t}:function(t){return t&&typeof Symbol=="function"&&t.constructor===Symbol&&t!==Symbol.prototype?"symbol":typeof t},_(e)}function X(e,t,r){return(t=j(t))in e?Object.defineProperty(e,t,{value:r,enumerable:!0,configurable:!0,writable:!0}):e[t]=r,e}function j(e){var t=q(e,"string");return _(t)=="symbol"?t:t+""}function q(e,t){if(_(e)!="object"||!e)return e;var r=e[Symbol.toPrimitive];if(r!==void 0){var l=r.call(e,t);if(_(l)!="object")return l;throw new TypeError("@@toPrimitive must return a primitive value.")}return(t==="string"?String:Number)(e)}var s={name:"Skeleton",extends:N,inheritAttrs:!1,computed:{containerStyle:function(){return this.size?{width:this.size,height:this.size,borderRadius:this.borderRadius}:{width:this.width,height:this.height,borderRadius:this.borderRadius}},dataP:function(){return $(X({},this.shape,this.shape))}}},H=["data-p"];function K(e,t,r,l,y,h){return i(),v("div",w({class:e.cx("root"),style:[e.sx("root"),h.containerStyle],"aria-hidden":"true"},e.ptmi("root"),{"data-p":h.dataP}),null,16,H)}s.render=K;const Q={class:"p-4 mx-auto"},U={class:"flex justify-between items-center mb-6"},F={class:"text-3xl font-bold text-surface-900"},G={class:"text-surface-500 mt-1"},J={class:"grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"},ee=E({__name:"Home",setup(e){const t=f(()=>m(()=>import("./ServerStatus-DPbroWXL.js"),__vite__mapDeps([0,1,2,3,4,5]))),r=f(()=>m(()=>import("./ClientInfo-sCFCsd3F.js"),__vite__mapDeps([6,1,2,3,4,7,5]))),l=f(()=>m(()=>import("./NodeStats-BCSWKE92.js"),__vite__mapDeps([8,1,2,3,4,7,9,10]))),y=f(()=>m(()=>import("./QuickConfig-B-pPanNC.js"),__vite__mapDeps([11,1,2,3,4,9,10,12,13]))),h=f(()=>m(()=>import("./UserStats-5exQ0Ols.js"),__vite__mapDeps([14,1,2,3,4,15,13]))),u=R(),k=D(),g=A(!1);async function S(){g.value=!0;try{await u.logout(),k.push("/login")}finally{g.value=!1}}const x=L(()=>u.user?.last_login_ts?new Date(u.user.last_login_ts*1e3).toLocaleString():"未知");return(M,W)=>(i(),v("div",Q,[d("div",U,[d("div",null,[d("h1",F," 欢迎回来, "+b(n(u).user?.username||"用户"),1),d("p",G,"最后登录: "+b(x.value),1)]),o(n(B),{label:"退出登录",icon:"pi pi-sign-out",severity:"danger",text:"",onClick:S,loading:g.value},null,8,["loading"])]),d("div",J,[(i(),c(p,null,{default:a(()=>[o(n(t))]),fallback:a(()=>[o(n(s),{height:"200px"})]),_:1})),(i(),c(p,null,{default:a(()=>[o(n(r))]),fallback:a(()=>[o(n(s),{height:"200px"})]),_:1})),(i(),c(p,null,{default:a(()=>[o(n(l))]),fallback:a(()=>[o(n(s),{height:"200px"})]),_:1})),(i(),c(p,null,{default:a(()=>[o(n(y))]),fallback:a(()=>[o(n(s),{height:"200px"})]),_:1})),n(u).isAdmin?(i(),c(p,{key:0},{default:a(()=>[o(n(h))]),fallback:a(()=>[o(n(s),{height:"200px"})]),_:1})):C("",!0)])]))}});export{ee as default};
