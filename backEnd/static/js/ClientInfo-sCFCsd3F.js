import{s as x}from"./index-BMnoWLTH.js";import{s as f}from"./index-CbIDIgY1.js";import{s as _,f as k}from"./index-BVYfK-K8.js";import{B as S,b as p,o,d as b,m as y,f as z,i as P,k as m,H as w,c as v,w as g,e as n,l as $,v as s,p as d}from"./index-CurZRE90.js";import{a as j}from"./status-CZo9rwVA.js";var B=`
    .p-divider-horizontal {
        display: flex;
        width: 100%;
        position: relative;
        align-items: center;
        margin: dt('divider.horizontal.margin');
        padding: dt('divider.horizontal.padding');
    }

    .p-divider-horizontal:before {
        position: absolute;
        display: block;
        inset-block-start: 50%;
        inset-inline-start: 0;
        width: 100%;
        content: '';
        border-block-start: 1px solid dt('divider.border.color');
    }

    .p-divider-horizontal .p-divider-content {
        padding: dt('divider.horizontal.content.padding');
    }

    .p-divider-vertical {
        min-height: 100%;
        display: flex;
        position: relative;
        justify-content: center;
        margin: dt('divider.vertical.margin');
        padding: dt('divider.vertical.padding');
    }

    .p-divider-vertical:before {
        position: absolute;
        display: block;
        inset-block-start: 0;
        inset-inline-start: 50%;
        height: 100%;
        content: '';
        border-inline-start: 1px solid dt('divider.border.color');
    }

    .p-divider.p-divider-vertical .p-divider-content {
        padding: dt('divider.vertical.content.padding');
    }

    .p-divider-content {
        z-index: 1;
        background: dt('divider.content.background');
        color: dt('divider.content.color');
    }

    .p-divider-solid.p-divider-horizontal:before {
        border-block-start-style: solid;
    }

    .p-divider-solid.p-divider-vertical:before {
        border-inline-start-style: solid;
    }

    .p-divider-dashed.p-divider-horizontal:before {
        border-block-start-style: dashed;
    }

    .p-divider-dashed.p-divider-vertical:before {
        border-inline-start-style: dashed;
    }

    .p-divider-dotted.p-divider-horizontal:before {
        border-block-start-style: dotted;
    }

    .p-divider-dotted.p-divider-vertical:before {
        border-inline-start-style: dotted;
    }

    .p-divider-left:dir(rtl),
    .p-divider-right:dir(rtl) {
        flex-direction: row-reverse;
    }
`,C={root:function(t){var e=t.props;return{justifyContent:e.layout==="horizontal"?e.align==="center"||e.align===null?"center":e.align==="left"?"flex-start":e.align==="right"?"flex-end":null:null,alignItems:e.layout==="vertical"?e.align==="center"||e.align===null?"center":e.align==="top"?"flex-start":e.align==="bottom"?"flex-end":null:null}}},I={root:function(t){var e=t.props;return["p-divider p-component","p-divider-"+e.layout,"p-divider-"+e.type,{"p-divider-left":e.layout==="horizontal"&&(!e.align||e.align==="left")},{"p-divider-center":e.layout==="horizontal"&&e.align==="center"},{"p-divider-right":e.layout==="horizontal"&&e.align==="right"},{"p-divider-top":e.layout==="vertical"&&e.align==="top"},{"p-divider-center":e.layout==="vertical"&&(!e.align||e.align==="center")},{"p-divider-bottom":e.layout==="vertical"&&e.align==="bottom"}]},content:"p-divider-content"},D=S.extend({name:"divider",style:B,classes:I,inlineStyles:C}),N={name:"BaseDivider",extends:_,props:{align:{type:String,default:null},layout:{type:String,default:"horizontal"},type:{type:String,default:"solid"}},style:D,provide:function(){return{$pcDivider:this,$parentInstance:this}}};function l(i){"@babel/helpers - typeof";return l=typeof Symbol=="function"&&typeof Symbol.iterator=="symbol"?function(t){return typeof t}:function(t){return t&&typeof Symbol=="function"&&t.constructor===Symbol&&t!==Symbol.prototype?"symbol":typeof t},l(i)}function u(i,t,e){return(t=V(t))in i?Object.defineProperty(i,t,{value:e,enumerable:!0,configurable:!0,writable:!0}):i[t]=e,i}function V(i){var t=A(i,"string");return l(t)=="symbol"?t:t+""}function A(i,t){if(l(i)!="object"||!i)return i;var e=i[Symbol.toPrimitive];if(e!==void 0){var a=e.call(i,t);if(l(a)!="object")return a;throw new TypeError("@@toPrimitive must return a primitive value.")}return(t==="string"?String:Number)(i)}var h={name:"Divider",extends:N,inheritAttrs:!1,computed:{dataP:function(){return k(u(u(u({},this.align,this.align),this.layout,this.layout),this.type,this.type))}}},E=["aria-orientation","data-p"],H=["data-p"];function K(i,t,e,a,c,r){return o(),p("div",y({class:i.cx("root"),style:i.sx("root"),role:"separator","aria-orientation":i.layout,"data-p":r.dataP},i.ptmi("root")),[i.$slots.default?(o(),p("div",y({key:0,class:i.cx("content"),"data-p":r.dataP},i.ptm("content")),[z(i.$slots,"default")],16,H)):b("",!0)],16,E)}h.render=K;const L={key:0,class:"flex justify-center p-4"},M={key:1,class:"space-y-3"},O={class:"flex flex-col gap-1"},T={class:"flex items-center gap-2"},U={class:"font-mono font-bold text-lg text-primary"},q={class:"flex flex-col gap-1"},F={class:"text-xs break-all text-surface-600 leading-relaxed bg-surface-50 p-2 rounded"},G={class:"flex justify-between items-center text-sm"},J={class:"font-medium"},Q={class:"flex justify-between items-center text-sm"},R={class:"font-medium"},te=P({__name:"ClientInfo",setup(i){const t=m(null),e=m(!0);async function a(){e.value=!0;try{t.value=await j()}catch(c){console.error("获取客户端信息失败:",c)}finally{e.value=!1}}return w(()=>{a()}),(c,r)=>(o(),v(d(x),{class:"h-full"},{title:g(()=>[...r[0]||(r[0]=[n("div",{class:"flex items-center gap-2"},[n("i",{class:"pi pi-info-circle text-primary"}),n("span",null,"访问信息")],-1)])]),content:g(()=>[e.value?(o(),p("div",L,[...r[1]||(r[1]=[n("i",{class:"pi pi-spin pi-spinner text-2xl"},null,-1)])])):t.value?(o(),p("div",M,[n("div",O,[r[2]||(r[2]=n("span",{class:"text-surface-500 text-sm"},"您的出口 IP",-1)),n("div",T,[n("span",U,s(t.value.ip),1),t.value.ip.includes(":")?(o(),v(d(f),{key:1,severity:"info",value:"IPv6"})):(o(),v(d(f),{key:0,severity:"info",value:"IPv4"}))])]),$(d(h)),n("div",q,[r[3]||(r[3]=n("span",{class:"text-surface-500 text-sm"},"浏览器 User-Agent",-1)),n("p",F,s(t.value.ua),1)]),n("div",G,[r[4]||(r[4]=n("span",{class:"text-surface-500"},"请求来源",-1)),n("span",J,s(t.value.origin||"直接访问"),1)]),n("div",Q,[r[5]||(r[5]=n("span",{class:"text-surface-500"},"请求时间",-1)),n("span",R,s(new Date(t.value.time.now_iso).toLocaleString()),1)])])):b("",!0)]),_:1}))}});export{te as default};
