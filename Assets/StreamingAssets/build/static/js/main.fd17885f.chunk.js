(this["webpackJsonpbonsai-gui"]=this["webpackJsonpbonsai-gui"]||[]).push([[0],{118:function(e,t,n){},162:function(e,t,n){"use strict";n.r(t);var c=n(2),o=n(1),s=n.n(o),a=n(33),i=n.n(a),r=(n(86),n(11)),l=n(37),d=n(7),u=n(71),j=-1,b=0,h=1,f=2,p=3,x=5,m=function(e){var t;switch(e){case-1:t="unstarted";break;case 0:t="ended";break;case 1:t="playing";break;case 2:t="paused";break;case 3:t="buffering";break;case 5:t="cued";break;default:t="BAD SWITCH STATE"}return t},O=function(e){var t=new URLSearchParams(e.location.search),n=parseInt(t.get("x")),s=parseInt(t.get("y")),a={width:n&&s?n:window.innerWidth,height:n&&s?s:window.innerHeight,playerVars:{autoplay:0,controls:0,disablekb:1,rel:0}},i=Object(r.g)(),l="youtube_test"===e.location.pathname.split("/")[1],O=e.match.params,g=O.id,v=O.timeStamp,y=Object(o.useState)(null),w=Object(d.a)(y,2),k=w[0],N=w[1],_=Object(o.useState)(!1),C=Object(d.a)(_,2),S=C[0],E=C[1],M=Object(o.useState)(!1),T=Object(d.a)(M,2),D=T[0],L=T[1],J=Object(o.useState)(!1),I=Object(d.a)(J,2),A=I[0],H=I[1],P=Object(o.useState)(!1),R=Object(d.a)(P,2),V=R[0],B=R[1],U=Object(o.useCallback)((function(e){"READY"!==e&&L(!1),console.log("POST "+e),l||window.vuplex.postMessage({type:"stateChange",message:e,current_time:k.getCurrentTime()})}),[k,l]),W=Object(o.useCallback)((function(e){if(null!=k){if(D&&Math.abs(k.getCurrentTime()-e)<.01)return console.log("bonsai: ready-up called while ready"),void U("READY");var t=k.getPlayerState();S?(console.log("bonsai: readying up from "+m(t)+" -> "+e),Y(k),t===f&&k.playVideo(),k.seekTo(e,!0),k.pauseVideo(),k.unMute()):console.log("bonsai: ignoring attempt to ready-up before init")}else console.log("bonsai: ignoring attempt to ready up while player is null")}),[S,k,U,D]),Y=function(e){console.log("bonsai: prepare to ready up"),L(!1),H(!0),e.mute()};Object(o.useEffect)((function(){if(null!=k&&!l){var e=function(e){var t=JSON.parse(e.data);if("video"===t.type)switch(console.log(e.data),t.command){case"play":console.log("COMMAND: play"),k.playVideo();break;case"pause":console.log("COMMAND: pause"),k.pauseVideo();break;case"readyUp":console.log("COMMAND: readyUp"),W(t.timeStamp);break;default:console.log("command: not handled (video) "+e.data)}};return window.vuplex.addEventListener("message",e),function(){window.vuplex.removeEventListener("message",e)}}}),[g,k,l,W]),Object(o.useEffect)((function(){console.log("bonsai: add ping interval");var e=setInterval((function(){var e=0;null!=k&&null!=k.getCurrentTime()&&(e=k.getCurrentTime()),l||window.vuplex.postMessage({type:"infoCurrentTime",current_time:e})}),100);return function(){console.log("bonsai: remove ping interval"),clearInterval(e)}}),[g,k,l]);return Object(c.jsxs)("div",{children:[l?Object(c.jsxs)("div",{children:[Object(c.jsx)("p",{onClick:function(){i.push("/home")},children:"home"})," ",Object(c.jsx)("p",{onClick:function(){W(40)},children:"ready up"})]}):"",Object(c.jsx)(u.a,{opts:a,onReady:function(e){var t=e.target;N(t),Y(t),t.loadVideoById(g,parseFloat(v))},onError:function(e){console.log("bonsai youtube error: "+e)},onStateChange:function(e){switch(e.data){case x:console.log("bonsai: "+m(k.getPlayerState()));break;case j:console.log("bonsai: "+m(k.getPlayerState())+" "+k.getCurrentTime());break;case h:S?A?console.log("bonsai: while readying -> play"):(V?console.log("bonsai: playing after buffer"):console.log("bonsai: playing"),U("PLAYING")):k.pauseVideo(),V&&B(!1);break;case f:S?V?A&&!D?(console.log("bonsai: ready (pause after buffer)"),L(!0),H(!1),U("READY")):(console.log("bonsai: paused after buffering"),U("PAUSED")):A?console.log("bonsai: while readying -> paused"):(console.log("bonsai: paused"),U("PAUSED")):(console.log("bonsai: init complete"),k.seekTo(v,!0),k.unMute(),E(!0),L(!0),H(!1),U("READY")),V&&B(!1);break;case p:A?console.log("bonsai: while readying -> buffering"):console.log("bonsai: buffering"),B(!0);break;case b:console.log("bonsai: "+m(k.getPlayerState())),U("ENDED");break;default:console.log("bonsai error: did not handle state change "+e.data)}}})]})},g=n(39),v=n(49),y=n(164),w=n(75);var k=function(){var e=Object(v.useSpring)((function(){return{x:0,y:0,config:{mass:1,tension:400,friction:5}}})),t=Object(d.a)(e,2),n=t[0],o=n.x,s=n.y,a=t[1],i=Object(w.a)((function(e){var t=e.down,n=Object(d.a)(e.movement,2),c=n[0],o=n[1];a({x:t?c:0,y:t?o:0})}));return Object(c.jsx)("div",{className:"h-screen w-full flex flex-wrap content-center justify-center",children:Object(c.jsx)(y.animated.div,Object(g.a)(Object(g.a)({},i()),{},{className:"bg-red-400 h-10 w-10 rounded-lg",style:{x:o,y:s}}))})},N=n(77),_=n.n(N),C=function(){var e={url:"https://www.twitch.tv/hamletva",width:window.innerWidth,height:window.innerHeight};return Object(c.jsx)(_.a,Object(g.a)(Object(g.a)({},e),{},{onProgress:function(e){var t=e.played,n=e.loaded,c=e.playedSeconds,o=e.loadedSeconds;console.log(t,n,c,o)}}))},S=(n(118),n(28)),E=n(42),M=n.n(E),T=n.p+"static/media/door-open.d2c81c6b.svg",D=n.p+"static/media/link.e557aaf1.svg",L=n.p+"static/media/thinking-face.179ede86.svg",J=n(14),I=n(16),A=n(5),H=n(38),P=s.a.createContext(),R=function(){return Object(o.useContext)(P)},V=new(function(){function e(){Object(J.a)(this,e),this.ip_address=null,this.port=null,this.network_state=null,this.loading_room_code=!1,this._refresh_room_code_handler=null,this.player_info=[],this._room_code=null,Object(A.f)(this)}return Object(I.a)(e,[{key:"refreshRoomCode",value:function(){var e=this;M()({method:"post",url:"https://api.desk.link"+"/rooms/".concat(V.room_code,"/refresh")}).then((function(e){console.log("refresh "+V.room_code)})).catch((function(t){console.log(t),e.room_code=null}))}},{key:"room_code",get:function(){return this._room_code},set:function(e){this._room_code=e,e?this._refresh_room_code_handler=setInterval(this.refreshRoomCode,1e3):(clearInterval(this._refresh_room_code_handler),this._refresh_room_code_handler=null)}}]),e}());Object.seal(V);var B=Object(A.b)((function(e){e.forEach((function(e){V[e.Key]=e.Val}))})),U=Object(A.b)((function(e){for(var t in e)V[t]=e[t]})),W=Object(A.b)((function(e){V[e.Key]=e.Val}));var Y=Object(H.a)((function(e){var t=e.children;return function(){var e=Object(o.useState)(!1),t=Object(d.a)(e,2),n=t[0],c=t[1];Object(o.useEffect)((function(){if(!n){c(!0);var e=function(e){var t=JSON.parse(e.data);switch(t.Type){case"command":switch(t.Message){case"pushStore":B(t.Data);break;case"pushStoreSingle":W(t.Data);break;default:console.log("message not handled "+e.data)}break;default:console.log("command not handled "+e.data),console.log(t)}};null!=window.vuplex?(console.log("bonsai: vuplex is not null -> storeListeners"),window.vuplex.addEventListener("message",e)):(console.log("bonsai: vuplex is null"),window.addEventListener("vuplexready",(function(t){console.log("bonsai: vuplexready -> storeListeners"),window.vuplex.addEventListener("message",e)})))}}),[n])}(),Object(c.jsx)(P.Provider,{value:{store:V,pushStore:U,pushStoreList:B},children:t})})),F=n(55),z="https://api.desk.link",G="py-4 px-8 font-bold bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer flex flex-wrap content-center";function K(){Object(S.postJson)({Type:"command",Message:"leaveRoom"})}function q(){Object(S.postJson)({Type:"command",Message:"kickAll"})}function Q(){Object(S.postJson)({Type:"event",Message:"mouseDown"})}function X(){Object(S.postJson)({Type:"event",Message:"mouseUp"})}function Z(){Object(S.postJson)({Type:"event",Message:"hover"})}function $(e){return Object(c.jsx)("div",{onMouseDown:Q,onMouseUp:X,onMouseEnter:Z,children:e.children})}function ee(e){var t=e.selected,n=e.handleClick,o=t?"rounded bg-blue-700 text-white px-3 py-2 cursor-pointer":"rounded hover:bg-gray-800 active:bg-gray-900 hover:text-white px-3 py-2 cursor-pointer";return Object(c.jsx)($,{children:Object(c.jsx)("div",{className:o,onClick:n,children:e.children})})}function te(e){return Object(c.jsx)("div",{className:"space-y-1 px-2",children:e.children})}function ne(e){return Object(c.jsx)("div",{className:"text-white font-bold text-xl px-5 pt-5 pb-2",children:e.children})}function ce(e){var t=e.handleClick,n=e.char;return Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){t(n)},className:"bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center",children:Object(c.jsx)("span",{className:"w-full text-center",children:n})})})}function oe(e){var t=e.info,n=t.Name,o=t.ConnectionId;return Object(c.jsx)(se,{title:n,slug:o,imgSrc:L,children:Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){var e;e=o,Object(S.postJson)({Type:"command",Message:"kickConnectionId",Data:e})},className:G,children:"kick"})})})}function se(e){return Object(c.jsxs)("div",{className:"flex w-full justify-between",children:[Object(c.jsxs)("div",{className:"flex w-auto",children:[Object(c.jsx)("div",{className:"flex flex-wrap content-center  p-2 mr-2",children:Object(c.jsx)("img",{className:"h-9 w-9",src:e.imgSrc,alt:""})}),Object(c.jsxs)("div",{children:[Object(c.jsx)("div",{className:"text-xl",children:e.title}),Object(c.jsx)("div",{className:"text-gray-400",children:e.slug})]})]}),e.children]})}function ae(e){var t=e.name;return Object(c.jsxs)("div",{className:"text-white p-4 h-full pr-8",children:[t?Object(c.jsx)("div",{className:"pb-8 text-xl",children:t}):"",Object(c.jsx)("div",{className:"space-y-8",children:e.children})]})}var ie=function(){return Object(c.jsx)("div",{className:"flex",children:Object(c.jsx)(se,{title:"Connected",slug:"You are connected to a host",imgSrc:D,children:Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:K,className:G,children:"exit"})})})})},re=Object(H.a)((function(){var e=R(),t=e.store,n=e.pushStore;return Object(c.jsxs)(s.a.Fragment,{children:[Object(c.jsx)(se,{title:"Desk Code",slug:"People who have this can join you",imgSrc:T,children:Object(c.jsx)("div",{className:"text-4xl flex flex-wrap content-center",children:t.room_code?Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){n({room_code:null})},className:"px-2 py-1 bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer flex flex-wrap content-center",children:t.room_code})}):Object(c.jsx)(F.BounceLoader,{size:40,color:"#737373"})})}),t.player_info.length>1?Object(c.jsxs)(s.a.Fragment,{children:[Object(c.jsx)(se,{title:"Clients connected",slug:"There are people in your room",imgSrc:D,children:Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:q,className:G,children:"kick all"})})}),t.player_info.map((function(e){return Object(c.jsx)(oe,{info:e})}))]}):""]})})),le=function(){return Object(c.jsx)("div",{className:"flex justify-center w-full h-full",children:Object(c.jsx)(F.BounceLoader,{size:200,color:"#737373"})})};var de=[{name:"Home",component:Object(H.a)((function(){var e;switch(R().store.network_state){case"Neutral":case"HostWaiting":case"Hosting":e=Object(c.jsx)(re,{});break;case"ClientConnected":e=Object(c.jsx)(ie,{});break;default:e=Object(c.jsx)(le,{})}return Object(c.jsx)(ae,{name:"Home",children:e})}))},{name:"Join Desk",component:function(e){var t=e.navHome,n=Object(o.useState)(""),s=Object(d.a)(n,2),a=s[0],i=s[1],r=Object(o.useState)(!1),l=Object(d.a)(r,2),u=l[0],j=l[1],b=Object(o.useState)(""),h=Object(d.a)(b,2),f=h[0],p=h[1];function x(e){switch(p(""),a.length){case 4:i(e);break;default:i(a+e)}}return Object(o.useEffect)((function(){if(!u&&4===a.length){var e=z+"/rooms/".concat(a);console.log(e),M()({method:"get",url:e}).then((function(e){var n;n=e.data,Object(S.postJson)({Type:"command",Message:"joinRoom",data:JSON.stringify(n)}),t(),i(""),j(!1)})).catch((function(e){console.log(e),i(""),j(!1),p("Could not find room, try again")}))}}),[u,a]),Object(c.jsx)(ae,{name:"Join Desk",children:Object(c.jsxs)("div",{className:"flex flex-wrap w-full content-center",children:[Object(c.jsxs)("div",{className:" w-1/2",children:[Object(c.jsx)("div",{className:"text-xl",children:f}),Object(c.jsx)("div",{className:"text-9xl h-full flex flex-wrap content-center justify-center",children:a})]}),Object(c.jsxs)("div",{className:"p-2 rounded space-y-4 text-2xl",children:[Object(c.jsxs)("div",{className:"flex space-x-4",children:[Object(c.jsx)(ce,{handleClick:x,char:"L"}),Object(c.jsx)(ce,{handleClick:x,char:"R"}),Object(c.jsx)(ce,{handleClick:x,char:"C"})]}),Object(c.jsxs)("div",{className:"flex space-x-4",children:[Object(c.jsx)(ce,{handleClick:x,char:"D"}),Object(c.jsx)(ce,{handleClick:x,char:"E"}),Object(c.jsx)(ce,{handleClick:x,char:"F"})]}),Object(c.jsxs)("div",{className:"flex space-x-4",children:[Object(c.jsx)(ce,{handleClick:x,char:"G"}),Object(c.jsx)(ce,{handleClick:x,char:"H"}),Object(c.jsx)(ce,{handleClick:x,char:"I"})]}),Object(c.jsx)("div",{className:"flex flex-wrap w-full justify-around",children:Object(c.jsx)(ce,{handleClick:function(){a.length>0&&i(a.slice(0,a.length-1))},char:"<"})})]})]})})}},{name:"Contacts",component:function(){return Object(c.jsx)(ae,{name:"Contacts"})}},{name:"Settings",component:Object(H.a)((function(){var e=R().store,t="bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer flex flex-wrap content-center",n=Object(A.b)((function(e){e.ip_address=1234,e.port=4321})),o=Object(A.b)((function(e){e.ip_address=null,e.port=null})),s=Object(A.b)((function(e,t){e.network_state=t})),a=Object(A.b)((function(e){e.player_info.push({Name:"cam",ConnectionId:0})})),i=Object(A.b)((function(e){e.player_info.pop()}));return Object(c.jsxs)(ae,{name:"Settings",children:[Object(c.jsxs)("div",{className:"flex space-x-2",children:[Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){s(e,"Neutral")},className:t,children:"Neutral"})}),Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){s(e,"HostWaiting")},className:t,children:"HostWaiting"})}),Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){s(e,"Hosting")},className:t,children:"Hosting"})}),Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){s(e,"ClientConnected")},className:t,children:"ClientConnected"})})]}),Object(c.jsxs)("div",{className:"flex space-x-2",children:[Object(c.jsx)($,{children:Object(c.jsx)("div",{className:t,onClick:function(){n(e)},children:"+ fake ip/port"})}),Object(c.jsx)($,{children:Object(c.jsx)("div",{className:t,onClick:function(){o(e)},children:"- fake ip/port"})})]}),Object(c.jsxs)("div",{className:"flex space-x-2",children:[Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){a(e)},className:t,children:"+ fake client"})}),Object(c.jsx)($,{children:Object(c.jsx)("div",{onClick:function(){i(e)},className:t,children:"- fake client"})})]}),Object(c.jsx)("ul",{children:Object.entries(e).map((function(e){return Object(c.jsxs)("li",{children:[e[0],": ",ue(e)]},e[0])}))})]})}))}];function ue(e){switch(e[0]){case"player_info":return"["+e[1].map((function(e){return"(".concat(e.Name,", ").concat(e.ConnectionId,")")})).join(" ")+"]";default:return e[1]?e[1].toString():""}}var je=function(){var e=R(),t=e.store,n=e.pushStore,s=Object(o.useState)(0),a=Object(d.a)(s,2),i=a[0],r=a[1],l=de[i].component;return Object(o.useEffect)((function(){Object(A.c)((function(){if(t.room_code&&(!t.ip_address||!t.port))return console.log("rm room code"),void n({room_code:null});if(!t.room_code&&!t.loading_room_code&&t.ip_address&&t.port){console.log("fetch room code"),n({loading_room_code:!0});M()({method:"post",url:"https://api.desk.link/rooms",data:"ip_address=".concat(t.ip_address,"&port=").concat(t.port),header:{"content-type":"application/x-www-form-urlencoded"}}).then((function(e){n({room_code:e.data.tag,loading_room_code:!1})})).catch((function(e){console.log(e),n({loading_room_code:!1})}))}}))})),Object(o.useEffect)((function(){return function(){n({room_code:null})}}),[n]),Object(c.jsxs)("div",{className:"flex text-lg text-gray-500 h-full",children:[Object(c.jsxs)("div",{className:"w-4/12 bg-black overflow-auto scrollhost static",children:[Object(c.jsx)("div",{className:"w-4/12 bg-black fixed",children:Object(c.jsx)(ne,{children:"Menu"})}),Object(c.jsx)("div",{className:"h-16"}),Object(c.jsx)(te,{children:de.map((function(e,t){return Object(c.jsx)(ee,{handleClick:function(){r(t)},selected:i===t,children:e.name},e.name)}))})]}),Object(c.jsx)("div",{className:"bg-gray-900 z-10 w-full overflow-auto scrollhost",children:Object(c.jsx)(l,{navHome:function(){r(0)}})})]})};function be(){Object(S.postJson)({Type:"event",Message:"listenersReady"})}function he(){console.log("Boot");var e=Object(r.g)(),t=function(e){return function(t){console.log(t.data);var n=JSON.parse(t.data);if("nav"===n.type)switch(n.command){case"push":console.log("command: nav "+n.path),e.push(n.path);break;default:console.log("command: not handled (navListeners) "+JSON.stringify(n))}}}(e);return null!=window.vuplex?(console.log("bonsai: vuplex is not null -> navListeners"),window.vuplex.addEventListener("message",t),be()):(console.log("bonsai: vuplex is null"),window.addEventListener("vuplexready",(function(e){console.log("bonsai: vuplexready -> navListeners"),window.vuplex.addEventListener("message",t),be()}))),Object(c.jsxs)("div",{children:["Boot",Object(c.jsxs)("ul",{children:[Object(c.jsx)("li",{children:Object(c.jsx)(l.a,{to:"/youtube_test/qEfPBt9dU60/19.02890180001912?x=480&y=360",children:"youtube_test video"})}),Object(c.jsx)("li",{children:Object(c.jsx)(l.a,{to:"/spring",children:"spring"})}),Object(c.jsx)("li",{children:Object(c.jsx)(l.a,{to:"/twitch",children:"twitch"})}),Object(c.jsx)("li",{onClick:function(){e.push("/menu")},className:"text-white",children:"menu"}),Object(c.jsx)("li",{children:Object(c.jsx)(l.a,{to:"/menu",children:"menu"})})]})]})}function fe(){return Object(c.jsx)("div",{children:"home"})}var pe=function(){return console.log("App"),Object(c.jsx)(r.a,{children:Object(c.jsx)("div",{className:"h-screen text-green-400 select-none",children:Object(c.jsxs)(r.d,{children:[Object(c.jsx)(r.b,{path:"/home",component:fe}),Object(c.jsx)(r.b,{path:"/spring",component:k}),Object(c.jsx)(r.b,{path:"/twitch",component:C}),Object(c.jsx)(r.b,{path:"/menu",component:je}),Object(c.jsx)(r.b,{path:"/youtube/:id/:timeStamp",component:O}),Object(c.jsx)(r.b,{path:"/youtube_test/:id/:timeStamp",component:O}),Object(c.jsx)(r.b,{path:"/",component:he})]})})})};i.a.render(Object(c.jsx)(s.a.StrictMode,{children:Object(c.jsx)(Y,{children:Object(c.jsx)(pe,{})})}),document.getElementById("root"))},28:function(e,t){e.exports={postJson:function(e){null!=window.vuplex&&window.vuplex.postMessage(e)}}},86:function(e,t,n){}},[[162,1,2]]]);
//# sourceMappingURL=main.fd17885f.chunk.js.map