(this["webpackJsonpbonsai-gui"]=this["webpackJsonpbonsai-gui"]||[]).push([[0],{120:function(e,t,c){},14:function(e,t){e.exports={postJson:function(e){null!=window.vuplex&&(console.log("post json "+JSON.stringify(e)),window.vuplex.postMessage(e))}}},164:function(e,t,c){"use strict";c.r(t);var n=c(0),s=c(2),a=c.n(s),i=c(32),r=c.n(i),o=(c(88),c(10)),l=c(28),j=c(5),d=c(74),b=-1,h=0,u=1,f=2,x=3,A=5,O=function(e){var t;switch(e){case-1:t="unstarted";break;case 0:t="ended";break;case 1:t="playing";break;case 2:t="paused";break;case 3:t="buffering";break;case 5:t="cued";break;default:t="BAD SWITCH STATE"}return t},m=function(e){var t=new URLSearchParams(e.location.search),c=parseInt(t.get("x")),a=parseInt(t.get("y")),i={width:c&&a?c:window.innerWidth,height:c&&a?a:window.innerHeight,playerVars:{autoplay:0,controls:0,disablekb:1,rel:0}},r=Object(o.g)(),l="youtube_test"===e.location.pathname.split("/")[1],m=e.match.params,p=m.id,g=m.timeStamp,v=Object(s.useState)(null),w=Object(j.a)(v,2),k=w[0],y=w[1],N=Object(s.useState)(!1),C=Object(j.a)(N,2),S=C[0],M=C[1],D=Object(s.useState)(!1),E=Object(j.a)(D,2),B=E[0],P=E[1],T=Object(s.useState)(!1),J=Object(j.a)(T,2),_=J[0],L=J[1],V=Object(s.useState)(!1),R=Object(j.a)(V,2),U=R[0],F=R[1],W=Object(s.useCallback)((function(e){"READY"!==e&&P(!1),console.log("POST "+e),l||window.vuplex.postMessage({type:"stateChange",message:e,current_time:k.getCurrentTime()})}),[k,l]),K=Object(s.useCallback)((function(e){if(null!=k){if(B&&Math.abs(k.getCurrentTime()-e)<.01)return console.log("bonsai: ready-up called while ready"),void W("READY");var t=k.getPlayerState();S?(console.log("bonsai: readying up from "+O(t)+" -> "+e),z(k),t===f&&k.playVideo(),k.seekTo(e,!0),k.pauseVideo(),k.unMute()):console.log("bonsai: ignoring attempt to ready-up before init")}else console.log("bonsai: ignoring attempt to ready up while player is null")}),[S,k,W,B]),z=function(e){console.log("bonsai: prepare to ready up"),P(!1),L(!0),e.mute()};Object(s.useEffect)((function(){if(null!=k&&!l){var e=function(e){var t=JSON.parse(e.data);if("video"===t.type)switch(console.log(e.data),t.command){case"play":console.log("COMMAND: play"),k.playVideo();break;case"pause":console.log("COMMAND: pause"),k.pauseVideo();break;case"readyUp":console.log("COMMAND: readyUp"),K(t.timeStamp);break;default:console.log("command: not handled (video) "+e.data)}};return window.vuplex.addEventListener("message",e),function(){window.vuplex.removeEventListener("message",e)}}}),[p,k,l,K]),Object(s.useEffect)((function(){console.log("bonsai: add ping interval");var e=setInterval((function(){var e=0,t=1;null!=k&&null!=k.getCurrentTime()&&null!=k.getDuration()&&(e=k.getCurrentTime(),t=k.getDuration()),l||window.vuplex.postMessage({type:"infoCurrentTime",current_time:e,duration:t})}),100);return function(){console.log("bonsai: remove ping interval"),clearInterval(e)}}),[p,k,l]);return Object(n.jsxs)("div",{children:[l?Object(n.jsxs)("div",{children:[Object(n.jsx)("p",{onClick:function(){r.push("/home")},children:"home"})," ",Object(n.jsx)("p",{onClick:function(){K(40)},children:"ready up"})]}):"",Object(n.jsx)(d.a,{opts:i,onReady:function(e){var t=e.target;y(t),z(t),t.loadVideoById(p,parseFloat(g))},onError:function(e){console.log("bonsai youtube error: "+e)},onStateChange:function(e){switch(e.data){case A:console.log("bonsai: "+O(k.getPlayerState()));break;case b:console.log("bonsai: "+O(k.getPlayerState())+" "+k.getCurrentTime());break;case u:S?_?console.log("bonsai: while readying -> play"):(U?console.log("bonsai: playing after buffer"):console.log("bonsai: playing"),W("PLAYING")):k.pauseVideo(),U&&F(!1);break;case f:S?U?_&&!B?(console.log("bonsai: ready (pause after buffer)"),P(!0),L(!1),W("READY")):(console.log("bonsai: paused after buffering"),W("PAUSED")):_?console.log("bonsai: while readying -> paused"):(console.log("bonsai: paused"),W("PAUSED")):(console.log("bonsai: init complete"),k.seekTo(g,!0),k.unMute(),M(!0),P(!0),L(!1),W("READY")),U&&F(!1);break;case x:_?console.log("bonsai: while readying -> buffering"):console.log("bonsai: buffering"),F(!0);break;case h:console.log("bonsai: "+O(k.getPlayerState())),W("ENDED");break;default:console.log("bonsai error: did not handle state change "+e.data)}}})]})},p=c(36),g=c(165),v=c(79);function w(){var e=Object(g.useSpring)((function(){return{x:0,y:0,config:{mass:1,tension:400,friction:5}}})),t=Object(j.a)(e,2),c=t[0],s=c.x,a=c.y,i=t[1],r=Object(v.a)((function(e){var t=e.down,c=Object(j.a)(e.movement,2),n=c[0],s=c[1];i({x:t?n:0,y:t?s:0})}));return Object(n.jsx)("div",{className:"h-screen w-full flex flex-wrap content-center justify-center",children:Object(n.jsx)(g.animated.div,Object(p.a)(Object(p.a)({},r()),{},{className:"bg-red-400 h-10 w-10 rounded-lg",style:{x:s,y:a}}))})}var k=function(){return Object(n.jsx)(w,{})},y=c(80),N=c.n(y),C=function(){var e={url:"https://www.twitch.tv/hamletva",width:window.innerWidth,height:window.innerHeight};return Object(n.jsx)(N.a,Object(p.a)(Object(p.a)({},e),{},{onProgress:function(e){var t=e.played,c=e.loaded,n=e.playedSeconds,s=e.loadedSeconds;console.log(t,c,n,s)}}))},S=(c(120),c(14));function M(){Object(S.postJson)({Type:"event",Message:"mouseUp"})}function D(){Object(S.postJson)({Type:"event",Message:"hover"})}function E(e){var t=e.handleClick,c=e.className,s=void 0===c?"":c,a=e.shouldPostDown,i=void 0===a||a,r=e.shouldPostHover,o=void 0===r||r,l=e.shouldPostUp,j=void 0===l||l;return Object(n.jsx)("div",{onPointerEnter:o?D:null,onPointerDown:function(){t(),i&&Object(S.postJson)({Type:"event",Message:"mouseDown"})},onPointerUp:j?M:null,children:Object(n.jsx)("div",{className:s,children:e.children})})}var B=c(38),P=c.n(B),T=c.p+"static/media/door-open.d2c81c6b.svg",J=c.p+"static/media/link.2f9ed73a.svg",_=c.p+"static/media/thinking-face.179ede86.svg",L=c(13),V=c(16),R=c(6),U=c(29),F=a.a.createContext(),W=function(){return Object(s.useContext)(F)},K=new(function(){function e(){Object(L.a)(this,e),this.ip_address=null,this.port=null,this.network_state=null,this.loading_room_code=!1,this._refresh_room_code_handler=null,this.user_info={},this.player_info=[],this.build="DEVELOPMENT",this.media_info={Active:!1,Name:"None",Paused:!0,Scrub:0,Duration:1},this._room_open=!1,this._room_code=null,Object(R.f)(this)}return Object(V.a)(e,[{key:"refreshRoomCode",value:function(){var e=this;P()({method:"post",url:"https://api.desk.link"+"/rooms/".concat(K.room_code,"/refresh")}).catch((function(t){console.log(t),e.room_code=null}))}},{key:"room_open",get:function(){return this._room_open},set:function(e){this._room_open=e,e||(this.room_code="")}},{key:"room_code",get:function(){return this._room_code},set:function(e){var t=this;this._room_code=e,e?this._refresh_room_code_handler=setInterval((function(){t.room_code&&t.refreshRoomCode()}),1e3):(clearInterval(this._refresh_room_code_handler),this._refresh_room_code_handler=null)}}]),e}());Object.seal(K);var z=Object(R.b)((function(e){e.forEach((function(e){K[e.Key]=e.Val}))})),Y=Object(R.b)((function(e){for(var t in e)K[t]=e[t]})),H=Object(R.b)((function(e){K[e.Key]=e.Val}));var G=Object(U.a)((function(e){var t=e.children;return function(){var e=Object(s.useState)(!1),t=Object(j.a)(e,2),c=t[0],n=t[1];Object(s.useEffect)((function(){if(!c){n(!0);var e=function(e){var t=JSON.parse(e.data);switch(t.Type){case"command":switch(t.Message){case"pushStore":z(t.Data);break;case"pushStoreSingle":H(t.Data);break;default:console.log("message not handled "+e.data)}}};null!=window.vuplex?(console.log("bonsai: vuplex is not null -> storeListeners"),window.vuplex.addEventListener("message",e)):(console.log("bonsai: vuplex is null"),window.addEventListener("vuplexready",(function(t){console.log("bonsai: vuplexready -> storeListeners"),window.vuplex.addEventListener("message",e)})))}}),[c])}(),Object(n.jsx)(F.Provider,{value:{store:K,pushStore:Y,pushStoreList:z},children:t})})),I=c(54),X="https://api.desk.link",q="py-4 px-8 font-bold bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer flex flex-wrap content-center",Q="py-4 px-8 font-bold bg-green-800 active:bg-green-700 hover:bg-green-600 rounded cursor-pointer flex flex-wrap content-center",Z="py-4 px-8 font-bold bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer flex flex-wrap content-center";function $(){Object(S.postJson)({Type:"command",Message:"browseYouTube"})}function ee(){Object(S.postJson)({Type:"command",Message:"openRoom"})}function te(){Object(S.postJson)({Type:"command",Message:"closeRoom"})}function ce(){Object(S.postJson)({Type:"command",Message:"leaveRoom"})}function ne(e){switch(e[0]){case"player_info":return"["+e[1].map((function(e){return"(".concat(e.Name,", ").concat(e.ConnectionId,")")})).join(" ")+"]";case"user_info":return JSON.stringify(e);default:return e[1]?JSON.stringify(e[1],null,2):""}}function se(e){var t=e.selected,c=e.handleClick,s=e.inactive;if(void 0!==s&&s)return Object(n.jsx)("div",{className:"py-4 px-8 bg-gray-800 rounded cursor-pointer flex flex-wrap content-center",children:e.children});var a=t?"py-4 px-8 bg-blue-700 text-white rounded cursor-pointer flex flex-wrap content-center":"py-4 px-8 hover:bg-gray-800 active:bg-gray-900 hover:text-white rounded cursor-pointer flex flex-wrap content-center";return Object(n.jsx)(E,{className:a,handleClick:c,children:e.children})}function ae(e){return Object(n.jsx)("div",{className:"space-y-1 px-2",children:e.children})}function ie(e){return Object(n.jsx)("div",{className:"text-white font-bold text-xl px-5 pt-5 pb-2",children:e.children})}function re(e){var t=e.handleClick,c=e.char;return Object(n.jsx)(E,{className:"bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center",handleClick:function(){t(c)},children:Object(n.jsx)("span",{className:"w-full text-center",children:c})})}function oe(e){var t=e.info,c=t.Name,s=t.ConnectionId;return 0===s?Object(n.jsx)("div",{className:"bg-gray-800 rounded-full p-4 h-20 flex flex-wrap content-center",children:Object(n.jsxs)("div",{className:"flex content-center p-2 space-x-4",children:[Object(n.jsx)("div",{children:Object(n.jsx)("img",{className:"h-9 w-9",src:_,alt:""})}),Object(n.jsx)("div",{children:c})]})}):Object(n.jsx)(E,{className:"bg-gray-800 active:bg-red-700 hover:bg-red-600 rounded-full p-4 cursor-pointer h-20 flex flex-wrap content-center",handleClick:function(){var e;e=s,Object(S.postJson)({Type:"command",Message:"kickConnectionId",Data:e})},children:Object(n.jsxs)("div",{className:"flex content-center p-2 space-x-4",children:[Object(n.jsx)("div",{children:Object(n.jsx)("img",{className:"h-9 w-9",src:_,alt:""})}),Object(n.jsx)("div",{children:c})]})})}function le(e){var t=e.imgSrc,c=e.title,s=e.slug,a=e.children;return Object(n.jsxs)("div",{className:"flex w-full justify-between",children:[Object(n.jsxs)("div",{className:"flex w-auto",children:[Object(n.jsx)("div",{className:"flex flex-wrap content-center  p-2 mr-2",children:Object(n.jsx)("img",{className:"h-9 w-9",src:t,alt:""})}),Object(n.jsxs)("div",{className:"my-auto",children:[Object(n.jsx)("div",{className:"text-xl",children:c}),Object(n.jsx)("div",{className:"text-gray-400",children:s})]})]}),a]})}function je(e){var t=e.name;return Object(n.jsxs)("div",{className:"text-white p-4 h-full pr-8",children:[t?Object(n.jsx)("div",{className:"pb-8 text-xl",children:t}):"",Object(n.jsx)("div",{className:"space-y-8",children:e.children})]})}function de(){return Object(n.jsx)("div",{className:"flex justify-center w-full flex-wrap",children:Object(n.jsx)(I.BounceLoader,{size:200,color:"#737373"})})}function be(){return Object(n.jsx)("div",{className:"flex",children:Object(n.jsx)(le,{title:"Connected",slug:"You are connected to a host",imgSrc:J,children:Object(n.jsx)(E,{handleClick:ce,className:q,children:"Exit"})})})}var he=Object(U.a)((function(){var e=W().store,t=Object(n.jsx)(le,{title:"Room",slug:"Invite others",imgSrc:T,children:Object(n.jsx)(E,{className:Q,handleClick:ee,children:"Open Up"})}),c=Object(n.jsx)(le,{title:"Room",slug:"Ready to accept connections",imgSrc:T,children:Object(n.jsx)(E,{className:q,handleClick:te,children:"Close"})});return e.room_open?Object(n.jsxs)(a.a.Fragment,{children:[c,Object(n.jsx)(le,{title:"Desk Code",slug:"People who have this can join you",imgSrc:J,children:Object(n.jsx)("div",{className:"h-20 flex flex-wrap content-center",children:e.room_code?Object(n.jsx)("div",{className:"text-5xl ",children:e.room_code}):Object(n.jsx)("div",{className:"py-4 px-8 font-bold bg-gray-800 rounded flex flex-wrap content-center",children:Object(n.jsx)(I.BeatLoader,{size:8,color:"#737373"})})})})]}):Object(n.jsx)(a.a.Fragment,{children:t})})),ue=Object(U.a)((function(){var e=W().store;return Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsx)(he,{}),e.player_info.length>0&&e.room_open?Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsx)("div",{className:"text-xl",children:"People in Your Room"}),Object(n.jsx)("div",{className:"flex space-x-2",children:e.player_info.map((function(e){return Object(n.jsx)(oe,{info:e})}))})]}):""]})})),fe=Object(U.a)((function(){var e=W().store,t=Object(s.useRef)(null),c=e.media_info,a=100*c.Scrub/c.Duration;if(console.log(a),!e.media_info.Active)return"";return Object(n.jsx)(je,{name:"Player",children:Object(n.jsx)("div",{ref:t,onPointerDown:function(c){!function(e){Object(S.postJson)({Type:"command",Message:"seekPlayer",Data:e})}((c.clientX-t.current.offsetLeft)/t.current.offsetWidth*e.media_info.Duration)},className:"relative h-16 bg-gray-600",children:Object(n.jsx)("div",{style:{width:a+"%"},className:"h-full bg-gray-400"})})})})),xe=Object(U.a)((function(){var e;switch(W().store.network_state){case"Neutral":case"HostWaiting":case"Hosting":e=Object(n.jsx)(ue,{});break;case"ClientConnected":e=Object(n.jsx)(be,{});break;default:e=Object(n.jsx)(de,{})}return Object(n.jsx)(je,{name:"Home",children:e})}));function Ae(e){var t=e.navHome,c=Object(s.useState)(""),a=Object(j.a)(c,2),i=a[0],r=a[1],o=Object(s.useState)(!1),l=Object(j.a)(o,2),d=l[0],b=l[1],h=Object(s.useState)(""),u=Object(j.a)(h,2),f=u[0],x=u[1];function A(e){switch(x(""),i.length){case 4:r(e);break;default:r(i+e)}}return Object(s.useEffect)((function(){if(!d&&4===i.length){var e=X+"/rooms/".concat(i);console.log(e),P()({method:"get",url:e}).then((function(e){var c;c=e.data,Object(S.postJson)({Type:"command",Message:"joinRoom",data:JSON.stringify(c)}),t(),r(""),b(!1)})).catch((function(e){console.log(e),x("Could not find ".concat(i," try again")),r(""),b(!1)}))}}),[d,i,t]),Object(n.jsx)(je,{name:"Join Desk",children:Object(n.jsxs)("div",{className:"flex flex-wrap w-full content-center",children:[Object(n.jsxs)("div",{className:" w-1/2",children:[Object(n.jsx)("div",{className:"text-xl",children:f}),Object(n.jsx)("div",{className:"text-9xl h-full flex flex-wrap content-center justify-center",children:i.length<4?i:""})]}),Object(n.jsxs)("div",{className:"p-2 rounded space-y-4 text-2xl",children:[Object(n.jsxs)("div",{className:"flex space-x-4",children:[Object(n.jsx)(re,{handleClick:A,char:"A"}),Object(n.jsx)(re,{handleClick:A,char:"B"}),Object(n.jsx)(re,{handleClick:A,char:"C"})]}),Object(n.jsxs)("div",{className:"flex space-x-4",children:[Object(n.jsx)(re,{handleClick:A,char:"D"}),Object(n.jsx)(re,{handleClick:A,char:"E"}),Object(n.jsx)(re,{handleClick:A,char:"F"})]}),Object(n.jsxs)("div",{className:"flex space-x-4",children:[Object(n.jsx)(re,{handleClick:A,char:"G"}),Object(n.jsx)(re,{handleClick:A,char:"H"}),Object(n.jsx)(re,{handleClick:A,char:"I"})]}),Object(n.jsx)("div",{className:"flex flex-wrap w-full justify-around",children:Object(n.jsx)(re,{handleClick:function(){i.length>0&&r(i.slice(0,i.length-1))},char:"<"})})]})]})})}function Oe(){return Object(n.jsx)(je,{name:"Videos",children:Object(n.jsx)(le,{imgSrc:"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAQAAAAEACAYAAABccqhmAAAM2ElEQVR4nO3da4yU1R3H8R97AbkuC4K69UJrxTt4ifRFMY0awYJio1ESb0TTak1tbKKpl8Y3pmqitonpGzXWmJp6N1WjrRirqcVGq1BRg0ukmiKoKLALyGVZkOZ0ztTHPbvjsjvzP89znu8nefLMnGdg5nlmz3/O7TlHAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABENMLirfcEKaXTImmUpJGSWv3z1szjFn+sv8d9t2b/vMn/m2b//4zw++bM6/u+doTfy3+epsxX1OP3vZK+lLTT73dJ2u33uzLHqq/dXeO1fbedmefZx71+yz7e4Z+XlkXmbAlSMFrSREnjJI2XNMHvx/XZxuqr/ejMtk9my2b6kf0EAJMAXAB7+mT+Xh8gstt2HxR2+MfVbaukLyRt8/stfu/SN/utmrbJvw5eGUoAYyQdIKlD0hRJk3wGd/t9/d5t7ZLafIZvIzgmx5VMujNBoUvSRr+t9/tuv/9c0seSPvGBJAqLzJlSAHC/tsdJminpKEnTJU2TdKD/BQf2lis1rJX0oaT3Ja2QtFzSW7700VAEgG92oqSzJP1A0km+OA40msv8b0r6m6RnJL3RiPcjAPTvW5J+IukSSd/u9xWArf9I+qOke/3juiAAfN0MSbdJmhccAfLjRUnXSVo23E9EAKiYKun3ks4MjgD59YKky3wbwpBYZM6mICVfrpC0jsyPApojaY2kX+T5o+e5BPCkpHOCVKB4/uwbq7/cm09e1iqA67P/p+++A1Lxme+pWj3Y8ylrAHhH0jFBKlB8nX6sSs9gzqSMbQB/IvMjYUdIejpPp5enAPArST8KUoG0zJX067ycUV6qACdIWhqkAuk6WdKSWmdXpirAb4MUIG2/ycPZ5SEALPRj+YEymSXp0tjnm4cqwFJfBQDKxt1dePRA51yGKsCZZH6UmLtt/fyYpx87APw0SAHKJWoeiFkFcH2i7wWpQPmc2N/dg6lXARYEKUA5nR3rrGMGgPlBClBO0ea4iFUFOMjPnMKsuEDFdD/v4P+lXAX4Ppkf+JrZMS5HzAAA4CulCgCzghSg3E6KcfYx2gDcIhwf+QU7AFS4lZEO8QuS/E+qbQBHkfmBQEuMuTBiBIAZQQqAKHkjRgA4IkgBoFo3BjVKjABweJACwDnU+irECADTghQAzsHWV8E6AEz0a/sBCLm8sX+Q2kDWAaCDFXyBAbVY/0DGCAAABpZ0ADggSAEQLY9QAgDyxTSPWAcAGgCB2g6qebTOrAMAC34CtSVdBZgapADI2s/yalgHgH2DFAzeKadI50edRRqNN9lyshzLADDSnxyGar/9pEcflZ55RprMpUxUu6Q2q1OzDACT/UhADFVvb+UfnnWWtH69dP31XMr0jLfMJ5YBoM2PdEK93Hab9OGH0sknc0nTYla8swwA7UEKhm/aNOmVV6TFi6UOhlkkIskqAAGgkebMkdaule64Q2rKy6rvGKIkSwBmUa3Urr1W6u6WLrqo7FeiyJIsAUwJUtAY48dLDz4offqpdM45XOTimWT1iS0DwLggBY3lug2ffFJaulSaPp2LXRxJBgA6rmM54QRp5UrpzjtpHyiG8Vaf0vKvganAY7vmGtoHisG1AbRafFLLAGBWrEEN1faBd9+Vjj9+4NchpvFWM2dRAiiro4+Wli2TnnhCmkRszplxVvnFKgC44syEIBXxnXuutGGDdNNNfBn50WbVaG4VAMYwGWjO3XyztG5d5T4DxLaP3xrOKgC4kxkdpCJfpk6t3Gn4wguVLkTEMs6qxGwVAMxOCHVw+umVQUSu23ACX1sEZj+YVgFglFWRBnXkug03baJ9wN7Y1NoAxhIACsy1D3z2mXT22WW/ElbGpBgAaAMosilTpKeekl5/XTqc9V0brMnPoGXyRhZcFaA50sVEPc2aJXV2SvffL41haEcDjbJ4E8tuQKTk0kulzZulRYv4WhsjqW5Aiv8pam6WHnhAWr5cmjmz7Fej3kymz7OsAiBVM2ZIb70lPfKINJF5X+skqTYAegDKYOFCqatLuuGGsl+JeiAAoKBuvbUykGjePL7BoUsqANAIWDZuKPFzz0krVkinnVb2qzEUSbUBmExugBw68kjpxRel55+nfWDvJDUhCFWAsps7t9I+cOONZb8Sg5VUADCpz6AAbrmlctvx/Pl8W7VRAkCi3G3Hzz4rbdzIsmYDMxk5axUAzJY7RoGMHi210jwUk1UA6AlSUF7uFmM3ZsAFgJde4g+hf1/2m1pnBADYcisau96Axx7jwtfWW/NonVgt170jSEG5LFkinXdeZYAQBsMkAFiVAHYFKSiHNWukBQsqjX1k/r2RVAlgW5CCtLkViK64gqL+0Jn8aFqVAKgClIm7F6C9ncw/PDst3sSqBLA9SEF63Nj/yy6rzB+I4UoqAJicDCJZtUq6+GLptdf4BurHJM9YVQEoAaTqqqukww4j89efSRsAjYAYmscfly6/vNLYh0YwGTtjFQB6/Mgmy9WI0QhuWXFX3HdTgKGRTAKAVYbcRk9AwbkZgC+8UDr2WDK/jaTaAL6gHaDAbr+9Mnz3oYfKfiWsuB/MrRbvZTkOgPsBiua++6T995euu07as6fsV8OSy/xbLN7Pqg3A7IRQB6++Kl1wgbR6NVczjh6rKrNlN6BJkQbD4Or5LuPPnk3mj8v9WG62+ASWAYA2gDyr1vMffrjsVyIPdlh1nVt2A24KUhGfm63XDd/95BO+jPzYkloAECWAnPngA+mSSyr1feTNZt9z1nCWA3O6ghTY6+mRrrxSOvRQMn9+bbX6wbQsAdAIGNs990hXX10JAsizLqteAMsAsCFIgY3OzsoknG+/zQUvBpPiv4yrAGYnBc/dl+/G7bvlucj8RbLR6rNalgA+D1LQGK6I74r6rsiPIjK7xdKyBMBIQAt33y1NmEDmL7YkSwDrgxTUj5t2e9GiSvceis6sx8yyBMBAoEZwA3jOOKMy7TaZPxVmecUyAHRbLXdUGm6l3Y4OafHisl+J1JiVlq27AV0QmBQcweC0+K/r5Zcrrftr13Lh0rM91RLAdkYDDpNbTtuN2z/1VDJ/urosewFMlu3OTCXxuqRZwQsAVL0jaYaMMqf1JJ2sGAHUZppHrAMA95wCtZnmEesA8FGQAiBrjeXVsA4AtFwBtX1c82idUQUA8iXpAGB6ckABmZaSYwQAVggCBpZ0APicagAwoI9T7wVwmHAe6N9qq2XBq2IEgFVBCgDH/HbOGAHgvSAFgLPS+irECABMTgf0z3zd9RgB4F3reg5QECusP2aMAOBaOTuDVKDcVsVoH4sRAJw3gxSg3JbFOPtYAWBJkAKU2z9inD0BAMiHv8f4FLECgOvuWB6kAuW0smxVAOcvQQpQTtGmdY4ZAJ4KUoByipYXrCcF7es1Sd8LUoHycAPjZvZ3tilOCtoXC9ih7O6Nef6xSwBN/gaIQ4IjQPrcDMDfkbS1vzMtQwnALRV2V5AKlMNdA2V+K7FLAFWuG2R6kAqky937f5iknQOdYRlKAFW/DFKAtF1fK/NbyUsAeFrS/UEqkKaH/RZdXqoAVW5dtGOCVCAd7o6/YwczOW6ZqgBV8yyXRgaMbfN/47mZGTtvAcAtHbYgSAXS4P6238/TmeQtADiv+HEB/w6OAMX0ke/l+mvePn0eA4B8F8l3Jf0hOAIUy2OSDs7bL39VXgNA1SJJp7GYCArILYLzQ0kL8/zR8x4AnJckdUj6saR1wVEgX9ZL+pmkqZKez/t3k7duwME4U9LPJc1p0McFhsL9UP2unrf2WmTOIgaAqkm+eDVf0mxJbcErgMb5wk9t96ykR/0vf10RAAavWdJxkmb5e6uP9OOsD2j8W6MEPvUDeDr94h1vSPqXpN5GnjoBYPg6fAus2x/o62WTJU2U1O4ft/utzQcSpG+3H3DW5beNft8taYO/TXeNX613td+b/xkTAOyM9EGhLbOvbhP8Nl7SuMw21m9jJO3jt9F+7/6/Vkkt+T/1Qtjtb5zZ6UfRbff7HX503dbM5ormWzKby+ib/b67z74nzydPACieFp/5R/lAMMo/H5l53Jp53pI53uKPtWbSW/zW7Lcm/3xEP8+rwabV7/u+rinzuNkfq752l5+bYbffZx/v9l/hrsxr93zDvvrvdvvnu3zm3eWLzb2ZtGp6T5/HPZnjPT6zZ1+TPJPMCQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABALJL+C2fvjKbAubDfAAAAAElFTkSuQmCC",title:"YouTube",slug:"Find videos to watch on the big screen",children:Object(n.jsx)(E,{className:Q,handleClick:$,children:"Browse"})})})}var me=Object(U.a)((function(){var e=W().store,t=Object(R.b)((function(e){e.ip_address=1234,e.port=4321})),c=Object(R.b)((function(e){e.ip_address=null,e.port=null})),s=Object(R.b)((function(e,t){e.network_state=t})),a=Object(R.b)((function(e){e.player_info.length>0?e.player_info.push({Name:"cam",ConnectionId:1}):e.player_info.push({Name:"loremIpsumLoremIpsumLorem",ConnectionId:0})})),i=Object(R.b)((function(e){e.player_info.pop()})),r=Object(R.b)((function(e){e.room_open=!e.room_open})),o="flex flex-wrap";return Object(n.jsx)(je,{name:"Debug",children:Object(n.jsxs)("div",{className:"flex",children:[Object(n.jsx)("div",{className:"w-1/2",children:Object(n.jsx)("ul",{children:Object.entries(e).map((function(e){return Object(n.jsxs)("li",{className:"mb-2",children:[Object(n.jsx)("span",{className:"font-bold",children:e[0]}),": ",Object(n.jsx)("span",{className:"text-gray-400",children:ne(e)})]},e[0])}))})}),Object(n.jsxs)("div",{className:"w-1/2",children:[Object(n.jsx)("div",{children:"Host State"}),Object(n.jsxs)("div",{className:o,children:[Object(n.jsx)(E,{handleClick:function(){s(e,"Neutral")},className:Z,children:"Neutral"}),Object(n.jsx)(E,{handleClick:function(){s(e,"HostWaiting")},className:Z,children:"HostWaiting"}),Object(n.jsx)(E,{handleClick:function(){s(e,"Hosting")},className:Z,children:"Hosting"}),Object(n.jsx)(E,{handleClick:function(){s(e,"ClientConnected")},className:Z,children:"ClientConnected"})]}),Object(n.jsx)("div",{children:"Connection"}),Object(n.jsxs)("div",{className:o,children:[Object(n.jsx)(E,{className:Z,handleClick:function(){t(e)},children:"+ fake ip/port"}),Object(n.jsx)(E,{className:Z,handleClick:function(){c(e)},children:"- fake ip/port"}),Object(n.jsx)(E,{handleClick:function(){a(e)},className:Z,children:"+ fake client"}),Object(n.jsx)(E,{handleClick:function(){i(e)},className:Z,children:"- fake client"})]}),Object(n.jsx)("div",{children:"Room Status"}),Object(n.jsx)("div",{className:o,children:Object(n.jsx)(E,{handleClick:function(){r(e)},className:Z,children:"toggle room open"})})]})]})})})),pe=Object(U.a)((function(){var e=W(),t=e.store,c=e.pushStore,a=Object(s.useState)(0),i=Object(j.a)(a,2),r=i[0],o=i[1];Object(s.useEffect)((function(){Object(R.c)((function(){if(t.room_code&&(!t.ip_address||!t.port||!t.room_open))return console.log("rm room code"),void c({room_code:null});if(t.room_open&&!t.room_code&&!t.loading_room_code&&t.ip_address&&t.port){console.log("fetch room code"),c({loading_room_code:!0});P()({method:"post",url:"https://api.desk.link/rooms",data:"ip_address=".concat(t.ip_address,"&port=").concat(t.port),header:{"content-type":"application/x-www-form-urlencoded"}}).then((function(e){c({room_code:e.data.tag,loading_room_code:!1})})).catch((function(e){console.log(e),c({loading_room_code:!1})}))}}))})),Object(s.useEffect)((function(){return function(){c({room_code:null})}}),[c]);var l=[{name:"Home",component:xe},{name:"Join Desk",component:Ae},{name:"Videos",component:Oe},{name:"Player",component:fe}];"DEVELOPMENT"===t.build&&l.push({name:"Debug",component:me});var d=l[r].component,b="Hosting"===t.network_state&&!t._room_open;return Object(n.jsxs)("div",{className:"flex text-lg text-gray-500 h-full",children:[Object(n.jsxs)("div",{className:"w-4/12 bg-black overflow-auto scroll-host static",children:[Object(n.jsx)("div",{className:"w-4/12 bg-black fixed",children:Object(n.jsx)(ie,{children:"Menu"})}),Object(n.jsx)("div",{className:"h-16"}),Object(n.jsx)(ae,{children:l.map((function(e,t){return"join desk"!==e.name.toLowerCase()||b?Object(n.jsx)(se,{handleClick:function(){o(t)},selected:r===t,children:e.name},e.name):Object(n.jsx)(se,{inactive:!0,children:e.name},e.name)}))})]}),Object(n.jsx)("div",{className:"bg-gray-900 z-10 w-full overflow-auto scroll-host",children:Object(n.jsx)(d,{navHome:function(){o(0)}})})]})})),ge=c.p+"static/media/backspace-hollow.5cb2158e.svg",ve=c.p+"static/media/caret-square-up.bce15253.svg",we=c.p+"static/media/caret-square-up-hollow.5d65dc16.svg";function ke(e){var t=e.shift,c=e.toggleShift,s="hidden h-10 w-10 absolute bottom-0 left-0",a="h-10 w-10 absolute -bottom-5 left-1";return Object(n.jsx)(ye,{handleClick:c,children:Object(n.jsxs)("div",{className:"relative w-full flex justify-center",children:[Object(n.jsx)("img",{className:t?a:s,src:ve,alt:""}),Object(n.jsx)("img",{className:t?s:a,src:t?ve:we,alt:""})]})})}function ye(e){var t=e.handleClick,c=e.width,a=e.children,i=Object(s.useState)(!1),r=Object(j.a)(i,2),o=r[0],l=r[1],d=Object(g.useSpring)({reset:!0,from:{color:"rgba(".concat(150,",").concat(150,",").concat(150,",1)")},color:"rgba(38,38,38,1)",config:{duration:400}}).color;return Object(n.jsx)(E,{handleClick:t,shouldPostDown:!1,shouldPostUp:!1,shouldPostHover:!1,children:Object(n.jsx)("div",{onClick:function(){return l(!o)},className:"",children:Object(n.jsx)(g.animated.div,{style:{width:c||"5rem",height:"5rem",padding:"1rem",cursor:"pointer",borderRadius:"0.25rem",background:d,display:"flex",flexWrap:"wrap",alignContent:"center"},children:Object(n.jsx)("div",{className:"w-full text-center text-white text-3xl",children:a})})})})}function Ne(e){var t,c=e.handleClick;return t=0===e.level,Object(n.jsx)(ye,{handleClick:c,width:"7em",children:t?".?123":"ABC"})}function Ce(e){var t,c=e.handleClick;return t=1!==e.level,Object(n.jsx)(ye,{handleClick:c,width:"5em",children:Object(n.jsx)("span",{className:"w-full -m-1",children:t?"123":"#+="})})}function Se(e){Object(S.postJson)({Type:"event",Message:"keyPress",Data:e})}function Me(e){var t=e.char,c=e.shift?t.toUpperCase():t;return Object(n.jsx)(ye,{handleClick:function(){Se(c)},children:Object(n.jsx)("span",{className:"w-full text-center text-white text-3xl",children:c})})}function De(){return Object(n.jsx)(ye,{handleClick:function(){Se("Enter")},width:"8rem",children:"Enter"})}function Ee(){return Object(n.jsx)(Be,{handleClick:function(){Se("Backspace")},imgSrc:ge})}function Be(e){var t=e.imgSrc,c=e.handleClick;return Object(n.jsx)(ye,{handleClick:c,children:Object(n.jsx)("div",{className:"relative w-full flex justify-center",children:Object(n.jsx)("img",{className:"h-10 w-10 absolute -bottom-5 left-1",src:t,alt:""})})})}function Pe(){return Object(n.jsx)(ye,{handleClick:function(){Se(" ")},width:"24rem"})}var Te=function(){var e=Object(s.useState)(!1),t=Object(j.a)(e,2),c=t[0],i=t[1],r=Object(s.useState)(0),o=Object(j.a)(r,2),l=o[0],d=o[1],b=Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsxs)("div",{className:"flex space-x-2 justify-center",children:[Object(n.jsx)(Me,{shift:c,char:"q"}),Object(n.jsx)(Me,{shift:c,char:"w"}),Object(n.jsx)(Me,{shift:c,char:"e"}),Object(n.jsx)(Me,{shift:c,char:"r"}),Object(n.jsx)(Me,{shift:c,char:"t"}),Object(n.jsx)(Me,{shift:c,char:"y"}),Object(n.jsx)(Me,{shift:c,char:"u"}),Object(n.jsx)(Me,{shift:c,char:"i"}),Object(n.jsx)(Me,{shift:c,char:"o"}),Object(n.jsx)(Me,{shift:c,char:"p"}),Object(n.jsx)(Ee,{})]}),Object(n.jsxs)("div",{className:"flex space-x-2 justify-end",children:[Object(n.jsx)(Me,{shift:c,char:"a"}),Object(n.jsx)(Me,{shift:c,char:"s"}),Object(n.jsx)(Me,{shift:c,char:"d"}),Object(n.jsx)(Me,{shift:c,char:"f"}),Object(n.jsx)(Me,{shift:c,char:"g"}),Object(n.jsx)(Me,{shift:c,char:"h"}),Object(n.jsx)(Me,{shift:c,char:"j"}),Object(n.jsx)(Me,{shift:c,char:"k"}),Object(n.jsx)(Me,{shift:c,char:"l"}),Object(n.jsx)(De,{})]}),Object(n.jsxs)("div",{className:"flex space-x-2 justify-center",children:[Object(n.jsx)(ke,{shift:c,toggleShift:function(){i(!c)}}),Object(n.jsx)(Me,{shift:c,char:"z"}),Object(n.jsx)(Me,{shift:c,char:"x"}),Object(n.jsx)(Me,{shift:c,char:"c"}),Object(n.jsx)(Me,{shift:c,char:"v"}),Object(n.jsx)(Me,{shift:c,char:"b"}),Object(n.jsx)(Me,{shift:c,char:"n"}),Object(n.jsx)(Me,{shift:c,char:"m"}),Object(n.jsx)(Me,{shift:c,char:","}),Object(n.jsx)(Me,{shift:c,char:"."}),Object(n.jsx)(ke,{shift:c,toggleShift:function(){i(!c)}})]})]}),h=Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsxs)("div",{className:"flex space-x-2 justify-end",children:[Object(n.jsx)(Me,{shift:c,char:"@"}),Object(n.jsx)(Me,{shift:c,char:"#"}),Object(n.jsx)(Me,{shift:c,char:"$"}),Object(n.jsx)(Me,{shift:c,char:"&"}),Object(n.jsx)(Me,{shift:c,char:"*"}),Object(n.jsx)(Me,{shift:c,char:"("}),Object(n.jsx)(Me,{shift:c,char:")"}),Object(n.jsx)(Me,{shift:c,char:"'"}),Object(n.jsx)(Me,{shift:c,char:'"'}),Object(n.jsx)(De,{})]}),Object(n.jsxs)("div",{className:"flex space-x-2 justify-end",children:[Object(n.jsx)(Ce,{level:l,handleClick:x}),Object(n.jsx)(Me,{shift:c,char:"%"}),Object(n.jsx)(Me,{shift:c,char:"-"}),Object(n.jsx)(Me,{shift:c,char:"+"}),Object(n.jsx)(Me,{shift:c,char:"="}),Object(n.jsx)(Me,{shift:c,char:"/"}),Object(n.jsx)(Me,{shift:c,char:";"}),Object(n.jsx)(Me,{shift:c,char:":"}),Object(n.jsx)(Me,{shift:c,char:","}),Object(n.jsx)(Me,{shift:c,char:"."}),Object(n.jsx)(Ce,{level:l,handleClick:x})]})]}),u=Object(n.jsx)(a.a.Fragment,{children:Object(n.jsxs)("div",{className:"flex space-x-2 justify-center",children:[Object(n.jsx)(Me,{shift:c,char:"1"}),Object(n.jsx)(Me,{shift:c,char:"2"}),Object(n.jsx)(Me,{shift:c,char:"3"}),Object(n.jsx)(Me,{shift:c,char:"4"}),Object(n.jsx)(Me,{shift:c,char:"5"}),Object(n.jsx)(Me,{shift:c,char:"6"}),Object(n.jsx)(Me,{shift:c,char:"7"}),Object(n.jsx)(Me,{shift:c,char:"8"}),Object(n.jsx)(Me,{shift:c,char:"9"}),Object(n.jsx)(Me,{shift:c,char:"0"}),Object(n.jsx)(Ee,{})]})}),f=Object(n.jsxs)(a.a.Fragment,{children:[Object(n.jsxs)("div",{className:"flex space-x-2 justify-end",children:[Object(n.jsx)(Me,{shift:c,char:"\u20ac"}),Object(n.jsx)(Me,{shift:c,char:"\xa3"}),Object(n.jsx)(Me,{shift:c,char:"\xa5"}),Object(n.jsx)(Me,{shift:c,char:"_"}),Object(n.jsx)(Me,{shift:c,char:"^"}),Object(n.jsx)(Me,{shift:c,char:"["}),Object(n.jsx)(Me,{shift:c,char:"]"}),Object(n.jsx)(Me,{shift:c,char:"{"}),Object(n.jsx)(Me,{shift:c,char:"}"}),Object(n.jsx)(De,{})]}),Object(n.jsxs)("div",{className:"flex space-x-2 justify-center",children:[Object(n.jsx)(Ce,{level:l,handleClick:x}),Object(n.jsx)(Me,{shift:c,char:"\xa7"}),Object(n.jsx)(Me,{shift:c,char:"|"}),Object(n.jsx)(Me,{shift:c,char:"~"}),Object(n.jsx)(Me,{shift:c,char:"\u2026"}),Object(n.jsx)(Me,{shift:c,char:"\\"}),Object(n.jsx)(Me,{shift:c,char:"<"}),Object(n.jsx)(Me,{shift:c,char:">"}),Object(n.jsx)(Me,{shift:c,char:"!"}),Object(n.jsx)(Me,{shift:c,char:"?"}),Object(n.jsx)(Ce,{level:l,handleClick:x})]})]});function x(){switch(l){case 1:d(2);break;default:d(1)}}function A(){switch(l){case 0:d(1);break;default:d(0)}}return Object(n.jsx)("div",{className:"w-full h-screen bg-black flex flex-wrap justify-center content-center",children:Object(n.jsxs)("div",{className:"space-y-2",children:[0===l?b:"",1===l||2===l?u:"",1===l?h:"",2===l?f:"",Object(n.jsxs)("div",{className:"w-full flex space-x-2 justify-between",children:[Object(n.jsx)(Ne,{level:l,handleClick:A}),Object(n.jsx)(Pe,{}),Object(n.jsx)("div",{className:"flex space-x-2",children:Object(n.jsx)(Ne,{level:l,handleClick:A})})]})]})})},Je=c.p+"static/media/close.0769b092.svg",_e=c.p+"static/media/back.d3a137ff.svg",Le=c.p+"static/media/forward.949d2099.svg",Ve=c.p+"static/media/keyboard.caceef28.svg",Re=c.p+"static/media/keyboard-dismiss.24998ccf.svg";function Ue(e){console.log(e),Object(S.postJson)({Type:"command",Message:e})}function Fe(e){var t=e.kbActive,c=e.handleClick;return Object(n.jsx)("div",{className:"w-full flex justify-center",children:Object(n.jsx)(Be,{handleClick:c,imgSrc:t?Re:Ve})})}var We=function(){var e=Object(s.useState)(!1),t=Object(j.a)(e,2),c=t[0],a=t[1];return Object(n.jsx)("div",{className:"w-full h-screen bg-black flex flex-wrap content-center justify-center",children:Object(n.jsxs)("div",{className:"space-y-2 mb-2",children:[Object(n.jsx)("div",{className:"w-full flex justify-center",children:Object(n.jsx)(Be,{handleClick:function(){Ue("closeWeb")},className:"bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center",imgSrc:Je})}),Object(n.jsxs)("div",{className:"flex space-x-2",children:[Object(n.jsx)(Be,{imgSrc:_e,handleClick:function(){Ue("navBack")}}),Object(n.jsx)(Be,{imgSrc:Le,handleClick:function(){Ue("navForward")}})]}),Object(n.jsx)(Fe,{kbActive:c,handleClick:function(){Ue(c?"dismissKeyboard":"spawnKeyboard"),a(!c)}})]})})};function Ke(){Object(S.postJson)({Type:"event",Message:"listenersReady",Data:(new Date).getTime()})}function ze(){console.log("Boot");var e=function(e){return function(t){var c=JSON.parse(t.data);if("nav"===c.type)switch(console.log("asdf"),c.command){case"push":console.log("command: nav "+c.path),e.push(c.path);break;default:console.log("command: not handled (navListeners) "+JSON.stringify(c))}}}(Object(o.g)());return null!=window.vuplex?(console.log("bonsai: vuplex is not null -> navListeners"),window.vuplex.addEventListener("message",e),Ke()):(console.log("bonsai: vuplex is null"),window.addEventListener("vuplexready",(function(t){console.log("bonsai: vuplexready -> navListeners"),window.vuplex.addEventListener("message",e),Ke()}))),Object(n.jsxs)("div",{children:["Boot",Object(n.jsxs)("ul",{children:[Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/youtube_test/qEfPBt9dU60/19.02890180001912?x=480&y=360",children:"youtube_test video"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/spring",children:"spring"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/twitch",children:"twitch"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/menu",children:"menu"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/home",children:"home"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/keyboard",children:"keyboard"})}),Object(n.jsx)("li",{children:Object(n.jsx)(l.a,{to:"/webnav",children:"webnav"})})]})]})}function Ye(){return Object(n.jsx)("div",{className:"w-full h-full bg-gray-900"})}var He=function(){return console.log("App"),Object(n.jsx)(o.a,{children:Object(n.jsx)("div",{className:"h-screen text-green-400 select-none",children:Object(n.jsxs)(o.d,{children:[Object(n.jsx)(o.b,{path:"/home",component:Ye}),Object(n.jsx)(o.b,{path:"/spring",component:k}),Object(n.jsx)(o.b,{path:"/twitch",component:C}),Object(n.jsx)(o.b,{path:"/menu",component:pe}),Object(n.jsx)(o.b,{path:"/keyboard",component:Te}),Object(n.jsx)(o.b,{path:"/webnav",component:We}),Object(n.jsx)(o.b,{path:"/youtube/:id/:timeStamp",component:m}),Object(n.jsx)(o.b,{path:"/youtube_test/:id/:timeStamp",component:m}),Object(n.jsx)(o.b,{path:"/",component:ze})]})})})};r.a.render(Object(n.jsx)(a.a.StrictMode,{children:Object(n.jsx)(G,{children:Object(n.jsx)(He,{})})}),document.getElementById("root"))},88:function(e,t,c){}},[[164,1,2]]]);
//# sourceMappingURL=main.35884b2a.chunk.js.map