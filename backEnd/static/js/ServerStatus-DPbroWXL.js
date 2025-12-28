import{s as w}from"./index-BMnoWLTH.js";import{s as _,f as B}from"./index-BVYfK-K8.js";import{B as S,b as d,o,d as b,m as c,f as P,t as M,v as i,i as $,k as g,C as j,H as V,c as z,w as h,e,l as C,p as y}from"./index-CurZRE90.js";import{g as N}from"./status-CZo9rwVA.js";var U=`
    .p-progressbar {
        display: block;
        position: relative;
        overflow: hidden;
        height: dt('progressbar.height');
        background: dt('progressbar.background');
        border-radius: dt('progressbar.border.radius');
    }

    .p-progressbar-value {
        margin: 0;
        background: dt('progressbar.value.background');
    }

    .p-progressbar-label {
        color: dt('progressbar.label.color');
        font-size: dt('progressbar.label.font.size');
        font-weight: dt('progressbar.label.font.weight');
    }

    .p-progressbar-determinate .p-progressbar-value {
        height: 100%;
        width: 0%;
        position: absolute;
        display: none;
        display: flex;
        align-items: center;
        justify-content: center;
        overflow: hidden;
        transition: width 1s ease-in-out;
    }

    .p-progressbar-determinate .p-progressbar-label {
        display: inline-flex;
    }

    .p-progressbar-indeterminate .p-progressbar-value::before {
        content: '';
        position: absolute;
        background: inherit;
        inset-block-start: 0;
        inset-inline-start: 0;
        inset-block-end: 0;
        will-change: inset-inline-start, inset-inline-end;
        animation: p-progressbar-indeterminate-anim 2.1s cubic-bezier(0.65, 0.815, 0.735, 0.395) infinite;
    }

    .p-progressbar-indeterminate .p-progressbar-value::after {
        content: '';
        position: absolute;
        background: inherit;
        inset-block-start: 0;
        inset-inline-start: 0;
        inset-block-end: 0;
        will-change: inset-inline-start, inset-inline-end;
        animation: p-progressbar-indeterminate-anim-short 2.1s cubic-bezier(0.165, 0.84, 0.44, 1) infinite;
        animation-delay: 1.15s;
    }

    @keyframes p-progressbar-indeterminate-anim {
        0% {
            inset-inline-start: -35%;
            inset-inline-end: 100%;
        }
        60% {
            inset-inline-start: 100%;
            inset-inline-end: -90%;
        }
        100% {
            inset-inline-start: 100%;
            inset-inline-end: -90%;
        }
    }
    @-webkit-keyframes p-progressbar-indeterminate-anim {
        0% {
            inset-inline-start: -35%;
            inset-inline-end: 100%;
        }
        60% {
            inset-inline-start: 100%;
            inset-inline-end: -90%;
        }
        100% {
            inset-inline-start: 100%;
            inset-inline-end: -90%;
        }
    }

    @keyframes p-progressbar-indeterminate-anim-short {
        0% {
            inset-inline-start: -200%;
            inset-inline-end: 100%;
        }
        60% {
            inset-inline-start: 107%;
            inset-inline-end: -8%;
        }
        100% {
            inset-inline-start: 107%;
            inset-inline-end: -8%;
        }
    }
    @-webkit-keyframes p-progressbar-indeterminate-anim-short {
        0% {
            inset-inline-start: -200%;
            inset-inline-end: 100%;
        }
        60% {
            inset-inline-start: 107%;
            inset-inline-end: -8%;
        }
        100% {
            inset-inline-start: 107%;
            inset-inline-end: -8%;
        }
    }
`,F={root:function(s){var l=s.instance;return["p-progressbar p-component",{"p-progressbar-determinate":l.determinate,"p-progressbar-indeterminate":l.indeterminate}]},value:"p-progressbar-value",label:"p-progressbar-label"},T=S.extend({name:"progressbar",style:U,classes:F}),A={name:"BaseProgressBar",extends:_,props:{value:{type:Number,default:null},mode:{type:String,default:"determinate"},showValue:{type:Boolean,default:!0}},style:T,provide:function(){return{$pcProgressBar:this,$parentInstance:this}}},x={name:"ProgressBar",extends:A,inheritAttrs:!1,computed:{progressStyle:function(){return{width:this.value+"%",display:"flex"}},indeterminate:function(){return this.mode==="indeterminate"},determinate:function(){return this.mode==="determinate"},dataP:function(){return B({determinate:this.determinate,indeterminate:this.indeterminate})}}},D=["aria-valuenow","data-p"],E=["data-p"],G=["data-p"],H=["data-p"];function I(n,s,l,f,v,r){return o(),d("div",c({role:"progressbar",class:n.cx("root"),"aria-valuemin":"0","aria-valuenow":n.value,"aria-valuemax":"100","data-p":r.dataP},n.ptmi("root")),[r.determinate?(o(),d("div",c({key:0,class:n.cx("value"),style:r.progressStyle,"data-p":r.dataP},n.ptm("value")),[n.value!=null&&n.value!==0&&n.showValue?(o(),d("div",c({key:0,class:n.cx("label"),"data-p":r.dataP},n.ptm("label")),[P(n.$slots,"default",{},function(){return[M(i(n.value+"%"),1)]})],16,G)):b("",!0)],16,E)):r.indeterminate?(o(),d("div",c({key:1,class:n.cx("value"),"data-p":r.dataP},n.ptm("value")),null,16,H)):b("",!0)],16,D)}x.render=I;const K={key:0,class:"flex justify-center p-4"},q={key:1,class:"space-y-3"},J={class:"flex justify-between items-center"},L={class:"font-medium"},O={class:"flex justify-between items-center"},Q={class:"font-medium"},R={class:"flex justify-between items-center"},W={class:"font-medium"},X={class:"flex justify-between items-center"},Y={class:"font-medium text-right text-xs"},Z={class:"flex flex-col gap-1"},ee={class:"flex justify-between text-sm"},ne={class:"flex justify-between text-xs text-surface-400"},ie=$({__name:"ServerStatus",setup(n){const s=g(null),l=g(!0),f=j(()=>{if(!s.value)return 0;const a=s.value.memory.total_bytes-s.value.memory.free_bytes;return Math.round(a/s.value.memory.total_bytes*100)});function v(a){if(a===0)return"0 B";const t=1024,p=["B","KB","MB","GB","TB"],u=Math.floor(Math.log(a)/Math.log(t));return parseFloat((a/Math.pow(t,u)).toFixed(2))+" "+p[u]}function r(a){const t=Math.floor(a/86400),p=Math.floor(a%(24*3600)/3600),u=Math.floor(a%3600/60),m=[];return t>0&&m.push(`${t}天`),p>0&&m.push(`${p}小时`),u>0&&m.push(`${u}分钟`),m.join(" ")||"< 1分钟"}async function k(){l.value=!0;try{s.value=await N()}catch(a){console.error("获取服务器状态失败:",a)}finally{l.value=!1}}return V(()=>{k()}),(a,t)=>(o(),z(y(w),{class:"h-full"},{title:h(()=>[...t[0]||(t[0]=[e("div",{class:"flex items-center gap-2"},[e("i",{class:"pi pi-server text-primary"}),e("span",null,"服务器状态")],-1)])]),content:h(()=>[l.value?(o(),d("div",K,[...t[1]||(t[1]=[e("i",{class:"pi pi-spin pi-spinner text-2xl"},null,-1)])])):s.value?(o(),d("div",q,[e("div",J,[t[2]||(t[2]=e("span",{class:"text-surface-500"},"主机名",-1)),e("span",L,i(s.value.os.hostname),1)]),e("div",O,[t[3]||(t[3]=e("span",{class:"text-surface-500"},"操作系统",-1)),e("span",Q,i(s.value.os.platform),1)]),e("div",R,[t[4]||(t[4]=e("span",{class:"text-surface-500"},"运行时间",-1)),e("span",W,i(r(s.value.os.uptime_sec)),1)]),e("div",X,[t[5]||(t[5]=e("span",{class:"text-surface-500"},"CPU",-1)),e("span",Y,i(s.value.cpu.model)+" ("+i(s.value.cpu.count)+"核)",1)]),e("div",Z,[e("div",ee,[t[6]||(t[6]=e("span",{class:"text-surface-500"},"内存使用率",-1)),e("span",null,i(f.value)+"%",1)]),C(y(x),{value:f.value,"show-value":!1,style:{height:"6px"}},null,8,["value"]),e("div",ne,[e("span",null,i(v(s.value.memory.total_bytes-s.value.memory.free_bytes)),1),e("span",null,i(v(s.value.memory.total_bytes)),1)])])])):b("",!0)]),_:1}))}});export{ie as default};
