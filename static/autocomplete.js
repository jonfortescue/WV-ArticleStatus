// Credit: https://github.com/kraaden/autocomplete; MIT Licensed
!function(e){if("function"==typeof define&&define.amd)define(e);else if("undefined"!=typeof module&&void 0!==module.exports){var t=e();Object.defineProperty(exports,"__esModule",{value:!0}),exports.autocomplete=t,exports.default=t}else window.autocomplete=e()}(function(){"use strict";function e(e){function t(){return"none"!==h.display}function n(){E++,m=[],p=void 0,h.display="none"}function o(){for(;v.firstChild;)v.removeChild(v.firstChild);var t=!1,o="#9?$";m.forEach(function(e){e.group&&(t=!0)});var i=function(e){var t=c.createElement("div");return t.textContent=e.label,t};e.render&&(i=e.render);var l=function(e){var t=c.createElement("div");return t.textContent=e,t};if(e.renderGroup&&(l=e.renderGroup),m.forEach(function(t){if(t.group&&t.group!==o){o=t.group;var r=l(t.group);r&&(r.className+=" group",v.appendChild(r))}var f=i(t);f&&(f.addEventListener("click",function(o){e.onSelect(t.item,u),n(),o.preventDefault(),o.stopPropagation()}),t===p&&(f.className+=" selected"),v.appendChild(f))}),m.length<1){if(!e.emptyMsg)return void n();var f=c.createElement("div");f.className="empty",f.textContent=e.emptyMsg,v.appendChild(f)}var a=u.getBoundingClientRect(),d=a.top+u.offsetHeight+c.body.scrollTop;h.top=d+"px",h.left=a.left+"px",h.width=u.offsetWidth+"px",h.maxHeight=window.innerHeight-(a.top+u.offsetHeight)+"px",h.height="auto",h.display="block",r()}function i(i){var r=i.which||i.keyCode||0,l=++E;38!==r&&13!==r&&27!==r&&39!==r&&37!==r&&0!==r&&(40===r&&t()||(u.value.length>=g?e.fetch(u.value,function(e){E===l&&e&&(m=e,p=m.length>0?m[0]:void 0,o())}):n()))}function r(){var e=v.getElementsByClassName("selected");if(e.length>0){var t=e[0],n=t.previousElementSibling;if(n&&-1!==n.className.indexOf("group")&&!n.previousElementSibling&&(t=n),t.offsetTop<v.scrollTop)v.scrollTop=t.offsetTop;else{var o=t.offsetTop+t.offsetHeight,i=v.scrollTop+v.offsetHeight;o>i&&(v.scrollTop+=o-i)}}}function l(){if(m.length<1)p=void 0;else if(p===m[0])p=m[m.length-1];else for(var e=m.length-1;e>0;e--)if(p===m[e]||1===e){p=m[e-1];break}}function f(){if(m.length<1&&(p=void 0),!p||p===m[m.length-1])return void(p=m[0]);for(var e=0;e<m.length-1;e++)if(p===m[e]){p=m[e+1];break}}function a(i){var r=i.which||i.keyCode||0;if(38===r||40===r||27===r){var a=t();if(27===r)n();else{if(!t||m.length<1)return;38===r?l():f(),o()}return i.preventDefault(),void(a&&i.stopPropagation())}13===r&&p&&(e.onSelect(p.item,u),n())}function d(){setTimeout(function(){c.activeElement!==u&&n()},200)}function s(){u.removeEventListener("keydown",a),u.removeEventListener("keyup",i),u.removeEventListener("focus",i),u.removeEventListener("blur",d),window.removeEventListener("resize",o),n();var e=v.parentNode;e&&e.removeChild(v)}var u,p,c=document,v=c.createElement("div"),h=v.style,m=[],g=e.minLength||2,E=0;if(!e.input)throw new Error("input undefined");return u=e.input,c.body.appendChild(v),v.className="autocomplete "+(e.className||""),h.position="absolute",h.display="none",u.addEventListener("keydown",a),u.addEventListener("keyup",i),u.addEventListener("focus",i),u.addEventListener("blur",d),window.addEventListener("resize",o),{destroy:s}}return e});
