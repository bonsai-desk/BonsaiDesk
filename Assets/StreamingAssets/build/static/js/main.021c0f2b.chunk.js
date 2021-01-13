(this.webpackJsonpBonsaiUI=this.webpackJsonpBonsaiUI||[]).push([[0],{53:function(e,n,a){e.exports=a(76)},58:function(e,n,a){},76:function(e,n,a){"use strict";a.r(n);var t=a(1),o=a.n(t),l=a(27),r=a.n(l),s=(a(58),a(77)),i=a(9),c=a(43),u=-1,d=0,p=1,g=2,b=3,m=5,f=function(e){var n;switch(e){case-1:n="unstarted";break;case 0:n="ended";break;case 1:n="playing";break;case 2:n="paused";break;case 3:n="buffering";break;case 5:n="cued";break;default:n="BAD SWITCH STATE"}return n},v=function(e){var n=new URLSearchParams(e.location.search),a=parseInt(n.get("x")),l=parseInt(n.get("y")),r={width:a&&l?a:window.innerWidth,height:a&&l?l:window.innerHeight,playerVars:{autoplay:0,controls:0,disablekb:1,rel:0}},v=Object(s.d)(),y="youtube_test"===e.location.pathname.split("/")[1],h=e.match.params,E=h.id,w=h.timeStamp,O=Object(t.useState)(null),k=Object(i.a)(O,2),S=k[0],j=k[1],x=Object(t.useState)(!1),C=Object(i.a)(x,2),A=C[0],D=C[1],M=Object(t.useState)(!1),T=Object(i.a)(M,2),N=T[0],I=T[1],L=Object(t.useState)(!1),P=Object(i.a)(L,2),B=P[0],U=P[1],V=Object(t.useState)(!1),R=Object(i.a)(V,2),J=R[0],Y=R[1],_=Object(t.useCallback)((function(e){"READY"!==e&&I(!1),console.log("POST "+e),y||window.vuplex.postMessage({type:"stateChange",message:e,current_time:S.getCurrentTime()})}),[S,y]),H=Object(t.useCallback)((function(e){if(null!=S){if(N&&Math.abs(S.getCurrentTime()-e)<.01)return console.log("bonsai: ready-up called while ready"),void _("READY");var n=S.getPlayerState();A?(console.log("bonsai: readying up from "+f(n)+" -> "+e),W(S),n===g&&S.playVideo(),S.seekTo(e,!0),S.pauseVideo(),S.unMute()):console.log("bonsai: ignoring attempt to ready-up before init")}else console.log("bonsai: ignoring attempt to ready up while player is null")}),[A,S,_,N]),W=function(e){console.log("bonsai: prepare to ready up"),I(!1),U(!0),e.mute()};Object(t.useEffect)((function(){if(null!=S&&!y){var e=function(e){var n=JSON.parse(e.data);if("video"===n.type)switch(console.log(e.data),n.command){case"play":console.log("COMMAND: play"),S.playVideo();break;case"pause":console.log("COMMAND: pause"),S.pauseVideo();break;case"readyUp":console.log("COMMAND: readyUp"),H(n.timeStamp);break;default:console.log("command: not handled (video) "+e.data)}};return window.vuplex.addEventListener("message",e),function(){window.vuplex.removeEventListener("message",e)}}}),[E,S,y,H]),Object(t.useEffect)((function(){console.log("bonsai: add ping interval");var e=setInterval((function(){var e=0;null!=S&&null!=S.getCurrentTime()&&(e=S.getCurrentTime()),y||window.vuplex.postMessage({type:"infoCurrentTime",current_time:e})}),100);return function(){console.log("bonsai: remove ping interval"),clearInterval(e)}}),[E,S,y]);return o.a.createElement("div",null,y?o.a.createElement("div",null,o.a.createElement("p",{onClick:function(){v.push("/home")}},"home")," ",o.a.createElement("p",{onClick:function(){H(40)}},"ready up")):"",o.a.createElement(c.a,{opts:r,onReady:function(e){var n=e.target;j(n),W(n),n.loadVideoById(E,parseFloat(w))},onError:function(e){console.log("bonsai youtube error: "+e)},onStateChange:function(e){switch(e.data){case m:console.log("bonsai: "+f(S.getPlayerState()));break;case u:console.log("bonsai: "+f(S.getPlayerState())+" "+S.getCurrentTime());break;case p:A?B?console.log("bonsai: while readying -> play"):(J?console.log("bonsai: playing after buffer"):console.log("bonsai: playing"),_("PLAYING")):S.pauseVideo(),J&&Y(!1);break;case g:A?J?B&&!N?(console.log("bonsai: ready (pause after buffer)"),I(!0),U(!1),_("READY")):(console.log("bonsai: paused after buffering"),_("PAUSED")):B?console.log("bonsai: while readying -> paused"):(console.log("bonsai: paused"),_("PAUSED")):(console.log("bonsai: init complete"),S.seekTo(w,!0),S.unMute(),D(!0),I(!0),U(!1),_("READY")),J&&Y(!1);break;case b:B?console.log("bonsai: while readying -> buffering"):console.log("bonsai: buffering"),Y(!0);break;case d:console.log("bonsai: "+f(S.getPlayerState())),_("ENDED");break;default:console.log("bonsai error: did not handle state change "+e.data)}}}))},y=a(34),h=a(48);var E=function(){var e=Object(y.useSpring)((function(){return{x:0,y:0,config:{mass:1,tension:400,friction:5}}})),n=Object(i.a)(e,2),a=n[0],t=a.x,l=a.y,r=n[1],s=Object(h.a)((function(e){var n=e.down,a=Object(i.a)(e.movement,2),t=a[0],o=a[1];r({x:n?t:0,y:n?o:0})}));return o.a.createElement("div",{className:"h-screen w-full flex flex-wrap content-center justify-center"},o.a.createElement(y.animated.div,Object.assign({},s(),{className:"bg-red-400 h-10 w-10 rounded-lg",style:{x:t,y:l}})))};var w=function(){console.log("Boot");var e=Object(s.d)(),n=function(e){return function(n){var a=JSON.parse(n.data);if("nav"===a.type)switch(a.command){case"push":console.log("command: nav "+a.path),e.push(a.path);break;default:console.log("command: not handled (navListeners) "+JSON.stringify(a))}}}(e);return null!=window.vuplex?(console.log("bonsai: vuplex is not null -> navListeners"),window.vuplex.addEventListener("message",n)):(console.log("bonsai: vuplex is null"),window.addEventListener("vuplexready",(function(e){console.log("bonsai: vuplexready -> navListeners"),window.vuplex.addEventListener("message",n)}))),o.a.createElement("div",null,"Boot",o.a.createElement("p",{onClick:function(){e.push("/youtube_test/qEfPBt9dU60/19.02890180001912?x=480&y=360")}},"test video"),o.a.createElement("p",{onClick:function(){e.push("/spring")}},"spring"))},O=function(){return o.a.createElement("div",null,"Home")};var k=function(){return console.log("App"),o.a.createElement(s.a,null,o.a.createElement("div",{className:"bg-gray-800 h-screen text-green-400"},o.a.createElement(s.c,null,o.a.createElement(s.b,{path:"/home",component:O}),o.a.createElement(s.b,{path:"/spring",component:E}),o.a.createElement(s.b,{path:"/youtube/:id/:timeStamp",component:v}),o.a.createElement(s.b,{path:"/youtube_test/:id/:timeStamp",component:v}),o.a.createElement(s.b,{path:"/",component:w}))))};r.a.render(o.a.createElement(o.a.StrictMode,null,o.a.createElement(k,null)),document.getElementById("root"))}},[[53,1,2]]]);
//# sourceMappingURL=main.021c0f2b.chunk.js.map