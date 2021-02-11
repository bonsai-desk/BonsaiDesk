(this["webpackJsonpbonsai-gui"]=this["webpackJsonpbonsai-gui"]||[]).push([[0],{118:function(e,t,c){},15:function(e,t){e.exports={postJson:function(e){null!=window.vuplex&&(console.log("post json==="),console.log(e),window.vuplex.postMessage(e),console.log("post json=="))}}},162:function(e,t,c){"use strict";c.r(t);var n=c(0),s=c(1),a=c.n(s),o=c(35),r=c.n(o),i=(c(86),c(11)),l=c(30),j=c(5),d=c(71),h=-1,b=0,u=1,f=2,x=3,O=5,p=function(e){var t;switch(e){case-1:t="unstarted";break;case 0:t="ended";break;case 1:t="playing";break;case 2:t="paused";break;case 3:t="buffering";break;case 5:t="cued";break;default:t="BAD SWITCH STATE"}return t},m=function(e){var t=new URLSearchParams(e.location.search),c=parseInt(t.get("x")),a=parseInt(t.get("y")),o={width:c&&a?c:window.innerWidth,height:c&&a?a:window.innerHeight,playerVars:{autoplay:0,controls:0,disablekb:1,rel:0}},r=Object(i.g)(),l="youtube_test"===e.location.pathname.split("/")[1],m=e.match.params,g=m.id,v=m.timeStamp,w=Object(s.useState)(null),y=Object(j.a)(w,2),k=y[0],N=y[1],C=Object(s.useState)(!1),_=Object(j.a)(C,2),S=_[0],M=_[1],D=Object(s.useState)(!1),E=Object(j.a)(D,2),T=E[0],J=E[1],L=Object(s.useState)(!1),P=Object(j.a)(L,2),R=P[0],I=P[1],A=Object(s.useState)(!1),H=Object(j.a)(A,2),U=H[0],B=H[1],F=Object(s.useCallback)((function(e){"READY"!==e&&J(!1),console.log("POST "+e),l||window.vuplex.postMessage({type:"stateChange",message:e,current_time:k.getCurrentTime()})}),[k,l]),V=Object(s.useCallback)((function(e){if(null!=k){if(T&&Math.abs(k.getCurrentTime()-e)<.01)return console.log("bonsai: ready-up called while ready"),void F("READY");var t=k.getPlayerState();S?(console.log("bonsai: readying up from "+p(t)+" -> "+e),Y(k),t===f&&k.playVideo(),k.seekTo(e,!0),k.pauseVideo(),k.unMute()):console.log("bonsai: ignoring attempt to ready-up before init")}else console.log("bonsai: ignoring attempt to ready up while player is null")}),[S,k,F,T]),Y=function(e){console.log("bonsai: prepare to ready up"),J(!1),I(!0),e.mute()};Object(s.useEffect)((function(){if(null!=k&&!l){var e=function(e){var t=JSON.parse(e.data);if("video"===t.type)switch(console.log(e.data),t.command){case"play":console.log("COMMAND: play"),k.playVideo();break;case"pause":console.log("COMMAND: pause"),k.pauseVideo();break;case"readyUp":console.log("COMMAND: readyUp"),V(t.timeStamp);break;default:console.log("command: not handled (video) "+e.data)}};return window.vuplex.addEventListener("message",e),function(){window.vuplex.removeEventListener("message",e)}}}),[g,k,l,V]),Object(s.useEffect)((function(){console.log("bonsai: add ping interval");var e=setInterval((function(){var e=0;null!=k&&null!=k.getCurrentTime()&&(e=k.getCurrentTime()),l||window.vuplex.postMessage({type:"infoCurrentTime",current_time:e})}),100);return function(){console.log("bonsai: remove ping interval"),clearInterval(e)}}),[g,k,l]);return Object(n.jsxs)("div",{children:[l?Object(n.jsxs)("div",{children:[Object(n.jsx)("p",{onClick:function(){r.push("/home")},children:"home"})," ",Object(n.jsx)("p",{onClick:function(){V(40)},children:"ready up"})]}):"",Object(n.jsx)(d.a,{opts:o,onReady:function(e){var t=e.target;N(t),Y(t),t.loadVideoById(g,parseFloat(v))},onError:function(e){console.log("bonsai youtube error: "+e)},onStateChange:function(e){switch(e.data){case O:console.log("bonsai: "+p(k.getPlayerState()));break;case h:console.log("bonsai: "+p(k.getPlayerState())+" "+k.getCurrentTime());break;case u:S?R?console.log("bonsai: while readying -> play"):(U?console.log("bonsai: playing after buffer"):console.log("bonsai: playing"),F("PLAYING")):k.pauseVideo(),U&&B(!1);break;case f:S?U?R&&!T?(console.log("bonsai: ready (pause after buffer)"),J(!0),I(!1),F("READY")):(console.log("bonsai: paused after buffering"),F("PAUSED")):R?console.log("bonsai: while readying -> paused"):(console.log("bonsai: paused"),F("PAUSED")):(console.log("bonsai: init complete"),k.seekTo(v,!0),k.unMute(),M(!0),J(!0),I(!1),F("READY")),U&&B(!1);break;case x:R?console.log("bonsai: while readying -> buffering"):console.log("bonsai: buffering"),B(!0);break;case b:console.log("bonsai: "+p(k.getPlayerState())),F("ENDED");break;default:console.log("bonsai error: did not handle state change "+e.data)}}})]})},g=c(39),v=c(49),w=c(164),y=c(75);var k=function(){var e=Object(v.useSpring)((function(){return{x:0,y:0,config:{mass:1,tension:400,friction:5}}})),t=Object(j.a)(e,2),c=t[0],s=c.x,a=c.y,o=t[1],r=Object(y.a)((function(e){var t=e.down,c=Object(j.a)(e.movement,2),n=c[0],s=c[1];o({x:t?n:0,y:t?s:0})}));return Object(n.jsx)("div",{className:"h-screen w-full flex flex-wrap content-center justify-center",children:Object(n.jsx)(w.animated.div,Object(g.a)(Object(g.a)({},r()),{},{className:"bg-red-400 h-10 w-10 rounded-lg",style:{x:s,y:a}}))})},N=c(77),C=c.n(N),_=function(){var e={url:"https://www.twitch.tv/hamletva",width:window.innerWidth,height:window.innerHeight};return Object(n.jsx)(C.a,Object(g.a)(Object(g.a)({},e),{},{onProgress:function(e){var t=e.played,c=e.loaded,n=e.playedSeconds,s=e.loadedSeconds;console.log(t,c,n,s)}}))},S=(c(118),c(15));function M(){Object(S.postJson)({Type:"event",Message:"mouseDown"})}function D(){Object(S.postJson)({Type:"event",Message:"mouseUp"})}function E(){Object(S.postJson)({Type:"event",Message:"hover"})}function T(e){var t=e.className,c=void 0===t?"":t;return Object(n.jsx)("div",{onPointerEnter:E,onPointerDown:M,onPointerUp:D,className:c,children:e.children})}function J(e){var t=e.handleClick,c=e.className,s=void 0===c?"":c;return Object(n.jsx)(T,{children:Object(n.jsx)("div",{className:s,onPointerDown:t,children:e.children})})}var L=c(42),P=c.n(L),R=c.p+"static/media/door-open.d2c81c6b.svg",I=c.p+"static/media/link.2f9ed73a.svg",A=c.p+"static/media/thinking-face.179ede86.svg",H=c(14),U=c(17),B=c(6),F=c(33),V=a.a.createContext(),Y=function(){return Object(s.useContext)(V)},W=new(function(){function e(){Object(H.a)(this,e),this.ip_address=null,this.port=null,this.network_state=null,this.loading_room_code=!1,this._refresh_room_code_handler=null,this.player_info=[],this._room_open=!1,this._room_code=null,Object(B.f)(this)}return Object(U.a)(e,[{key:"refreshRoomCode",value:function(){var e=this;P()({method:"post",url:"https://api.desk.link"+"/rooms/".concat(W.room_code,"/refresh")}).then((function(e){})).catch((function(t){console.log(t),e.room_code=null}))}},{key:"room_open",get:function(){return this._room_open},set:function(e){this._room_open=e,e||(this.room_code="")}},{key:"room_code",get:function(){return this._room_code},set:function(e){var t=this;this._room_code=e,e?this._refresh_room_code_handler=setInterval((function(){t.room_code&&t.refreshRoomCode()}),1e3):(clearInterval(this._refresh_room_code_handler),this._refresh_room_code_handler=null)}}]),e}());Object.seal(W);var K=Object(B.b)((function(e){e.forEach((function(e){W[e.Key]=e.Val}))})),q=Object(B.b)((function(e){for(var t in e)W[t]=e[t]})),z=Object(B.b)((function(e){W[e.Key]=e.Val}));var G=Object(F.a)((function(e){var t=e.children;return function(){var e=Object(s.useState)(!1),t=Object(j.a)(e,2),c=t[0],n=t[1];Object(s.useEffect)((function(){if(!c){n(!0);var e=function(e){var t=JSON.parse(e.data);switch(t.Type){case"command":switch(t.Message){case"pushStore":K(t.Data);break;case"pushStoreSingle":z(t.Data);break;default:console.log("message not handled "+e.data)}}};null!=window.vuplex?(console.log("bonsai: vuplex is not null -> storeListeners"),window.vuplex.addEventListener("message",e)):(console.log("bonsai: vuplex is null"),window.addEventListener("vuplexready",(function(t){console.log("bonsai: vuplexready -> storeListeners"),window.vuplex.addEventListener("message",e)})))}}),[c])}(),Object(n.jsx)(V.Provider,{value:{store:W,pushStore:q,pushStoreList:K},children:t})})),$=c(55),Q="https://api.desk.link",X="py-4 px-8 font-bold bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer flex flex-wrap content-center",Z="py-4 px-8 font-bold bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer flex flex-wrap content-center";function ee(){Object(S.postJson)({Type:"command",Message:"openRoom"})}function te(){Object(S.postJson)({Type:"command",Message:"closeRoom"})}function ce(){Object(S.postJson)({Type:"command",Message:"leaveRoom"})}function ne(e){var t=e.selected,c=e.handleClick,s=t?"py-4 px-8 bg-blue-700 text-white rounded cursor-pointer flex flex-wrap content-center":"py-4 px-8 hover:bg-gray-800 active:bg-gray-900 hover:text-white rounded cursor-pointer flex flex-wrap content-center";return Object(n.jsx)(J,{className:s,handleClick:c,children:e.children})}function se(e){return Object(n.jsx)("div",{className:"space-y-1 px-2",children:e.children})}function ae(e){return Object(n.jsx)("div",{className:"text-white font-bold text-xl px-5 pt-5 pb-2",children:e.children})}function oe(e){var t=e.handleClick,c=e.char;return Object(n.jsx)(J,{className:"bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center",handleClick:function(){t(c)},children:Object(n.jsx)("span",{className:"w-full text-center",children:c})})}function re(e){var t=e.info,c=t.Name,s=t.ConnectionId;return 0===s?Object(n.jsx)(ie,{title:"You",slug:"".concat(s),imgSrc:A}):Object(n.jsx)(ie,{title:c,slug:s,imgSrc:A,children:Object(n.jsx)(J,{handleClick:function(){var e;e=s,Object(S.postJson)({Type:"command",Message:"kickConnectionId",Data:e})},className:X,children:"Kick"})})}function ie(e){return Object(n.jsxs)("div",{className:"flex w-full justify-between",children:[Object(n.jsxs)("div",{className:"flex w-auto",children:[Object(n.jsx)("div",{className:"flex flex-wrap content-center  p-2 mr-2",children:Object(n.jsx)("img",{className:"h-9 w-9",src:e.imgSrc,alt:""})}),Object(n.jsxs)("div",{className:"my-auto",children:[Object(n.jsx)("div",{className:"text-xl",children:e.title}),Object(n.jsx)("div",{className:"text-gray-400",children:e.slug})]})]}),e.children]})}function le(e){var t=e.name;return Object(n.jsxs)("div",{className:"text-white p-4 h-full pr-8",children:[t?Object(n.jsx)("div",{className:"pb-8 text-xl",children:t}):"",Object(n.jsx)("div",{className:"space-y-8",children:e.children})]})}var je=function(){return Object(n.jsx)("div",{className:"flex",children:Object(n.jsx)(ie,{title:"Connected",slug:"You are connected to a host",imgSrc:I,children:Object(n.jsx)(J,{handleClick:ce,className:X,children:"Exit"})})})},de=Object(F.a)((function(){var e=Y().store,t=Object(n.jsx)(ie,{title:"Room",slug:"Invite others",imgSrc:R,children:Object(n.jsx)(J,{className:"py-4 px-8 font-bold bg-green-800 active:bg-green-700 hover:bg-green-600 rounded cursor-pointer flex flex-wrap content-center",handleClick:ee,children:"Open Up"})}),c=Object(n.jsx)(ie,{title:"Room",slug:"Ready to accept connections",imgSrc:R,children:Object(n.jsx)(J,{className:X,handleClick:te,children:"Close"})});return e.room_open?Object(n.jsxs)(a.a.Fragment,{children:[c,Object(n.jsx)(ie,{title:"Desk Code",slug:"People who have this can join you",imgSrc:I,children:Object(n.jsx)("div",{className:"h-20 flex flex-wrap content-center",children:e.room_code?Object(n.jsx)("div",{className:"text-5xl ",children:e.room_code}):Object(n.jsx)("div",{className:"py-4 px-8 font-bold bg-gray-800 rounded flex flex-wrap content-center",children:Object(n.jsx)($.BeatLoader,{size:8,color:"#737373"})})})})]}):Object(n.jsx)(a.a.Fragment,{children:t})})),he=Object(F.a)((function(){var e=Y().store;return Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsx)(de,{}),e.player_info.length>0&&e.room_open?Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsx)("div",{className:"text-xl",children:"People in Your Room"}),e.player_info.map((function(e){return Object(n.jsx)(re,{info:e})}))]}):""]})})),be=function(){return Object(n.jsx)("div",{className:"flex justify-center w-full flex-wrap",children:Object(n.jsx)($.BounceLoader,{size:200,color:"#737373"})})};var ue=[{name:"Home",component:Object(F.a)((function(){var e;switch(Y().store.network_state){case"Neutral":case"HostWaiting":case"Hosting":e=Object(n.jsx)(he,{});break;case"ClientConnected":e=Object(n.jsx)(je,{});break;default:e=Object(n.jsx)(be,{})}return Object(n.jsx)(le,{name:"Home",children:e})}))},{name:"Join Desk",component:function(e){var t=e.navHome,c=Object(s.useState)(""),a=Object(j.a)(c,2),o=a[0],r=a[1],i=Object(s.useState)(!1),l=Object(j.a)(i,2),d=l[0],h=l[1],b=Object(s.useState)(""),u=Object(j.a)(b,2),f=u[0],x=u[1];function O(e){switch(console.log("handleclick"),x(""),o.length){case 4:r(e);break;default:r(o+e)}}return Object(s.useEffect)((function(){if(!d&&4===o.length){var e=Q+"/rooms/".concat(o);console.log(e),P()({method:"get",url:e}).then((function(e){var c;c=e.data,Object(S.postJson)({Type:"command",Message:"joinRoom",data:JSON.stringify(c)}),t(),r(""),h(!1)})).catch((function(e){console.log(e),x("Could not find ".concat(o," try again")),r(""),h(!1)}))}}),[d,o,t]),Object(n.jsx)(le,{name:"Join Desk",children:Object(n.jsxs)("div",{className:"flex flex-wrap w-full content-center",children:[Object(n.jsxs)("div",{className:" w-1/2",children:[Object(n.jsx)("div",{className:"text-xl",children:f}),Object(n.jsx)("div",{className:"text-9xl h-full flex flex-wrap content-center justify-center",children:o.length<4?o:""})]}),Object(n.jsxs)("div",{className:"p-2 rounded space-y-4 text-2xl",children:[Object(n.jsxs)("div",{className:"flex space-x-4",children:[Object(n.jsx)(oe,{handleClick:O,char:"L"}),Object(n.jsx)(oe,{handleClick:O,char:"R"}),Object(n.jsx)(oe,{handleClick:O,char:"C"})]}),Object(n.jsxs)("div",{className:"flex space-x-4",children:[Object(n.jsx)(oe,{handleClick:O,char:"D"}),Object(n.jsx)(oe,{handleClick:O,char:"E"}),Object(n.jsx)(oe,{handleClick:O,char:"F"})]}),Object(n.jsxs)("div",{className:"flex space-x-4",children:[Object(n.jsx)(oe,{handleClick:O,char:"G"}),Object(n.jsx)(oe,{handleClick:O,char:"H"}),Object(n.jsx)(oe,{handleClick:O,char:"I"})]}),Object(n.jsx)("div",{className:"flex flex-wrap w-full justify-around",children:Object(n.jsx)(oe,{handleClick:function(){o.length>0&&r(o.slice(0,o.length-1))},char:"<"})})]})]})})}},{name:"Contacts",component:function(){return Object(n.jsx)(le,{name:"Contacts"})}},{name:"Settings",component:Object(F.a)((function(){var e=Y().store,t=Object(B.b)((function(e){e.ip_address=1234,e.port=4321})),c=Object(B.b)((function(e){e.ip_address=null,e.port=null})),s=Object(B.b)((function(e,t){e.network_state=t})),a=Object(B.b)((function(e){e.player_info.length>0?e.player_info.push({Name:"cam",ConnectionId:1}):e.player_info.push({Name:"cam",ConnectionId:0})})),o=Object(B.b)((function(e){e.player_info.pop()})),r=Object(B.b)((function(e){e.room_open=!e.room_open}));return Object(n.jsxs)(le,{name:"Settings",children:[Object(n.jsxs)("div",{className:"flex space-x-2",children:[Object(n.jsx)(J,{handleClick:function(){s(e,"Neutral")},className:Z,children:"Neutral"}),Object(n.jsx)(J,{handleClick:function(){s(e,"HostWaiting")},className:Z,children:"HostWaiting"}),Object(n.jsx)(J,{handleClick:function(){s(e,"Hosting")},className:Z,children:"Hosting"}),Object(n.jsx)(J,{handleClick:function(){s(e,"ClientConnected")},className:Z,children:"ClientConnected"})]}),Object(n.jsxs)("div",{className:"flex space-x-2",children:[Object(n.jsx)(J,{className:Z,handleClick:function(){t(e)},children:"+ fake ip/port"}),Object(n.jsx)(J,{className:Z,handleClick:function(){c(e)},children:"- fake ip/port"}),Object(n.jsx)(J,{handleClick:function(){a(e)},className:Z,children:"+ fake client"}),Object(n.jsx)(J,{handleClick:function(){o(e)},className:Z,children:"- fake client"})]}),Object(n.jsx)(J,{handleClick:function(){r(e)},className:Z,children:"toggle room open"}),Object(n.jsx)("div",{className:"flex space-x-2"}),Object(n.jsx)("ul",{children:Object.entries(e).map((function(e){return Object(n.jsxs)("li",{children:[e[0],": ",fe(e)]},e[0])}))})]})}))}];function fe(e){switch(e[0]){case"player_info":return"["+e[1].map((function(e){return"(".concat(e.Name,", ").concat(e.ConnectionId,")")})).join(" ")+"]";default:return e[1]?e[1].toString():""}}var xe=function(){var e=Y(),t=e.store,c=e.pushStore,a=Object(s.useState)(0),o=Object(j.a)(a,2),r=o[0],i=o[1],l=ue[r].component;return Object(s.useEffect)((function(){Object(B.c)((function(){if(t.room_code&&(!t.ip_address||!t.port||!t.room_open))return console.log("rm room code"),void c({room_code:null});if(t.room_open&&!t.room_code&&!t.loading_room_code&&t.ip_address&&t.port){console.log("fetch room code"),c({loading_room_code:!0});P()({method:"post",url:"https://api.desk.link/rooms",data:"ip_address=".concat(t.ip_address,"&port=").concat(t.port),header:{"content-type":"application/x-www-form-urlencoded"}}).then((function(e){c({room_code:e.data.tag,loading_room_code:!1})})).catch((function(e){console.log(e),c({loading_room_code:!1})}))}}))})),Object(s.useEffect)((function(){return function(){c({room_code:null})}}),[c]),Object(n.jsxs)("div",{className:"flex text-lg text-gray-500 h-full",children:[Object(n.jsxs)("div",{className:"w-4/12 bg-black overflow-auto scrollhost static",children:[Object(n.jsx)("div",{className:"w-4/12 bg-black fixed",children:Object(n.jsx)(ae,{children:"Menu"})}),Object(n.jsx)("div",{className:"h-16"}),Object(n.jsx)(se,{children:ue.map((function(e,t){return Object(n.jsx)(ne,{handleClick:function(){i(t)},selected:r===t,children:e.name},e.name)}))})]}),Object(n.jsx)("div",{className:"bg-gray-900 z-10 w-full overflow-auto scrollhost",children:Object(n.jsx)(l,{navHome:function(){i(0)}})})]})},Oe=c.p+"static/media/caret-square-up-hollow.5d65dc16.svg",pe=c.p+"static/media/caret-square-up.bce15253.svg",me=c.p+"static/media/backspace.85903aa4.svg",ge=c.p+"static/media/backspace-hollow.5cb2158e.svg";function ve(e){console.log(e),Object(S.postJson)({Type:"event",Message:"keyPress",Data:e})}function we(e){var t,c=e.char,s=e.shift,a=e.handleClick,o=e.stretch,r=void 0!==o&&o,i=e.className,l=s?c.toUpperCase():c;return t=i||(r?"bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer h-20 flex flex-wrap content-center":"bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center"),Object(n.jsx)(J,{children:Object(n.jsx)("div",{className:t,onMouseDown:function(){ve(l),a&&a()},children:Object(n.jsx)("span",{className:"w-full text-center text-white text-3xl",children:l})})})}function ye(){var e=Object(s.useState)(!1),t=Object(j.a)(e,2),c=t[0],a=t[1],o="hidden h-10 w-10 absolute bottom-0 left-0",r="h-10 w-10 absolute -bottom-5 left-5";return Object(n.jsx)(J,{children:Object(n.jsx)("div",{onMouseDown:function(){a(!0),ve("Backspace")},onMouseUp:function(){a(!1)},className:"bg-gray-900 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center",children:Object(n.jsxs)("div",{className:"relative w-full flex justify-center",children:[Object(n.jsx)("img",{className:c?r:o,src:me,alt:""}),Object(n.jsx)("img",{className:c?o:r,src:ge,alt:""})]})})})}function ke(e){var t=e.shift,c=e.toggleShift,s="hidden h-10 w-10 absolute bottom-0 left-0",a="h-10 w-10 absolute -bottom-5 left-5";return Object(n.jsx)(J,{children:Object(n.jsx)("div",{onMouseDown:c,className:t?"bg-gray-600 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center":"bg-gray-900 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center",children:Object(n.jsxs)("div",{className:"relative w-full flex justify-center",children:[Object(n.jsx)("img",{className:t?a:s,src:pe,alt:""}),Object(n.jsx)("img",{className:t?s:a,src:t?pe:Oe,alt:""})]})})})}function Ne(e){var t,c=e.handleClick;t=0===e.level;return Object(n.jsx)(J,{children:Object(n.jsx)("div",{onMouseDown:function(){c()},className:"bg-gray-900 hover:bg-gray-600 rounded cursor-pointer w-24 h-20 flex flex-wrap content-center",children:Object(n.jsx)("div",{className:"relative w-full flex justify-center text-white text-3xl",children:t?".?123":"ABC"})})})}function Ce(e){var t,c=e.handleClick;t=1!==e.level;return Object(n.jsx)(J,{children:Object(n.jsx)("div",{onMouseDown:function(){c()},className:"bg-gray-900 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center",children:Object(n.jsx)("div",{className:"relative w-full flex justify-center text-white text-3xl",children:t?"123":"#+="})})})}function _e(e){var t=e.char,c=e.shift?t.toUpperCase():t;return Object(n.jsx)(J,{children:Object(n.jsx)("div",{className:"bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer w-full h-20 flex flex-wrap content-center",onMouseDown:function(){ve(" ")},children:Object(n.jsx)("span",{className:"w-96 text-center text-white text-3xl",children:c})})})}function Se(){return Object(n.jsx)(we,{className:"bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer w-32 h-20 flex flex-wrap content-center",char:"Enter"})}var Me=function(e){var t=Object(s.useState)(!1),c=Object(j.a)(t,2),o=c[0],r=c[1],i=Object(s.useState)(0),l=Object(j.a)(i,2),d=l[0],h=l[1],b=Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsxs)("div",{className:"flex space-x-2 justify-center",children:[Object(n.jsx)(we,{shift:o,char:"q"}),Object(n.jsx)(we,{shift:o,char:"w"}),Object(n.jsx)(we,{shift:o,char:"e"}),Object(n.jsx)(we,{shift:o,char:"r"}),Object(n.jsx)(we,{shift:o,char:"t"}),Object(n.jsx)(we,{shift:o,char:"y"}),Object(n.jsx)(we,{shift:o,char:"u"}),Object(n.jsx)(we,{shift:o,char:"i"}),Object(n.jsx)(we,{shift:o,char:"o"}),Object(n.jsx)(we,{shift:o,char:"p"}),Object(n.jsx)(ye,{})]}),Object(n.jsxs)("div",{className:"flex space-x-2 justify-end",children:[Object(n.jsx)(we,{shift:o,char:"a"}),Object(n.jsx)(we,{shift:o,char:"s"}),Object(n.jsx)(we,{shift:o,char:"d"}),Object(n.jsx)(we,{shift:o,char:"f"}),Object(n.jsx)(we,{shift:o,char:"g"}),Object(n.jsx)(we,{shift:o,char:"h"}),Object(n.jsx)(we,{shift:o,char:"j"}),Object(n.jsx)(we,{shift:o,char:"k"}),Object(n.jsx)(we,{shift:o,char:"l"}),Object(n.jsx)(Se,{})]}),Object(n.jsxs)("div",{className:"flex space-x-2 justify-center",children:[Object(n.jsx)(ke,{shift:o,toggleShift:function(){r(!o)}}),Object(n.jsx)(we,{shift:o,char:"z"}),Object(n.jsx)(we,{shift:o,char:"x"}),Object(n.jsx)(we,{shift:o,char:"c"}),Object(n.jsx)(we,{shift:o,char:"v"}),Object(n.jsx)(we,{shift:o,char:"b"}),Object(n.jsx)(we,{shift:o,char:"n"}),Object(n.jsx)(we,{shift:o,char:"m"}),Object(n.jsx)(we,{shift:o,char:","}),Object(n.jsx)(we,{shift:o,char:"."}),Object(n.jsx)(ke,{shift:o,toggleShift:function(){r(!o)}})]})]}),u=Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsxs)("div",{className:"flex space-x-2 justify-end",children:[Object(n.jsx)(we,{shift:o,char:"@"}),Object(n.jsx)(we,{shift:o,char:"#"}),Object(n.jsx)(we,{shift:o,char:"$"}),Object(n.jsx)(we,{shift:o,char:"&"}),Object(n.jsx)(we,{shift:o,char:"*"}),Object(n.jsx)(we,{shift:o,char:"("}),Object(n.jsx)(we,{shift:o,char:")"}),Object(n.jsx)(we,{shift:o,char:"'"}),Object(n.jsx)(we,{shift:o,char:'"'}),Object(n.jsx)(Se,{})]}),Object(n.jsxs)("div",{className:"flex space-x-2 justify-end",children:[Object(n.jsx)(Ce,{level:d,handleClick:O}),Object(n.jsx)(we,{shift:o,char:"%"}),Object(n.jsx)(we,{shift:o,char:"-"}),Object(n.jsx)(we,{shift:o,char:"+"}),Object(n.jsx)(we,{shift:o,char:"="}),Object(n.jsx)(we,{shift:o,char:"/"}),Object(n.jsx)(we,{shift:o,char:";"}),Object(n.jsx)(we,{shift:o,char:":"}),Object(n.jsx)(we,{shift:o,char:","}),Object(n.jsx)(we,{shift:o,char:"."}),Object(n.jsx)(Ce,{level:d,handleClick:O})]})]}),f=Object(n.jsx)(a.a.Fragment,{children:Object(n.jsxs)("div",{className:"flex space-x-2 justify-center",children:[Object(n.jsx)(we,{shift:o,char:"1"}),Object(n.jsx)(we,{shift:o,char:"2"}),Object(n.jsx)(we,{shift:o,char:"3"}),Object(n.jsx)(we,{shift:o,char:"4"}),Object(n.jsx)(we,{shift:o,char:"5"}),Object(n.jsx)(we,{shift:o,char:"6"}),Object(n.jsx)(we,{shift:o,char:"7"}),Object(n.jsx)(we,{shift:o,char:"8"}),Object(n.jsx)(we,{shift:o,char:"9"}),Object(n.jsx)(we,{shift:o,char:"0"}),Object(n.jsx)(ye,{})]})}),x=Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsxs)("div",{className:"flex space-x-2 justify-end",children:[Object(n.jsx)(we,{shift:o,char:"\u20ac"}),Object(n.jsx)(we,{shift:o,char:"\xa3"}),Object(n.jsx)(we,{shift:o,char:"\xa5"}),Object(n.jsx)(we,{shift:o,char:"_"}),Object(n.jsx)(we,{shift:o,char:"^"}),Object(n.jsx)(we,{shift:o,char:"["}),Object(n.jsx)(we,{shift:o,char:"]"}),Object(n.jsx)(we,{shift:o,char:"{"}),Object(n.jsx)(we,{shift:o,char:"}"}),Object(n.jsx)(Se,{})]}),Object(n.jsxs)("div",{className:"flex space-x-2 justify-center",children:[Object(n.jsx)(Ce,{level:d,handleClick:O}),Object(n.jsx)(we,{shift:o,char:"\xa7"}),Object(n.jsx)(we,{shift:o,char:"|"}),Object(n.jsx)(we,{shift:o,char:"~"}),Object(n.jsx)(we,{shift:o,char:"\u2026"}),Object(n.jsx)(we,{shift:o,char:"\\"}),Object(n.jsx)(we,{shift:o,char:"<"}),Object(n.jsx)(we,{shift:o,char:">"}),Object(n.jsx)(we,{shift:o,char:"!"}),Object(n.jsx)(we,{shift:o,char:"?"}),Object(n.jsx)(Ce,{level:d,handleClick:O})]})]});function O(){switch(d){case 1:h(2);break;default:h(1)}}function p(){switch(d){case 0:h(1);break;default:h(0)}}return Object(n.jsx)("div",{className:"w-full h-screen bg-black flex flex-wrap justify-center content-center",children:Object(n.jsxs)("div",{className:"space-y-2",children:[0===d?b:"",1===d||2===d?f:"",1===d?u:"",2===d?x:"",Object(n.jsxs)("div",{className:"w-full flex space-x-2 justify-between",children:[Object(n.jsx)(Ne,{level:d,handleClick:p}),Object(n.jsx)(_e,{}),Object(n.jsx)("div",{className:"flex space-x-2",children:Object(n.jsx)(Ne,{level:d,handleClick:p})})]})]})})},De=c.p+"static/media/close.0769b092.svg",Ee=c.p+"static/media/back.d3a137ff.svg",Te=c.p+"static/media/forward.949d2099.svg",Je=c.p+"static/media/keyboard.caceef28.svg",Le=c.p+"static/media/keyboard-dismiss.24998ccf.svg";var Pe=function(e){var t,c=e.imgSrc,s=e.className;return t=s||"bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center",Object(n.jsx)("div",{className:t,children:Object(n.jsx)("div",{className:"relative w-full flex justify-center",children:Object(n.jsx)("img",{className:"h-10 w-10 absolute -bottom-5 left-5",src:c,alt:""})})})};function Re(e){console.log(e),Object(S.postJson)({Type:"command",Message:e})}function Ie(e){var t=e.kbActive,c=e.handleClick;return Object(n.jsx)(J,{className:"w-full flex justify-center",handleClick:c,children:Object(n.jsx)(Pe,{imgSrc:t?Le:Je})})}var Ae=function(){var e=Object(s.useState)(!1),t=Object(j.a)(e,2),c=t[0],a=t[1];return Object(n.jsx)("div",{className:"w-full h-screen bg-black flex flex-wrap content-center justify-center",children:Object(n.jsxs)("div",{className:"space-y-2 mb-2",children:[Object(n.jsx)(J,{className:"w-full flex justify-center",handleClick:function(){Re("closeWeb")},children:Object(n.jsx)(Pe,{className:"bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center",imgSrc:De})}),Object(n.jsxs)("div",{className:"flex space-x-2",children:[Object(n.jsx)(J,{handleClick:function(){Re("navBack")},children:Object(n.jsx)(Pe,{imgSrc:Ee})}),Object(n.jsx)(J,{handleClick:function(){Re("navForward")},children:Object(n.jsx)(Pe,{imgSrc:Te})})]}),Object(n.jsx)(Ie,{kbActive:c,handleClick:function(){Re(c?"dismissKeyboard":"spawnKeyboard"),a(!c)}})]})})};function He(){Object(S.postJson)({Type:"event",Message:"listenersReady",Data:(new Date).getTime()})}function Ue(){console.log("Boot");var e=function(e){return function(t){var c=JSON.parse(t.data);if("nav"===c.type)switch(console.log("asdf"),c.command){case"push":console.log("command: nav "+c.path),e.push(c.path);break;default:console.log("command: not handled (navListeners) "+JSON.stringify(c))}}}(Object(i.g)());return null!=window.vuplex?(console.log("bonsai: vuplex is not null -> navListeners"),window.vuplex.addEventListener("message",e),He()):(console.log("bonsai: vuplex is null"),window.addEventListener("vuplexready",(function(t){console.log("bonsai: vuplexready -> navListeners"),window.vuplex.addEventListener("message",e),He()}))),Object(n.jsxs)("div",{children:["Boot",Object(n.jsxs)("ul",{children:[Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/youtube_test/qEfPBt9dU60/19.02890180001912?x=480&y=360",children:"youtube_test video"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/spring",children:"spring"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/twitch",children:"twitch"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/menu",children:"menu"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/home",children:"home"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/keyboard",children:"keyboard"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/webnav",children:"webnav"})})]})]})}function Be(){return Object(n.jsx)("div",{className:"w-full h-full bg-gray-900"})}var Fe=function(){return console.log("App"),Object(n.jsx)(i.a,{children:Object(n.jsx)("div",{className:"h-screen text-green-400 select-none",children:Object(n.jsxs)(i.d,{children:[Object(n.jsx)(i.b,{path:"/home",component:Be}),Object(n.jsx)(i.b,{path:"/spring",component:k}),Object(n.jsx)(i.b,{path:"/twitch",component:_}),Object(n.jsx)(i.b,{path:"/menu",component:xe}),Object(n.jsx)(i.b,{path:"/keyboard",component:Me}),Object(n.jsx)(i.b,{path:"/webnav",component:Ae}),Object(n.jsx)(i.b,{path:"/youtube/:id/:timeStamp",component:m}),Object(n.jsx)(i.b,{path:"/youtube_test/:id/:timeStamp",component:m}),Object(n.jsx)(i.b,{path:"/",component:Ue})]})})})};r.a.render(Object(n.jsx)(a.a.StrictMode,{children:Object(n.jsx)(G,{children:Object(n.jsx)(Fe,{})})}),document.getElementById("root"))},86:function(e,t,c){}},[[162,1,2]]]);
//# sourceMappingURL=main.35175832.chunk.js.map