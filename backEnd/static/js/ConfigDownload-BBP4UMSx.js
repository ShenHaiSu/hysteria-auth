import{i as b,u as $,H as V,b as c,e as s,d as z,l,p as a,E as x,o as p,B as C,m as y,G as S}from"./index-CurZRE90.js";import{u as _}from"./configDownload-DBEnllgD.js";import{u as I}from"./user-CTt0FKav.js";import{a as g,d as P}from"./index-9NuiK9eO.js";import{c as v,b as U}from"./index-Dy6SO-wM.js";import{a as h,f as B}from"./index-BVYfK-K8.js";import{_ as w}from"./_plugin-vue_export-helper-DlAUqK2U.js";import"./node-rrHiEOCh.js";import"./user-Bfv-Uxkn.js";const T={class:"bg-surface-0 dark:bg-surface-900 p-4 rounded-lg shadow-sm mb-4 border border-surface-200 dark:border-surface-700"},A={class:"flex flex-wrap gap-4 items-end"},D={class:"flex flex-col gap-2"},H={class:"flex flex-col gap-2"},R={class:"flex flex-col gap-2"},K={class:"flex flex-col gap-2"},j={key:0,class:"flex flex-col gap-2"},E={class:"flex items-center gap-2 ml-auto"},F=b({__name:"ConfigFilter",setup(t){const e=_(),r=$(),i=I(),f=[{label:"已启用",value:1},{label:"已禁用",value:0}];V(async()=>{if(r.isAdmin)try{await i.fetchUsers()}catch(d){console.error("加载用户列表失败:",d)}});function o(){e.generateConfig()}return(d,n)=>(p(),c("div",T,[s("div",A,[s("div",D,[n[5]||(n[5]=s("label",{for:"server_group",class:"text-sm font-medium text-surface-600 dark:text-surface-400"},"节点分组",-1)),l(a(g),{id:"server_group",modelValue:a(e).filters.server_group,"onUpdate:modelValue":n[0]||(n[0]=u=>a(e).filters.server_group=u),placeholder:"搜索分组...",class:"w-full md:w-48",onKeyup:x(o,["enter"])},null,8,["modelValue"])]),s("div",H,[n[6]||(n[6]=s("label",{for:"ip_address",class:"text-sm font-medium text-surface-600 dark:text-surface-400"},"IP 地址",-1)),l(a(g),{id:"ip_address",modelValue:a(e).filters.ip_address,"onUpdate:modelValue":n[1]||(n[1]=u=>a(e).filters.ip_address=u),placeholder:"搜索 IP...",class:"w-full md:w-48",onKeyup:x(o,["enter"])},null,8,["modelValue"])]),s("div",R,[n[7]||(n[7]=s("label",{for:"domain",class:"text-sm font-medium text-surface-600 dark:text-surface-400"},"域名",-1)),l(a(g),{id:"domain",modelValue:a(e).filters.domain,"onUpdate:modelValue":n[2]||(n[2]=u=>a(e).filters.domain=u),placeholder:"搜索域名...",class:"w-full md:w-48",onKeyup:x(o,["enter"])},null,8,["modelValue"])]),s("div",K,[n[8]||(n[8]=s("label",{for:"is_active",class:"text-sm font-medium text-surface-600 dark:text-surface-400"},"启用状态",-1)),l(a(v),{id:"is_active",modelValue:a(e).filters.is_active,"onUpdate:modelValue":n[3]||(n[3]=u=>a(e).filters.is_active=u),options:f,optionLabel:"label",optionValue:"value",placeholder:"全部状态",class:"w-full md:w-48",showClear:"",onChange:o},null,8,["modelValue"])]),a(r).isAdmin?(p(),c("div",j,[n[9]||(n[9]=s("label",{for:"target-user",class:"text-sm font-medium text-surface-600 dark:text-surface-400"},"目标用户",-1)),l(a(v),{id:"target-user",modelValue:a(e).targetUserId,"onUpdate:modelValue":n[4]||(n[4]=u=>a(e).targetUserId=u),options:a(i).users,optionLabel:"username",optionValue:"id",placeholder:"选择用户 (默认为自己)",class:"w-full md:w-48",showClear:"",loading:a(i).loading,onChange:o},null,8,["modelValue","options","loading"])])):z("",!0),s("div",E,[l(a(h),{label:"重置",icon:"pi pi-refresh",severity:"secondary",onClick:a(e).resetFilters},null,8,["onClick"]),l(a(h),{label:"生成配置",icon:"pi pi-cog",loading:a(e).loading,onClick:o,class:"p-button-primary"},null,8,["loading"])])])]))}});var N=`
    .p-textarea {
        font-family: inherit;
        font-feature-settings: inherit;
        font-size: 1rem;
        color: dt('textarea.color');
        background: dt('textarea.background');
        padding-block: dt('textarea.padding.y');
        padding-inline: dt('textarea.padding.x');
        border: 1px solid dt('textarea.border.color');
        transition:
            background dt('textarea.transition.duration'),
            color dt('textarea.transition.duration'),
            border-color dt('textarea.transition.duration'),
            outline-color dt('textarea.transition.duration'),
            box-shadow dt('textarea.transition.duration');
        appearance: none;
        border-radius: dt('textarea.border.radius');
        outline-color: transparent;
        box-shadow: dt('textarea.shadow');
    }

    .p-textarea:enabled:hover {
        border-color: dt('textarea.hover.border.color');
    }

    .p-textarea:enabled:focus {
        border-color: dt('textarea.focus.border.color');
        box-shadow: dt('textarea.focus.ring.shadow');
        outline: dt('textarea.focus.ring.width') dt('textarea.focus.ring.style') dt('textarea.focus.ring.color');
        outline-offset: dt('textarea.focus.ring.offset');
    }

    .p-textarea.p-invalid {
        border-color: dt('textarea.invalid.border.color');
    }

    .p-textarea.p-variant-filled {
        background: dt('textarea.filled.background');
    }

    .p-textarea.p-variant-filled:enabled:hover {
        background: dt('textarea.filled.hover.background');
    }

    .p-textarea.p-variant-filled:enabled:focus {
        background: dt('textarea.filled.focus.background');
    }

    .p-textarea:disabled {
        opacity: 1;
        background: dt('textarea.disabled.background');
        color: dt('textarea.disabled.color');
    }

    .p-textarea::placeholder {
        color: dt('textarea.placeholder.color');
    }

    .p-textarea.p-invalid::placeholder {
        color: dt('textarea.invalid.placeholder.color');
    }

    .p-textarea-fluid {
        width: 100%;
    }

    .p-textarea-resizable {
        overflow: hidden;
        resize: none;
    }

    .p-textarea-sm {
        font-size: dt('textarea.sm.font.size');
        padding-block: dt('textarea.sm.padding.y');
        padding-inline: dt('textarea.sm.padding.x');
    }

    .p-textarea-lg {
        font-size: dt('textarea.lg.font.size');
        padding-block: dt('textarea.lg.padding.y');
        padding-inline: dt('textarea.lg.padding.x');
    }
`,O={root:function(e){var r=e.instance,i=e.props;return["p-textarea p-component",{"p-filled":r.$filled,"p-textarea-resizable ":i.autoResize,"p-textarea-sm p-inputfield-sm":i.size==="small","p-textarea-lg p-inputfield-lg":i.size==="large","p-invalid":r.$invalid,"p-variant-filled":r.$variant==="filled","p-textarea-fluid":r.$fluid}]}},L=C.extend({name:"textarea",style:N,classes:O}),q={name:"BaseTextarea",extends:P,props:{autoResize:Boolean},style:L,provide:function(){return{$pcTextarea:this,$parentInstance:this}}};function m(t){"@babel/helpers - typeof";return m=typeof Symbol=="function"&&typeof Symbol.iterator=="symbol"?function(e){return typeof e}:function(e){return e&&typeof Symbol=="function"&&e.constructor===Symbol&&e!==Symbol.prototype?"symbol":typeof e},m(t)}function G(t,e,r){return(e=M(e))in t?Object.defineProperty(t,e,{value:r,enumerable:!0,configurable:!0,writable:!0}):t[e]=r,t}function M(t){var e=J(t,"string");return m(e)=="symbol"?e:e+""}function J(t,e){if(m(t)!="object"||!t)return t;var r=t[Symbol.toPrimitive];if(r!==void 0){var i=r.call(t,e);if(m(i)!="object")return i;throw new TypeError("@@toPrimitive must return a primitive value.")}return(e==="string"?String:Number)(t)}var k={name:"Textarea",extends:q,inheritAttrs:!1,observer:null,mounted:function(){var e=this;this.autoResize&&(this.observer=new ResizeObserver(function(){requestAnimationFrame(function(){e.resize()})}),this.observer.observe(this.$el))},updated:function(){this.autoResize&&this.resize()},beforeUnmount:function(){this.observer&&this.observer.disconnect()},methods:{resize:function(){if(this.$el.offsetParent){var e=this.$el.style.height,r=parseInt(e)||0,i=this.$el.scrollHeight,f=!r||i>r,o=r&&i<r;o?(this.$el.style.height="auto",this.$el.style.height="".concat(this.$el.scrollHeight,"px")):f&&(this.$el.style.height="".concat(i,"px"))}},onInput:function(e){this.autoResize&&this.resize(),this.writeValue(e.target.value,e)}},computed:{attrs:function(){return y(this.ptmi("root",{context:{filled:this.$filled,disabled:this.disabled}}),this.formField)},dataP:function(){return B(G({invalid:this.$invalid,fluid:this.$fluid,filled:this.$variant==="filled"},this.size,this.size))}}},Q=["value","name","disabled","aria-invalid","data-p"];function W(t,e,r,i,f,o){return p(),c("textarea",y({class:t.cx("root"),value:t.d_value,name:t.name,disabled:t.disabled,"aria-invalid":t.invalid||void 0,"data-p":o.dataP,onInput:e[0]||(e[0]=function(){return o.onInput&&o.onInput.apply(o,arguments)})},o.attrs),null,16,Q)}k.render=W;const X={class:"mt-6"},Y=b({__name:"ConfigDisplay",setup(t){const e=_(),r=S(),i=o=>{const d=o.target;d&&e.generatedConfig&&d.select()},f=async()=>{if(e.generatedConfig)try{await navigator.clipboard.writeText(e.generatedConfig),r.add({severity:"success",summary:"复制成功",detail:"配置已复制到剪贴板",life:3e3})}catch{r.add({severity:"error",summary:"复制失败",detail:"请手动复制内容",life:3e3})}};return(o,d)=>(p(),c("div",X,[d[1]||(d[1]=s("div",{class:"flex items-center justify-between mb-2"},[s("h3",{class:"text-lg font-semibold"},"生成的配置内容"),s("span",{class:"text-xs text-surface-500 italic"},"单击全选, 双击复制")],-1)),l(a(k),{modelValue:a(e).generatedConfig,"onUpdate:modelValue":d[0]||(d[0]=n=>a(e).generatedConfig=n),readonly:"",rows:"15",class:"w-full font-mono text-sm p-4 bg-surface-50 dark:bg-surface-950",placeholder:"点击上方生成按钮获取配置...",onClick:i,onDblclick:f},null,8,["modelValue"])]))}}),Z=w(Y,[["__scopeId","data-v-45cd8203"]]),ee={class:"config-download p-4"},te=b({__name:"ConfigDownload",setup(t){return(e,r)=>(p(),c("div",ee,[r[0]||(r[0]=s("div",{class:"mb-6"},[s("h1",{class:"text-2xl font-bold"},"代理配置下载")],-1)),l(F),l(Z),l(a(U))]))}}),fe=w(te,[["__scopeId","data-v-fae2ecb5"]]);export{fe as default};
