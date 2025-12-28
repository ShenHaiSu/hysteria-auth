import{B as j,r as $,a as O,c as h,o as i,w as m,b as g,d as w,m as c,e as a,f as v,g as I,n as B,h as V,T as P,i as T,u as C,j as D,k as L,l as f,p as d,q as N,s as _,t as M,v as A}from"./index-VQGNluwy.js";import{s as E,a as q,b as K,m as R}from"./index-UK-kETeX.js";import{s as W}from"./index-C475Ct1q.js";import{f as U,s as F}from"./index-DO8xaQY0.js";import{R as G,s as H}from"./index-osO_gb33.js";import{_ as J}from"./_plugin-vue_export-helper-DlAUqK2U.js";var Q=`
    .p-message {
        display: grid;
        grid-template-rows: 1fr;
        border-radius: dt('message.border.radius');
        outline-width: dt('message.border.width');
        outline-style: solid;
    }

    .p-message-content-wrapper {
        min-height: 0;
    }

    .p-message-content {
        display: flex;
        align-items: center;
        padding: dt('message.content.padding');
        gap: dt('message.content.gap');
    }

    .p-message-icon {
        flex-shrink: 0;
    }

    .p-message-close-button {
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
        margin-inline-start: auto;
        overflow: hidden;
        position: relative;
        width: dt('message.close.button.width');
        height: dt('message.close.button.height');
        border-radius: dt('message.close.button.border.radius');
        background: transparent;
        transition:
            background dt('message.transition.duration'),
            color dt('message.transition.duration'),
            outline-color dt('message.transition.duration'),
            box-shadow dt('message.transition.duration'),
            opacity 0.3s;
        outline-color: transparent;
        color: inherit;
        padding: 0;
        border: none;
        cursor: pointer;
        user-select: none;
    }

    .p-message-close-icon {
        font-size: dt('message.close.icon.size');
        width: dt('message.close.icon.size');
        height: dt('message.close.icon.size');
    }

    .p-message-close-button:focus-visible {
        outline-width: dt('message.close.button.focus.ring.width');
        outline-style: dt('message.close.button.focus.ring.style');
        outline-offset: dt('message.close.button.focus.ring.offset');
    }

    .p-message-info {
        background: dt('message.info.background');
        outline-color: dt('message.info.border.color');
        color: dt('message.info.color');
        box-shadow: dt('message.info.shadow');
    }

    .p-message-info .p-message-close-button:focus-visible {
        outline-color: dt('message.info.close.button.focus.ring.color');
        box-shadow: dt('message.info.close.button.focus.ring.shadow');
    }

    .p-message-info .p-message-close-button:hover {
        background: dt('message.info.close.button.hover.background');
    }

    .p-message-info.p-message-outlined {
        color: dt('message.info.outlined.color');
        outline-color: dt('message.info.outlined.border.color');
    }

    .p-message-info.p-message-simple {
        color: dt('message.info.simple.color');
    }

    .p-message-success {
        background: dt('message.success.background');
        outline-color: dt('message.success.border.color');
        color: dt('message.success.color');
        box-shadow: dt('message.success.shadow');
    }

    .p-message-success .p-message-close-button:focus-visible {
        outline-color: dt('message.success.close.button.focus.ring.color');
        box-shadow: dt('message.success.close.button.focus.ring.shadow');
    }

    .p-message-success .p-message-close-button:hover {
        background: dt('message.success.close.button.hover.background');
    }

    .p-message-success.p-message-outlined {
        color: dt('message.success.outlined.color');
        outline-color: dt('message.success.outlined.border.color');
    }

    .p-message-success.p-message-simple {
        color: dt('message.success.simple.color');
    }

    .p-message-warn {
        background: dt('message.warn.background');
        outline-color: dt('message.warn.border.color');
        color: dt('message.warn.color');
        box-shadow: dt('message.warn.shadow');
    }

    .p-message-warn .p-message-close-button:focus-visible {
        outline-color: dt('message.warn.close.button.focus.ring.color');
        box-shadow: dt('message.warn.close.button.focus.ring.shadow');
    }

    .p-message-warn .p-message-close-button:hover {
        background: dt('message.warn.close.button.hover.background');
    }

    .p-message-warn.p-message-outlined {
        color: dt('message.warn.outlined.color');
        outline-color: dt('message.warn.outlined.border.color');
    }

    .p-message-warn.p-message-simple {
        color: dt('message.warn.simple.color');
    }

    .p-message-error {
        background: dt('message.error.background');
        outline-color: dt('message.error.border.color');
        color: dt('message.error.color');
        box-shadow: dt('message.error.shadow');
    }

    .p-message-error .p-message-close-button:focus-visible {
        outline-color: dt('message.error.close.button.focus.ring.color');
        box-shadow: dt('message.error.close.button.focus.ring.shadow');
    }

    .p-message-error .p-message-close-button:hover {
        background: dt('message.error.close.button.hover.background');
    }

    .p-message-error.p-message-outlined {
        color: dt('message.error.outlined.color');
        outline-color: dt('message.error.outlined.border.color');
    }

    .p-message-error.p-message-simple {
        color: dt('message.error.simple.color');
    }

    .p-message-secondary {
        background: dt('message.secondary.background');
        outline-color: dt('message.secondary.border.color');
        color: dt('message.secondary.color');
        box-shadow: dt('message.secondary.shadow');
    }

    .p-message-secondary .p-message-close-button:focus-visible {
        outline-color: dt('message.secondary.close.button.focus.ring.color');
        box-shadow: dt('message.secondary.close.button.focus.ring.shadow');
    }

    .p-message-secondary .p-message-close-button:hover {
        background: dt('message.secondary.close.button.hover.background');
    }

    .p-message-secondary.p-message-outlined {
        color: dt('message.secondary.outlined.color');
        outline-color: dt('message.secondary.outlined.border.color');
    }

    .p-message-secondary.p-message-simple {
        color: dt('message.secondary.simple.color');
    }

    .p-message-contrast {
        background: dt('message.contrast.background');
        outline-color: dt('message.contrast.border.color');
        color: dt('message.contrast.color');
        box-shadow: dt('message.contrast.shadow');
    }

    .p-message-contrast .p-message-close-button:focus-visible {
        outline-color: dt('message.contrast.close.button.focus.ring.color');
        box-shadow: dt('message.contrast.close.button.focus.ring.shadow');
    }

    .p-message-contrast .p-message-close-button:hover {
        background: dt('message.contrast.close.button.hover.background');
    }

    .p-message-contrast.p-message-outlined {
        color: dt('message.contrast.outlined.color');
        outline-color: dt('message.contrast.outlined.border.color');
    }

    .p-message-contrast.p-message-simple {
        color: dt('message.contrast.simple.color');
    }

    .p-message-text {
        font-size: dt('message.text.font.size');
        font-weight: dt('message.text.font.weight');
    }

    .p-message-icon {
        font-size: dt('message.icon.size');
        width: dt('message.icon.size');
        height: dt('message.icon.size');
    }

    .p-message-sm .p-message-content {
        padding: dt('message.content.sm.padding');
    }

    .p-message-sm .p-message-text {
        font-size: dt('message.text.sm.font.size');
    }

    .p-message-sm .p-message-icon {
        font-size: dt('message.icon.sm.size');
        width: dt('message.icon.sm.size');
        height: dt('message.icon.sm.size');
    }

    .p-message-sm .p-message-close-icon {
        font-size: dt('message.close.icon.sm.size');
        width: dt('message.close.icon.sm.size');
        height: dt('message.close.icon.sm.size');
    }

    .p-message-lg .p-message-content {
        padding: dt('message.content.lg.padding');
    }

    .p-message-lg .p-message-text {
        font-size: dt('message.text.lg.font.size');
    }

    .p-message-lg .p-message-icon {
        font-size: dt('message.icon.lg.size');
        width: dt('message.icon.lg.size');
        height: dt('message.icon.lg.size');
    }

    .p-message-lg .p-message-close-icon {
        font-size: dt('message.close.icon.lg.size');
        width: dt('message.close.icon.lg.size');
        height: dt('message.close.icon.lg.size');
    }

    .p-message-outlined {
        background: transparent;
        outline-width: dt('message.outlined.border.width');
    }

    .p-message-simple {
        background: transparent;
        outline-color: transparent;
        box-shadow: none;
    }

    .p-message-simple .p-message-content {
        padding: dt('message.simple.content.padding');
    }

    .p-message-outlined .p-message-close-button:hover,
    .p-message-simple .p-message-close-button:hover {
        background: transparent;
    }

    .p-message-enter-active {
        animation: p-animate-message-enter 0.3s ease-out forwards;
        overflow: hidden;
    }

    .p-message-leave-active {
        animation: p-animate-message-leave 0.15s ease-in forwards;
        overflow: hidden;
    }

    @keyframes p-animate-message-enter {
        from {
            opacity: 0;
            grid-template-rows: 0fr;
        }
        to {
            opacity: 1;
            grid-template-rows: 1fr;
        }
    }

    @keyframes p-animate-message-leave {
        from {
            opacity: 1;
            grid-template-rows: 1fr;
        }
        to {
            opacity: 0;
            margin: 0;
            grid-template-rows: 0fr;
        }
    }
`,X={root:function(s){var n=s.props;return["p-message p-component p-message-"+n.severity,{"p-message-outlined":n.variant==="outlined","p-message-simple":n.variant==="simple","p-message-sm":n.size==="small","p-message-lg":n.size==="large"}]},contentWrapper:"p-message-content-wrapper",content:"p-message-content",icon:"p-message-icon",text:"p-message-text",closeButton:"p-message-close-button",closeIcon:"p-message-close-icon"},Y=j.extend({name:"message",style:Q,classes:X}),Z={name:"BaseMessage",extends:H,props:{severity:{type:String,default:"info"},closable:{type:Boolean,default:!1},life:{type:Number,default:null},icon:{type:String,default:void 0},closeIcon:{type:String,default:void 0},closeButtonProps:{type:null,default:null},size:{type:String,default:null},variant:{type:String,default:null}},style:Y,provide:function(){return{$pcMessage:this,$parentInstance:this}}};function b(e){"@babel/helpers - typeof";return b=typeof Symbol=="function"&&typeof Symbol.iterator=="symbol"?function(s){return typeof s}:function(s){return s&&typeof Symbol=="function"&&s.constructor===Symbol&&s!==Symbol.prototype?"symbol":typeof s},b(e)}function k(e,s,n){return(s=ee(s))in e?Object.defineProperty(e,s,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[s]=n,e}function ee(e){var s=se(e,"string");return b(s)=="symbol"?s:s+""}function se(e,s){if(b(e)!="object"||!e)return e;var n=e[Symbol.toPrimitive];if(n!==void 0){var o=n.call(e,s);if(b(o)!="object")return o;throw new TypeError("@@toPrimitive must return a primitive value.")}return(s==="string"?String:Number)(e)}var S={name:"Message",extends:Z,inheritAttrs:!1,emits:["close","life-end"],timeout:null,data:function(){return{visible:!0}},mounted:function(){var s=this;this.life&&setTimeout(function(){s.visible=!1,s.$emit("life-end")},this.life)},methods:{close:function(s){this.visible=!1,this.$emit("close",s)}},computed:{closeAriaLabel:function(){return this.$primevue.config.locale.aria?this.$primevue.config.locale.aria.close:void 0},dataP:function(){return U(k(k({outlined:this.variant==="outlined",simple:this.variant==="simple"},this.severity,this.severity),this.size,this.size))}},directives:{ripple:G},components:{TimesIcon:E}};function y(e){"@babel/helpers - typeof";return y=typeof Symbol=="function"&&typeof Symbol.iterator=="symbol"?function(s){return typeof s}:function(s){return s&&typeof Symbol=="function"&&s.constructor===Symbol&&s!==Symbol.prototype?"symbol":typeof s},y(e)}function x(e,s){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var o=Object.getOwnPropertySymbols(e);s&&(o=o.filter(function(l){return Object.getOwnPropertyDescriptor(e,l).enumerable})),n.push.apply(n,o)}return n}function z(e){for(var s=1;s<arguments.length;s++){var n=arguments[s]!=null?arguments[s]:{};s%2?x(Object(n),!0).forEach(function(o){ne(e,o,n[o])}):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):x(Object(n)).forEach(function(o){Object.defineProperty(e,o,Object.getOwnPropertyDescriptor(n,o))})}return e}function ne(e,s,n){return(s=oe(s))in e?Object.defineProperty(e,s,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[s]=n,e}function oe(e){var s=te(e,"string");return y(s)=="symbol"?s:s+""}function te(e,s){if(y(e)!="object"||!e)return e;var n=e[Symbol.toPrimitive];if(n!==void 0){var o=n.call(e,s);if(y(o)!="object")return o;throw new TypeError("@@toPrimitive must return a primitive value.")}return(s==="string"?String:Number)(e)}var ae=["data-p"],re=["data-p"],ie=["data-p"],le=["aria-label","data-p"],ce=["data-p"];function de(e,s,n,o,l,r){var u=$("TimesIcon"),t=O("ripple");return i(),h(P,c({name:"p-message",appear:""},e.ptmi("transition")),{default:m(function(){return[l.visible?(i(),g("div",c({key:0,class:e.cx("root"),role:"alert","aria-live":"assertive","aria-atomic":"true","data-p":r.dataP},e.ptm("root")),[a("div",c({class:e.cx("contentWrapper")},e.ptm("contentWrapper")),[e.$slots.container?v(e.$slots,"container",{key:0,closeCallback:r.close}):(i(),g("div",c({key:1,class:e.cx("content"),"data-p":r.dataP},e.ptm("content")),[v(e.$slots,"icon",{class:B(e.cx("icon"))},function(){return[(i(),h(V(e.icon?"span":null),c({class:[e.cx("icon"),e.icon],"data-p":r.dataP},e.ptm("icon")),null,16,["class","data-p"]))]}),e.$slots.default?(i(),g("div",c({key:0,class:e.cx("text"),"data-p":r.dataP},e.ptm("text")),[v(e.$slots,"default")],16,ie)):w("",!0),e.closable?I((i(),g("button",c({key:1,class:e.cx("closeButton"),"aria-label":r.closeAriaLabel,type:"button",onClick:s[0]||(s[0]=function(p){return r.close(p)}),"data-p":r.dataP},z(z({},e.closeButtonProps),e.ptm("closeButton"))),[v(e.$slots,"closeicon",{},function(){return[e.closeIcon?(i(),g("i",c({key:0,class:[e.cx("closeIcon"),e.closeIcon],"data-p":r.dataP},e.ptm("closeIcon")),null,16,ce)):(i(),h(u,c({key:1,class:[e.cx("closeIcon"),e.closeIcon],"data-p":r.dataP},e.ptm("closeIcon")),null,16,["class","data-p"]))]})],16,le)),[[t]]):w("",!0)],16,re))],16)],16,ae)):w("",!0)]}),_:3},16)}S.render=de;const me={class:"login-container flex items-center justify-center min-h-screen bg-surface-50 dark:bg-surface-950 p-4"},ue={class:"w-full max-w-md md:max-w-lg lg:max-w-xl transition-all duration-300"},ge={class:"flex flex-col gap-2"},pe={class:"relative"},fe={class:"flex flex-col gap-2"},be={class:"relative"},ye=T({__name:"Login",setup(e){const s=N(),n=C(),o=D({username:"",password:""}),l=L("");async function r(){if(!o.username||!o.password){l.value="请输入用户名和密码";return}l.value="";try{await n.login({username:o.username,login_password_md5:R(o.password)}),s.push("/")}catch(u){console.error("Login failed:",u),l.value=u.response?.data?.message||u.message||"登录失败，请检查用户名或密码"}}return(u,t)=>(i(),g("div",me,[a("div",ue,[f(d(W),{class:"shadow-xl border-none overflow-hidden"},{header:m(()=>[...t[2]||(t[2]=[a("div",{class:"h-2 bg-primary-500"},null,-1)])]),title:m(()=>[...t[3]||(t[3]=[a("div",{class:"flex flex-col items-center gap-2 pt-6"},[a("div",{class:"w-16 h-16 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center mb-2"},[a("i",{class:"pi pi-shield text-3xl text-primary-600 dark:text-primary-400"})]),a("h1",{class:"text-2xl font-bold text-surface-900 dark:text-surface-0"},"欢迎回来"),a("p",{class:"text-surface-500 dark:text-surface-400 text-sm"}," 请输入您的凭据以访问控制面板 ")],-1)])]),content:m(()=>[a("form",{onSubmit:_(r,["prevent"]),class:"flex flex-col gap-6 px-2 md:px-6 py-4"},[a("div",ge,[t[5]||(t[5]=a("label",{for:"username",class:"font-medium text-surface-700 dark:text-surface-200"},"用户名",-1)),a("div",pe,[t[4]||(t[4]=a("i",{class:"pi pi-user absolute left-3 top-1/2 -translate-y-1/2 text-surface-400"},null,-1)),f(d(q),{id:"username",modelValue:o.username,"onUpdate:modelValue":t[0]||(t[0]=p=>o.username=p),placeholder:"请输入用户名",class:"w-full pl-10",disabled:d(n).loading,required:""},null,8,["modelValue","disabled"])])]),a("div",fe,[t[8]||(t[8]=a("label",{for:"password",class:"font-medium text-surface-700 dark:text-surface-200"},"密码",-1)),a("div",be,[f(d(K),{id:"password",modelValue:o.password,"onUpdate:modelValue":t[1]||(t[1]=p=>o.password=p),placeholder:"请输入密码",class:"w-full","input-class":"w-full pl-10",feedback:!1,"toggle-mask":"",disabled:d(n).loading,required:""},{header:m(()=>[...t[6]||(t[6]=[a("i",{class:"pi pi-lock absolute left-3 top-1/2 -translate-y-1/2 text-surface-400 z-10"},null,-1)])]),_:1},8,["modelValue","disabled"]),t[7]||(t[7]=a("i",{class:"pi pi-lock absolute left-3 top-1/2 -translate-y-1/2 text-surface-400 z-10 pointer-events-none"},null,-1))])]),f(P,{name:"p-message"},{default:m(()=>[l.value?(i(),h(d(S),{key:0,severity:"error",variant:"simple",size:"small",class:"mt-2"},{default:m(()=>[M(A(l.value),1)]),_:1})):w("",!0)]),_:1}),f(d(F),{type:"submit",label:"登录",icon:"pi pi-sign-in",class:"w-full mt-2",loading:d(n).loading},null,8,["loading"])],32)]),_:1})])]))}}),Pe=J(ye,[["__scopeId","data-v-073b0c06"]]);export{Pe as default};
