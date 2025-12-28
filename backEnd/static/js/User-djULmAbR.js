import{i as V,k as y,b as m,e,l as r,p as a,w as v,o as g,E as H,G as E,g as T,t as F,v as x,d as S,a as J,F as Q,A as W,H as P,I as X,c as I,B as j,f as z,m as C,J as B,n as L,K as Y,L as Z}from"./index-CurZRE90.js";import{a as _,s as ee,f as te}from"./index-BVYfK-K8.js";import{a as U,b as se}from"./index-9NuiK9eO.js";import{s as oe,a as ae,b as le}from"./index-Dy6SO-wM.js";import{s as ie,a as k,b as ne,c as re,d as M,e as de}from"./index-BOg_vN7t.js";import{s as D}from"./index-CbIDIgY1.js";import{s as pe,m as ue}from"./index-BqUPPSvS.js";import{_ as O}from"./_plugin-vue_export-helper-DlAUqK2U.js";import{u as ce}from"./user-CTt0FKav.js";import"./user-Bfv-Uxkn.js";const ge={class:"bg-surface-0 dark:bg-surface-900 p-4 rounded-lg shadow-sm mb-4 border border-surface-200 dark:border-surface-700"},me={class:"flex flex-wrap items-end gap-4"},fe={class:"flex flex-col gap-2"},he={class:"flex items-center gap-2 ml-auto"},be=V({__name:"UserToolbar",props:{initialSearch:{}},emits:["search","add"],setup(o,{emit:i}){const l=o,u=i,s=y(l.initialSearch||""),t=()=>{u("search",s.value)},d=()=>{s.value="",u("search","")};return(h,p)=>(g(),m("div",ge,[e("div",me,[e("div",fe,[p[2]||(p[2]=e("label",{for:"user_search",class:"text-sm font-medium text-surface-600 dark:text-surface-400"},"用户搜索",-1)),r(a(oe),{iconPosition:"left"},{default:v(()=>[r(a(ae),{class:"pi pi-search"}),r(a(U),{id:"user_search",modelValue:s.value,"onUpdate:modelValue":p[0]||(p[0]=w=>s.value=w),placeholder:"搜索用户名或邮箱...",class:"w-full md:w-64",onKeyup:H(t,["enter"])},null,8,["modelValue"])]),_:1})]),e("div",he,[r(a(_),{label:"重置",icon:"pi pi-refresh",severity:"secondary",onClick:d}),r(a(_),{label:"查询",icon:"pi pi-search",onClick:t}),r(a(_),{label:"新增用户",icon:"pi pi-plus",severity:"primary",onClick:p[1]||(p[1]=w=>h.$emit("add"))})])])]))}}),ve={class:"bg-surface-0 dark:bg-surface-900 rounded-lg shadow-sm border border-surface-200 dark:border-surface-700 overflow-hidden"},we=["onClick"],xe={class:"flex flex-col gap-1"},ye={key:0,class:"flex items-center gap-1"},ke={class:"flex items-center gap-1"},_e={class:"text-500"},$e={class:"flex gap-2"},Ve=V({__name:"UserTableDesktop",props:{users:{},loading:{type:Boolean}},emits:["edit","delete"],setup(o){const i=E(),l=async u=>{try{await navigator.clipboard.writeText(u),i.add({severity:"success",summary:"复制成功",detail:"代理密码已复制到剪切板",life:1e3})}catch(s){console.error("无法复制文本: ",s),i.add({severity:"error",summary:"复制失败",detail:"请手动复制",life:1e3})}};return(u,s)=>{const t=J("tooltip");return g(),m("div",ve,[r(a(ie),{value:o.users,loading:o.loading,dataKey:"id",responsiveLayout:"scroll",stripedRows:"",removableSort:"",rowHover:"",class:"p-datatable-sm",pt:{column:{headerCell:{class:"bg-surface-50 dark:bg-surface-800/50 text-surface-700 dark:text-surface-0/70 font-semibold py-3 border-b border-surface-200 dark:border-surface-700"},bodyCell:{class:"py-3 border-b border-surface-100 dark:border-surface-800"}}}},{empty:v(()=>[...s[0]||(s[0]=[e("div",{class:"flex flex-col items-center justify-center py-8 text-surface-500 dark:text-surface-400"},[e("i",{class:"pi pi-inbox text-4xl mb-2"}),e("span",null,"未找到用户信息")],-1)])]),loading:v(()=>[...s[1]||(s[1]=[F(" 正在加载用户数据，请稍候... ",-1)])]),default:v(()=>[r(a(k),{field:"id",header:"ID",sortable:"",style:{width:"5rem"}}),r(a(k),{field:"username",header:"用户名",sortable:""}),r(a(k),{field:"email",header:"邮箱",sortable:""}),r(a(k),{field:"permission",header:"权限"},{body:v(d=>[r(a(D),{value:d.data.permission==="admin"?"管理":"用户",severity:d.data.permission==="admin"?"danger":"info"},null,8,["value","severity"])]),_:1}),r(a(k),{field:"proxy_password",header:"代理密码"},{body:v(d=>[T((g(),m("code",{class:"text-primary font-bold cursor-pointer hover:underline",onClick:h=>l(d.data.proxy_password)},[F(x(d.data.proxy_password),1)],8,we)),[[t,"点击复制",void 0,{top:!0}]])]),_:1}),r(a(k),{field:"formatted_proxy_expire_at",header:"代理到期",sortable:""}),r(a(k),{field:"status_label",header:"状态"},{body:v(d=>[r(a(D),{value:d.data.status_label,severity:d.data.is_active?"success":"secondary"},null,8,["value","severity"])]),_:1}),r(a(k),{header:"登录信息",class:"text-sm"},{body:v(d=>[e("div",xe,[d.data.last_login_ip?(g(),m("div",ye,[s[2]||(s[2]=e("i",{class:"pi pi-map-marker text-xs text-500"},null,-1)),e("span",null,x(d.data.last_login_ip),1)])):S("",!0),e("div",ke,[s[3]||(s[3]=e("i",{class:"pi pi-clock text-xs text-500"},null,-1)),e("span",_e,x(d.data.formatted_last_login_at),1)])])]),_:1}),r(a(k),{field:"formatted_created_at",header:"创建时间",sortable:""}),r(a(k),{header:"操作",style:{"min-width":"8rem"}},{body:v(d=>[e("div",$e,[T(r(a(_),{icon:"pi pi-pencil",outlined:"",rounded:"",severity:"warn",onClick:h=>u.$emit("edit",d.data)},null,8,["onClick"]),[[t,"编辑",void 0,{top:!0}]]),T(r(a(_),{icon:"pi pi-trash",outlined:"",rounded:"",severity:"danger",onClick:h=>u.$emit("delete",d.data)},null,8,["onClick"]),[[t,"删除",void 0,{top:!0}]])])]),_:1})]),_:1},8,["value","loading"])])}}}),Ce={class:"space-y-3"},Ue={key:0,class:"flex flex-col items-center justify-center py-12 bg-surface-0 dark:bg-surface-900 rounded-lg border border-surface-200 dark:border-surface-700"},Se={class:"flex justify-between items-start"},De={class:"flex flex-col gap-1"},Te={class:"text-lg font-bold text-surface-900 dark:text-surface-0"},Be={class:"flex items-center gap-2"},Ie={class:"text-xs text-surface-500 font-mono"},Ee={class:"grid grid-cols-1 gap-3 text-sm bg-surface-50 dark:bg-surface-800/50 p-3 rounded-md"},Fe={class:"text-surface-900 dark:text-surface-0 break-all"},Le=["onClick"],Me={class:"grid grid-cols-2 gap-4"},Pe={class:"text-surface-700 dark:text-surface-200"},je={class:"text-surface-700 dark:text-surface-200"},ze={class:"flex flex-col gap-1"},Oe={key:0,class:"flex items-center gap-1"},Ae={class:"text-surface-700 dark:text-surface-200"},Ne={class:"flex items-center gap-1"},Ge={class:"text-surface-700 dark:text-surface-200"},Ke={class:"grid grid-cols-2 gap-2 pt-2"},Re={key:2,class:"flex flex-col items-center justify-center py-12 bg-surface-0 dark:bg-surface-900 rounded-lg border border-dashed border-surface-300 dark:border-surface-700"},qe=V({__name:"UserTableMobile",props:{users:{},loading:{type:Boolean}},emits:["edit","delete"],setup(o){const i=E(),l=async u=>{try{await navigator.clipboard.writeText(u),i.add({severity:"success",summary:"复制成功",detail:"代理密码已复制到剪切板",life:1e3})}catch(s){console.error("无法复制文本: ",s),i.add({severity:"error",summary:"复制失败",detail:"请手动复制",life:1e3})}};return(u,s)=>(g(),m("div",Ce,[o.loading?(g(),m("div",Ue,[...s[0]||(s[0]=[e("i",{class:"pi pi-spin pi-spinner text-3xl text-primary-500 mb-2"},null,-1),e("span",{class:"text-surface-500"},"加载中...",-1)])])):o.users.length>0?(g(!0),m(Q,{key:1},W(o.users,t=>(g(),m("div",{key:t.id,class:"bg-surface-0 dark:bg-surface-900 rounded-lg shadow-sm border border-surface-200 dark:border-surface-700 p-4 space-y-4"},[e("div",Se,[e("div",De,[e("span",Te,x(t.username),1),e("div",Be,[r(a(D),{value:t.permission==="admin"?"管理":"用户",severity:t.permission==="admin"?"danger":"info",class:"w-fit"},null,8,["value","severity"]),r(a(D),{value:t.status_label,severity:t.is_active?"success":"secondary",class:"w-fit"},null,8,["value","severity"])])]),e("div",Ie,"ID: "+x(t.id),1)]),e("div",Ee,[e("div",null,[s[1]||(s[1]=e("div",{class:"text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"}," 邮箱地址 ",-1)),e("div",Fe,x(t.email),1)]),e("div",null,[s[2]||(s[2]=e("div",{class:"text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"}," 代理密码 (点击复制) ",-1)),e("div",{class:"font-mono text-primary-600 dark:text-primary-400 font-bold cursor-pointer hover:underline break-all",onClick:d=>l(t.proxy_password)},x(t.proxy_password),9,Le)]),e("div",Me,[e("div",null,[s[3]||(s[3]=e("div",{class:"text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"}," 代理到期 ",-1)),e("div",Pe,x(t.formatted_proxy_expire_at),1)]),e("div",null,[s[4]||(s[4]=e("div",{class:"text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"}," 创建时间 ",-1)),e("div",je,x(t.formatted_created_at),1)])]),e("div",null,[s[7]||(s[7]=e("div",{class:"text-surface-500 dark:text-surface-400 text-[10px] uppercase tracking-wider mb-1"}," 最后登录 ",-1)),e("div",ze,[t.last_login_ip?(g(),m("div",Oe,[s[5]||(s[5]=e("i",{class:"pi pi-map-marker text-xs text-surface-400"},null,-1)),e("span",Ae,x(t.last_login_ip),1)])):S("",!0),e("div",Ne,[s[6]||(s[6]=e("i",{class:"pi pi-clock text-xs text-surface-400"},null,-1)),e("span",Ge,x(t.formatted_last_login_at),1)])])])]),e("div",Ke,[r(a(_),{icon:"pi pi-pencil",label:"编辑用户",severity:"warn",outlined:"",size:"small",onClick:d=>u.$emit("edit",t)},null,8,["onClick"]),r(a(_),{icon:"pi pi-trash",label:"删除用户",severity:"danger",outlined:"",size:"small",onClick:d=>u.$emit("delete",t)},null,8,["onClick"])])]))),128)):(g(),m("div",Re,[...s[8]||(s[8]=[e("i",{class:"pi pi-inbox text-4xl text-surface-300 dark:text-surface-600 mb-2"},null,-1),e("p",{class:"text-surface-500 dark:text-surface-400"},"暂无用户数据",-1)])]))]))}}),He={class:"space-y-4"},Je=V({__name:"UserTable",props:{users:{},loading:{type:Boolean}},emits:["edit","delete"],setup(o){const i=y(!1);let l=null;const u=s=>{i.value=s.matches};return P(()=>{l=window.matchMedia("(max-width: 767px)"),i.value=l.matches,l.addEventListener("change",u)}),X(()=>{l&&l.removeEventListener("change",u)}),(s,t)=>(g(),m("div",He,[i.value?(g(),I(qe,{key:1,users:o.users,loading:o.loading,onEdit:t[2]||(t[2]=d=>s.$emit("edit",d)),onDelete:t[3]||(t[3]=d=>s.$emit("delete",d))},null,8,["users","loading"])):(g(),I(Ve,{key:0,users:o.users,loading:o.loading,onEdit:t[0]||(t[0]=d=>s.$emit("edit",d)),onDelete:t[1]||(t[1]=d=>s.$emit("delete",d))},null,8,["users","loading"]))]))}});var Qe=`
    .p-inputgroup,
    .p-inputgroup .p-iconfield,
    .p-inputgroup .p-floatlabel,
    .p-inputgroup .p-iftalabel {
        display: flex;
        align-items: stretch;
        width: 100%;
    }

    .p-inputgroup .p-inputtext,
    .p-inputgroup .p-inputwrapper {
        flex: 1 1 auto;
        width: 1%;
    }

    .p-inputgroupaddon {
        display: flex;
        align-items: center;
        justify-content: center;
        padding: dt('inputgroup.addon.padding');
        background: dt('inputgroup.addon.background');
        color: dt('inputgroup.addon.color');
        border-block-start: 1px solid dt('inputgroup.addon.border.color');
        border-block-end: 1px solid dt('inputgroup.addon.border.color');
        min-width: dt('inputgroup.addon.min.width');
    }

    .p-inputgroupaddon:first-child,
    .p-inputgroupaddon + .p-inputgroupaddon {
        border-inline-start: 1px solid dt('inputgroup.addon.border.color');
    }

    .p-inputgroupaddon:last-child {
        border-inline-end: 1px solid dt('inputgroup.addon.border.color');
    }

    .p-inputgroupaddon:has(.p-button) {
        padding: 0;
        overflow: hidden;
    }

    .p-inputgroupaddon .p-button {
        border-radius: 0;
    }

    .p-inputgroup > .p-component,
    .p-inputgroup > .p-inputwrapper > .p-component,
    .p-inputgroup > .p-iconfield > .p-component,
    .p-inputgroup > .p-floatlabel > .p-component,
    .p-inputgroup > .p-floatlabel > .p-inputwrapper > .p-component,
    .p-inputgroup > .p-iftalabel > .p-component,
    .p-inputgroup > .p-iftalabel > .p-inputwrapper > .p-component {
        border-radius: 0;
        margin: 0;
    }

    .p-inputgroupaddon:first-child,
    .p-inputgroup > .p-component:first-child,
    .p-inputgroup > .p-inputwrapper:first-child > .p-component,
    .p-inputgroup > .p-iconfield:first-child > .p-component,
    .p-inputgroup > .p-floatlabel:first-child > .p-component,
    .p-inputgroup > .p-floatlabel:first-child > .p-inputwrapper > .p-component,
    .p-inputgroup > .p-iftalabel:first-child > .p-component,
    .p-inputgroup > .p-iftalabel:first-child > .p-inputwrapper > .p-component {
        border-start-start-radius: dt('inputgroup.addon.border.radius');
        border-end-start-radius: dt('inputgroup.addon.border.radius');
    }

    .p-inputgroupaddon:last-child,
    .p-inputgroup > .p-component:last-child,
    .p-inputgroup > .p-inputwrapper:last-child > .p-component,
    .p-inputgroup > .p-iconfield:last-child > .p-component,
    .p-inputgroup > .p-floatlabel:last-child > .p-component,
    .p-inputgroup > .p-floatlabel:last-child > .p-inputwrapper > .p-component,
    .p-inputgroup > .p-iftalabel:last-child > .p-component,
    .p-inputgroup > .p-iftalabel:last-child > .p-inputwrapper > .p-component {
        border-start-end-radius: dt('inputgroup.addon.border.radius');
        border-end-end-radius: dt('inputgroup.addon.border.radius');
    }

    .p-inputgroup .p-component:focus,
    .p-inputgroup .p-component.p-focus,
    .p-inputgroup .p-inputwrapper-focus,
    .p-inputgroup .p-component:focus ~ label,
    .p-inputgroup .p-component.p-focus ~ label,
    .p-inputgroup .p-inputwrapper-focus ~ label {
        z-index: 1;
    }

    .p-inputgroup > .p-button:not(.p-button-icon-only) {
        width: auto;
    }

    .p-inputgroup .p-iconfield + .p-iconfield .p-inputtext {
        border-inline-start: 0;
    }
`,We={root:"p-inputgroup"},Xe=j.extend({name:"inputgroup",style:Qe,classes:We}),Ye={name:"BaseInputGroup",extends:ee,style:Xe,provide:function(){return{$pcInputGroup:this,$parentInstance:this}}},A={name:"InputGroup",extends:Ye,inheritAttrs:!1};function Ze(o,i,l,u,s,t){return g(),m("div",C({class:o.cx("root")},o.ptmi("root")),[z(o.$slots,"default")],16)}A.render=Ze;var et=`
    .p-toggleswitch {
        display: inline-block;
        width: dt('toggleswitch.width');
        height: dt('toggleswitch.height');
    }

    .p-toggleswitch-input {
        cursor: pointer;
        appearance: none;
        position: absolute;
        top: 0;
        inset-inline-start: 0;
        width: 100%;
        height: 100%;
        padding: 0;
        margin: 0;
        opacity: 0;
        z-index: 1;
        outline: 0 none;
        border-radius: dt('toggleswitch.border.radius');
    }

    .p-toggleswitch-slider {
        cursor: pointer;
        width: 100%;
        height: 100%;
        border-width: dt('toggleswitch.border.width');
        border-style: solid;
        border-color: dt('toggleswitch.border.color');
        background: dt('toggleswitch.background');
        transition:
            background dt('toggleswitch.transition.duration'),
            color dt('toggleswitch.transition.duration'),
            border-color dt('toggleswitch.transition.duration'),
            outline-color dt('toggleswitch.transition.duration'),
            box-shadow dt('toggleswitch.transition.duration');
        border-radius: dt('toggleswitch.border.radius');
        outline-color: transparent;
        box-shadow: dt('toggleswitch.shadow');
    }

    .p-toggleswitch-handle {
        position: absolute;
        top: 50%;
        display: flex;
        justify-content: center;
        align-items: center;
        background: dt('toggleswitch.handle.background');
        color: dt('toggleswitch.handle.color');
        width: dt('toggleswitch.handle.size');
        height: dt('toggleswitch.handle.size');
        inset-inline-start: dt('toggleswitch.gap');
        margin-block-start: calc(-1 * calc(dt('toggleswitch.handle.size') / 2));
        border-radius: dt('toggleswitch.handle.border.radius');
        transition:
            background dt('toggleswitch.transition.duration'),
            color dt('toggleswitch.transition.duration'),
            inset-inline-start dt('toggleswitch.slide.duration'),
            box-shadow dt('toggleswitch.slide.duration');
    }

    .p-toggleswitch.p-toggleswitch-checked .p-toggleswitch-slider {
        background: dt('toggleswitch.checked.background');
        border-color: dt('toggleswitch.checked.border.color');
    }

    .p-toggleswitch.p-toggleswitch-checked .p-toggleswitch-handle {
        background: dt('toggleswitch.handle.checked.background');
        color: dt('toggleswitch.handle.checked.color');
        inset-inline-start: calc(dt('toggleswitch.width') - calc(dt('toggleswitch.handle.size') + dt('toggleswitch.gap')));
    }

    .p-toggleswitch:not(.p-disabled):has(.p-toggleswitch-input:hover) .p-toggleswitch-slider {
        background: dt('toggleswitch.hover.background');
        border-color: dt('toggleswitch.hover.border.color');
    }

    .p-toggleswitch:not(.p-disabled):has(.p-toggleswitch-input:hover) .p-toggleswitch-handle {
        background: dt('toggleswitch.handle.hover.background');
        color: dt('toggleswitch.handle.hover.color');
    }

    .p-toggleswitch:not(.p-disabled):has(.p-toggleswitch-input:hover).p-toggleswitch-checked .p-toggleswitch-slider {
        background: dt('toggleswitch.checked.hover.background');
        border-color: dt('toggleswitch.checked.hover.border.color');
    }

    .p-toggleswitch:not(.p-disabled):has(.p-toggleswitch-input:hover).p-toggleswitch-checked .p-toggleswitch-handle {
        background: dt('toggleswitch.handle.checked.hover.background');
        color: dt('toggleswitch.handle.checked.hover.color');
    }

    .p-toggleswitch:not(.p-disabled):has(.p-toggleswitch-input:focus-visible) .p-toggleswitch-slider {
        box-shadow: dt('toggleswitch.focus.ring.shadow');
        outline: dt('toggleswitch.focus.ring.width') dt('toggleswitch.focus.ring.style') dt('toggleswitch.focus.ring.color');
        outline-offset: dt('toggleswitch.focus.ring.offset');
    }

    .p-toggleswitch.p-invalid > .p-toggleswitch-slider {
        border-color: dt('toggleswitch.invalid.border.color');
    }

    .p-toggleswitch.p-disabled {
        opacity: 1;
    }

    .p-toggleswitch.p-disabled .p-toggleswitch-slider {
        background: dt('toggleswitch.disabled.background');
    }

    .p-toggleswitch.p-disabled .p-toggleswitch-handle {
        background: dt('toggleswitch.handle.disabled.background');
    }
`,tt={root:{position:"relative"}},st={root:function(i){var l=i.instance,u=i.props;return["p-toggleswitch p-component",{"p-toggleswitch-checked":l.checked,"p-disabled":u.disabled,"p-invalid":l.$invalid}]},input:"p-toggleswitch-input",slider:"p-toggleswitch-slider",handle:"p-toggleswitch-handle"},ot=j.extend({name:"toggleswitch",style:et,classes:st,inlineStyles:tt}),at={name:"BaseToggleSwitch",extends:se,props:{trueValue:{type:null,default:!0},falseValue:{type:null,default:!1},readonly:{type:Boolean,default:!1},tabindex:{type:Number,default:null},inputId:{type:String,default:null},inputClass:{type:[String,Object],default:null},inputStyle:{type:Object,default:null},ariaLabelledby:{type:String,default:null},ariaLabel:{type:String,default:null}},style:ot,provide:function(){return{$pcToggleSwitch:this,$parentInstance:this}}},N={name:"ToggleSwitch",extends:at,inheritAttrs:!1,emits:["change","focus","blur"],methods:{getPTOptions:function(i){var l=i==="root"?this.ptmi:this.ptm;return l(i,{context:{checked:this.checked,disabled:this.disabled}})},onChange:function(i){if(!this.disabled&&!this.readonly){var l=this.checked?this.falseValue:this.trueValue;this.writeValue(l,i),this.$emit("change",i)}},onFocus:function(i){this.$emit("focus",i)},onBlur:function(i){var l,u;this.$emit("blur",i),(l=(u=this.formField).onBlur)===null||l===void 0||l.call(u,i)}},computed:{checked:function(){return this.d_value===this.trueValue},dataP:function(){return te({checked:this.checked,disabled:this.disabled,invalid:this.$invalid})}}},lt=["data-p-checked","data-p-disabled","data-p"],it=["id","checked","tabindex","disabled","readonly","aria-checked","aria-labelledby","aria-label","aria-invalid"],nt=["data-p"],rt=["data-p"];function dt(o,i,l,u,s,t){return g(),m("div",C({class:o.cx("root"),style:o.sx("root")},t.getPTOptions("root"),{"data-p-checked":t.checked,"data-p-disabled":o.disabled,"data-p":t.dataP}),[e("input",C({id:o.inputId,type:"checkbox",role:"switch",class:[o.cx("input"),o.inputClass],style:o.inputStyle,checked:t.checked,tabindex:o.tabindex,disabled:o.disabled,readonly:o.readonly,"aria-checked":t.checked,"aria-labelledby":o.ariaLabelledby,"aria-label":o.ariaLabel,"aria-invalid":o.invalid||void 0,onFocus:i[0]||(i[0]=function(){return t.onFocus&&t.onFocus.apply(t,arguments)}),onBlur:i[1]||(i[1]=function(){return t.onBlur&&t.onBlur.apply(t,arguments)}),onChange:i[2]||(i[2]=function(){return t.onChange&&t.onChange.apply(t,arguments)})},t.getPTOptions("input")),null,16,it),e("div",C({class:o.cx("slider")},t.getPTOptions("slider"),{"data-p":t.dataP}),[e("div",C({class:o.cx("handle")},t.getPTOptions("handle"),{"data-p":t.dataP}),[z(o.$slots,"handle",{checked:t.checked})],16,rt)],16,nt)],16,lt)}N.render=dt;const pt={class:"grid grid-cols-1 md:grid-cols-2 gap-x-8 gap-y-6 mt-4"},ut={class:"flex flex-col gap-6"},ct={class:"field"},gt={key:0,class:"p-error block mt-1"},mt={class:"field"},ft={key:0,class:"p-error block mt-1"},ht={key:1,class:"p-error block mt-1"},bt={class:"field"},vt={class:"flex flex-col gap-6"},wt={class:"field"},xt={class:"field"},yt={class:"field"},kt={class:"flex gap-4"},_t={class:"flex items-center"},$t={class:"flex items-center"},Vt={class:"field flex items-center gap-3 mt-2"},Ct={class:"flex justify-end gap-2 mt-6"},Ut=V({__name:"UserDialog",props:{show:{type:Boolean},user:{},saving:{type:Boolean}},emits:["update:show","save"],setup(o,{emit:i}){const l=o,u=i,s=y(l.show),t=y(!1),d=y(!1),h=y(""),p=y({username:"",email:"",permission:"user",is_active:1,proxy_password:"",proxy_expire_ts:0}),w=y(null),f=()=>{h.value="",l.user?(t.value=!0,p.value={username:l.user.username,email:l.user.email,permission:l.user.permission,is_active:l.user.is_active,proxy_password:l.user.proxy_password,proxy_expire_ts:l.user.proxy_expire_ts},w.value=l.user.proxy_expire_ts&&l.user.proxy_expire_ts>0?new Date(l.user.proxy_expire_ts*1e3):null):(t.value=!1,p.value={username:"",email:"",permission:"user",is_active:1,proxy_password:"",proxy_expire_ts:0},w.value=null),d.value=!1},$=b=>/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(b),G=()=>{s.value=!1},K=()=>{f()},R=()=>{d.value=!0,w.value?p.value.proxy_expire_ts=Math.floor(w.value.getTime()/1e3):p.value.proxy_expire_ts=0;const b={...p.value};h.value&&(b.login_password_md5=ue(h.value)),p.value.username&&$(p.value.email)&&u("save",b)},q=()=>{const b="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";let n="";for(let c=0;c<20;c++)n+=b.charAt(Math.floor(Math.random()*b.length));p.value.proxy_password=n};return B(()=>l.show,b=>{s.value=b}),B(s,b=>{u("update:show",b)}),B(()=>l.user,()=>{f()},{immediate:!0}),(b,n)=>(g(),I(a(ne),{visible:s.value,"onUpdate:visible":n[8]||(n[8]=c=>s.value=c),header:t.value?"编辑用户":"新增用户",modal:!0,class:"p-fluid w-full max-w-2xl",onHide:K},{footer:v(()=>[e("div",Ct,[r(a(_),{label:"取消",icon:"pi pi-times",text:"",class:"p-button-lg",onClick:G}),r(a(_),{label:"保存",icon:"pi pi-check",loading:o.saving,class:"p-button-lg px-6",onClick:R},null,8,["loading"])])]),default:v(()=>[e("div",pt,[e("div",ut,[n[12]||(n[12]=e("h3",{class:"text-xl font-semibold border-b pb-2 mb-2 text-primary"},"基础信息",-1)),e("div",ct,[n[9]||(n[9]=e("label",{for:"username",class:"font-bold block mb-2 text-700"},"用户名",-1)),r(a(U),{id:"username",modelValue:p.value.username,"onUpdate:modelValue":n[0]||(n[0]=c=>p.value.username=c),modelModifiers:{trim:!0},required:"true",autofocus:"",class:L(["w-full",{"p-invalid":d.value&&!p.value.username}]),placeholder:"请输入用户名"},null,8,["modelValue","class"]),d.value&&!p.value.username?(g(),m("small",gt," 用户名是必填项 ")):S("",!0)]),e("div",mt,[n[10]||(n[10]=e("label",{for:"email",class:"font-bold block mb-2 text-700"},"邮箱",-1)),r(a(U),{id:"email",modelValue:p.value.email,"onUpdate:modelValue":n[1]||(n[1]=c=>p.value.email=c),modelModifiers:{trim:!0},required:"true",class:L(["w-full",{"p-invalid":d.value&&!$(p.value.email)}]),placeholder:"请输入邮箱"},null,8,["modelValue","class"]),d.value&&!p.value.email?(g(),m("small",ft," 邮箱是必填项 ")):d.value&&!$(p.value.email)?(g(),m("small",ht," 请输入有效的邮箱格式 ")):S("",!0)]),e("div",bt,[n[11]||(n[11]=e("label",{for:"password",class:"font-bold block mb-2 text-700"},"登录密码",-1)),r(a(pe),{id:"password",modelValue:h.value,"onUpdate:modelValue":n[2]||(n[2]=c=>h.value=c),modelModifiers:{trim:!0},class:"w-full","input-class":"w-full","toggle-mask":"",feedback:!1,placeholder:"留空则不修改/使用默认"},null,8,["modelValue"])])]),e("div",vt,[n[19]||(n[19]=e("h3",{class:"text-xl font-semibold border-b pb-2 mb-2 text-primary"},"代理与设置",-1)),e("div",wt,[n[13]||(n[13]=e("label",{for:"proxy_password",class:"font-bold block mb-2 text-700"},"代理密码",-1)),r(a(A),null,{default:v(()=>[r(a(U),{id:"proxy_password",modelValue:p.value.proxy_password,"onUpdate:modelValue":n[3]||(n[3]=c=>p.value.proxy_password=c),modelModifiers:{trim:!0},placeholder:"留空则使用默认"},null,8,["modelValue"]),r(a(_),{icon:"pi pi-refresh",severity:"secondary",title:"生成随机密码",onClick:q,class:"border"})]),_:1})]),e("div",xt,[n[14]||(n[14]=e("label",{for:"proxy_expire_ts",class:"font-bold block mb-2 text-700"},"代理过期时间",-1)),r(a(re),{id:"proxy_expire_ts",modelValue:w.value,"onUpdate:modelValue":n[4]||(n[4]=c=>w.value=c),showTime:"",hourFormat:"24",class:"w-full",placeholder:"永久有效",showIcon:""},null,8,["modelValue"])]),e("div",yt,[n[17]||(n[17]=e("label",{class:"font-bold block mb-2 text-700"},"权限角色",-1)),e("div",kt,[e("div",_t,[r(a(M),{modelValue:p.value.permission,"onUpdate:modelValue":n[5]||(n[5]=c=>p.value.permission=c),inputId:"roleUser",name:"permission",value:"user"},null,8,["modelValue"]),n[15]||(n[15]=e("label",{for:"roleUser",class:"ml-2"},"普通用户",-1))]),e("div",$t,[r(a(M),{modelValue:p.value.permission,"onUpdate:modelValue":n[6]||(n[6]=c=>p.value.permission=c),inputId:"roleAdmin",name:"permission",value:"admin"},null,8,["modelValue"]),n[16]||(n[16]=e("label",{for:"roleAdmin",class:"ml-2"},"管理员",-1))])])]),e("div",Vt,[r(a(N),{modelValue:p.value.is_active===1,"onUpdate:modelValue":n[7]||(n[7]=c=>p.value.is_active=c?1:0)},null,8,["modelValue"]),n[18]||(n[18]=e("label",{class:"font-bold text-700"},"激活状态",-1))])])])]),_:1},8,["visible","header"]))}}),St=O(Ut,[["__scopeId","data-v-d01dc7b9"]]);function Dt(){const o=ce(),i=Y(),l=E(),u=y(!1),s=y(null),t=y(!1);return{store:o,showDialog:u,selectedUser:s,saving:t,openNew:()=>{s.value=null,u.value=!0},openEdit:f=>{s.value=f,u.value=!0},handleSave:async f=>{t.value=!0;try{s.value?(await o.editUser(s.value.id,f),l.add({severity:"success",summary:"成功",detail:"用户信息已更新",life:1e3})):(await o.addUser(f),l.add({severity:"success",summary:"成功",detail:"用户已新增",life:1e3})),u.value=!1}catch{l.add({severity:"error",summary:"错误",detail:"操作失败",life:1e3})}finally{t.value=!1}},confirmDelete:f=>{i.require({message:`确定要删除用户 "${f.username}" 吗？此操作不可撤销。`,header:"删除确认",icon:"pi pi-exclamation-triangle",rejectLabel:"取消",acceptLabel:"确定",rejectProps:{label:"取消",severity:"secondary",outlined:!0},acceptProps:{label:"删除",severity:"danger"},accept:async()=>{try{await o.removeUser(f.id),l.add({severity:"success",summary:"成功",detail:"用户已删除",life:3e3})}catch{l.add({severity:"error",summary:"错误",detail:"删除失败",life:3e3})}}})}}}const Tt={class:"user-management p-4"},Bt=V({__name:"User",setup(o){const{store:i,showDialog:l,selectedUser:u,saving:s,openNew:t,openEdit:d,handleSave:h,confirmDelete:p}=Dt();return P(()=>{i.fetchUsers()}),(w,f)=>(g(),m("div",Tt,[f[1]||(f[1]=e("div",{class:"mb-4"},[e("h1",{class:"text-2xl font-bold"},"用户管理")],-1)),r(be,{"initial-search":a(i).search,onSearch:a(i).onSearch,onAdd:a(t)},null,8,["initial-search","onSearch","onAdd"]),r(Je,{users:a(i).userVOs,loading:a(i).loading,onEdit:a(d),onDelete:a(p)},null,8,["users","loading","onEdit","onDelete"]),r(St,{show:a(l),"onUpdate:show":f[0]||(f[0]=$=>Z(l)?l.value=$:null),user:a(u),saving:a(s),onSave:a(h)},null,8,["show","user","saving","onSave"]),r(a(de)),r(a(le))]))}}),Nt=O(Bt,[["__scopeId","data-v-2996defe"]]);export{Nt as default};
