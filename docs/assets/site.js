/* Feeder Editor Tools docs - hành vi dùng chung. Script thường (không phải module) để chạy được trên file://. */
(function () {
  "use strict";
  var doc = document.documentElement;
  var body = document.body;
  var root = body.getAttribute("data-root") || "";
  var reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  var fine = window.matchMedia("(pointer: fine)").matches;
  var hasGsap = !!window.gsap;
  window.__feeder = true;

  function $(s, c) { return (c || document).querySelector(s); }
  function $$(s, c) { return Array.prototype.slice.call((c || document).querySelectorAll(s)); }

  if (hasGsap && window.ScrollTrigger) gsap.registerPlugin(ScrollTrigger);

  /* ---------- Smooth scroll ---------- */
  var lenis = null;
  if (!reduce && window.Lenis) {
    try {
      lenis = new Lenis({ lerp: 0.11, smoothWheel: true, anchors: false });
      if (hasGsap) {
        lenis.on("scroll", function () { if (window.ScrollTrigger) ScrollTrigger.update(); });
        gsap.ticker.add(function (t) { lenis.raf(t * 1000); });
        gsap.ticker.lagSmoothing(0);
      } else {
        var raf = function (t) { lenis.raf(t); requestAnimationFrame(raf); };
        requestAnimationFrame(raf);
      }
    } catch (e) { lenis = null; }
  }
  window.__lenis = lenis;

  function scrollToEl(el) {
    if (lenis) lenis.scrollTo(el, { offset: -84, duration: 1.2 });
    else el.scrollIntoView({ behavior: reduce ? "auto" : "smooth", block: "start" });
  }
  document.addEventListener("click", function (e) {
    var a = e.target.closest && e.target.closest('a[href^="#"]');
    if (!a) return;
    var id = a.getAttribute("href").slice(1);
    var el = id && document.getElementById(id);
    if (!el) return;
    e.preventDefault();
    scrollToEl(el);
    history.replaceState(null, "", "#" + id);
  });

  /* ---------- Topbar ẩn khi cuộn xuống ---------- */
  var bar = $(".topbar"), lastY = 0;
  function onScroll() {
    var y = window.scrollY;
    if (bar) bar.classList.toggle("is-hidden", y > 240 && y > lastY && !body.classList.contains("side-open"));
    lastY = y;
  }
  window.addEventListener("scroll", onScroll, { passive: true });

  /* ---------- Reveal ---------- */
  if (doc.classList.contains("motion") && "IntersectionObserver" in window) {
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (en.isIntersecting) { en.target.classList.add("in"); io.unobserve(en.target); }
      });
    }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });
    $$("[data-reveal]").forEach(function (el) { io.observe(el); });
  } else {
    $$("[data-reveal]").forEach(function (el) { el.classList.add("in"); });
  }
  $$("[data-stagger]").forEach(function (wrap) {
    Array.prototype.forEach.call(wrap.children, function (c, i) { c.style.setProperty("--i", i % 6); });
  });

  /* ---------- Spotlight theo chuột ---------- */
  if (fine) {
    document.addEventListener("pointermove", function (e) {
      var el = e.target.closest && e.target.closest(".btn");
      if (!el) return;
      var r = el.getBoundingClientRect();
      el.style.setProperty("--mx", (e.clientX - r.left) + "px");
      el.style.setProperty("--my", (e.clientY - r.top) + "px");
    }, { passive: true });
  }

  /* ---------- Cursor gizmo ---------- */
  if (fine && !reduce) {
    var cur = document.createElement("div");
    cur.className = "cursor"; cur.setAttribute("aria-hidden", "true");
    body.appendChild(cur);
    var tx = -100, ty = -100, cx = -100, cy = -100, moving = false;
    document.addEventListener("pointermove", function (e) {
      tx = e.clientX; ty = e.clientY;
      if (!moving) { moving = true; cx = tx; cy = ty; cur.classList.add("is-on"); loop(); }
      var link = e.target.closest && e.target.closest("a, button, [data-zoom], input, summary");
      cur.classList.toggle("is-link", !!link);
    }, { passive: true });
    document.addEventListener("pointerdown", function () { cur.classList.add("is-down"); });
    document.addEventListener("pointerup", function () { cur.classList.remove("is-down"); });
    document.addEventListener("mouseleave", function () { cur.classList.remove("is-on"); moving = false; });
    var loop = function () {
      if (!moving) return;
      cx += (tx - cx) * 0.2; cy += (ty - cy) * 0.2;
      cur.style.transform = "translate3d(" + cx.toFixed(1) + "px," + cy.toFixed(1) + "px,0)";
      requestAnimationFrame(loop);
    };
  }

  /* ---------- Copy ---------- */
  function copyText(text) {
    if (navigator.clipboard && window.isSecureContext) return navigator.clipboard.writeText(text);
    return new Promise(function (res, rej) {
      var t = document.createElement("textarea");
      t.value = text; t.style.position = "fixed"; t.style.opacity = "0";
      body.appendChild(t); t.select();
      try { document.execCommand("copy") ? res() : rej(); } catch (e) { rej(e); }
      body.removeChild(t);
    });
  }
  document.addEventListener("click", function (e) {
    var b = e.target.closest && e.target.closest("[data-copy]");
    if (!b) return;
    var label = b.querySelector("span") || b;
    var old = label.textContent;
    copyText(b.getAttribute("data-copy")).then(function () {
      b.classList.add("is-done"); label.textContent = "Đã chép";
      setTimeout(function () { b.classList.remove("is-done"); label.textContent = old; }, 1600);
    }, function () { label.textContent = "Bôi đen để chép"; });
  });

  /* ---------- Lightbox ---------- */
  var lb = null;
  function openLightbox(src, alt) {
    if (!lb) {
      lb = document.createElement("div");
      lb.className = "lightbox"; lb.setAttribute("role", "dialog"); lb.setAttribute("aria-modal", "true");
      lb.innerHTML = '<img alt=""><button type="button" aria-label="Đóng ảnh">&times;</button>';
      body.appendChild(lb);
      lb.addEventListener("click", closeLightbox);
    }
    var img = lb.querySelector("img"); img.src = src; img.alt = alt || "";
    lb.classList.add("is-open");
    if (lenis) lenis.stop();
    lb.querySelector("button").focus();
  }
  function closeLightbox() {
    if (!lb) return;
    lb.classList.remove("is-open");
    if (lenis) lenis.start();
  }
  document.addEventListener("click", function (e) {
    var z = e.target.closest && e.target.closest("[data-zoom]");
    if (!z) return;
    var img = z.querySelector("img");
    var full = z.getAttribute("data-full");
    if (full) openLightbox(root + full, img ? img.alt : "");
    else if (img && img.naturalWidth) openLightbox(img.currentSrc || img.src, img.alt);
  });
  document.addEventListener("keydown", function (e) {
    if (e.key === "Enter" && e.target.matches && e.target.matches("[data-zoom]")) e.target.click();
  });

  /* ---------- Search palette ---------- */
  var tools = window.FEEDER_TOOLS || [];
  var pal = null, input, list, sel = 0, shown = [];
  function norm(s) {
    return s.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/đ/g, "d");
  }
  tools.forEach(function (t) { t._n = norm(t.name); t._s = norm(t.summary + " " + t.group + " " + (t.module || "")); });
  function score(t, q) {
    if (!q) return 1;
    var parts = q.split(/\s+/).filter(Boolean), total = 0;
    for (var i = 0; i < parts.length; i++) {
      var p = parts[i], ni = t._n.indexOf(p);
      if (ni === 0) total += 12;
      else if (ni > 0) total += 8;
      else if (t._s.indexOf(p) >= 0) total += 3;
      else {
        var j = 0, k = 0;
        while (j < t._n.length && k < p.length) { if (t._n[j] === p[k]) k++; j++; }
        if (k === p.length) total += 1; else return 0;
      }
    }
    return total;
  }
  function esc(s) { return s.replace(/[&<>"]/g, function (c) { return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]; }); }
  function render() {
    var q = norm(input.value.trim());
    shown = tools.map(function (t) { return { t: t, s: score(t, q) }; })
      .filter(function (x) { return x.s > 0; })
      .sort(function (a, b) { return b.s - a.s; })
      .map(function (x) { return x.t; });
    sel = Math.min(sel, Math.max(0, shown.length - 1));
    if (!shown.length) { list.innerHTML = '<li class="palette__empty">Không có tool nào khớp. Thử gõ tên ngắn hơn, ví dụ "mesh".</li>'; return; }
    list.innerHTML = shown.map(function (t, i) {
      return '<li><a class="g-' + t.group.toLowerCase() + (i === sel ? " is-sel" : "") + '" href="' + root + "tools/" + t.slug + '.html" data-i="' + i + '">' +
        "<b>" + esc(t.name) + "</b><em>" + (t.module ? "module " + esc(t.module) : esc(t.group)) + "</em><small>" + esc(t.summary) + "</small></a></li>";
    }).join("");
  }
  function moveSel(d) {
    if (!shown.length) return;
    sel = (sel + d + shown.length) % shown.length;
    $$("a", list).forEach(function (a, i) { a.classList.toggle("is-sel", i === sel); if (i === sel) a.scrollIntoView({ block: "nearest" }); });
  }
  function openPalette() {
    if (!pal) {
      pal = document.createElement("dialog");
      pal.className = "palette"; pal.setAttribute("aria-label", "Tìm tool");
      pal.innerHTML = '<div class="palette__in"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>' +
        '<input type="search" placeholder="Tìm tool theo tên hoặc việc nó làm..." autocomplete="off" spellcheck="false" aria-label="Tìm tool"><kbd>Esc</kbd></div>' +
        '<ul role="listbox"></ul><div class="palette__foot"><span><kbd>↑</kbd><kbd>↓</kbd> chọn</span><span><kbd>Enter</kbd> mở</span><span>' + tools.length + " tool</span></div>";
      body.appendChild(pal);
      input = $("input", pal); list = $("ul", pal);
      input.addEventListener("input", function () { sel = 0; render(); });
      input.addEventListener("keydown", function (e) {
        if (e.key === "ArrowDown") { e.preventDefault(); moveSel(1); }
        else if (e.key === "ArrowUp") { e.preventDefault(); moveSel(-1); }
        else if (e.key === "Enter") { var a = $$("a", list)[sel]; if (a) { e.preventDefault(); navigate(a.href); } }
      });
      list.addEventListener("pointermove", function (e) {
        var a = e.target.closest("a"); if (!a) return;
        var i = +a.getAttribute("data-i"); if (i !== sel) { sel = i; $$("a", list).forEach(function (x, j) { x.classList.toggle("is-sel", j === sel); }); }
      });
      pal.addEventListener("click", function (e) { if (e.target === pal) pal.close(); });
      pal.addEventListener("close", function () { if (lenis) lenis.start(); });
    }
    input.value = ""; sel = 0; render();
    if (pal.showModal) pal.showModal(); else pal.setAttribute("open", "");
    if (lenis) lenis.stop();
    input.focus();
  }
  $$("[data-search]").forEach(function (b) { b.addEventListener("click", openPalette); });
  document.addEventListener("keydown", function (e) {
    var typing = /INPUT|TEXTAREA|SELECT/.test(document.activeElement && document.activeElement.tagName);
    if ((e.key === "k" && (e.ctrlKey || e.metaKey)) || (e.key === "/" && !typing)) { e.preventDefault(); openPalette(); }
    if (e.key === "Escape") { closeLightbox(); body.classList.remove("side-open"); }
  });

  /* ---------- Page transition ---------- */
  var nativeVT = /^https?:$/.test(location.protocol) && ("CSSViewTransitionRule" in window);
  var useCurtain = !reduce && !nativeVT;
  function navigate(href) {
    if (!useCurtain) { location.href = href; return; }
    try { sessionStorage.setItem("feeder-curtain", "1"); } catch (e) {}
    doc.classList.remove("is-entering", "is-entered");
    doc.classList.add("is-leaving");
    setTimeout(function () { location.href = href; }, 480);
  }
  if (useCurtain) {
    document.addEventListener("click", function (e) {
      var a = e.target.closest && e.target.closest("a[href]");
      if (!a || e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
      if (a.target && a.target !== "_self") return;
      var href = a.getAttribute("href");
      if (!href || href.charAt(0) === "#" || /^(https?:|mailto:)/.test(href)) return;
      if (a.pathname === location.pathname && a.hash) return;
      e.preventDefault();
      navigate(a.href);
    });
    if (doc.classList.contains("is-entering")) {
      requestAnimationFrame(function () { requestAnimationFrame(function () { doc.classList.add("is-entered"); }); });
      setTimeout(function () { doc.classList.remove("is-entering", "is-entered"); }, 900);
    }
    window.addEventListener("pageshow", function (e) { if (e.persisted) doc.classList.remove("is-leaving", "is-entering", "is-entered"); });
  }

  /* ---------- Trang tool: sidebar, TOC, tiến độ ---------- */
  var menuBtn = $(".menu-btn");
  if (menuBtn) {
    menuBtn.addEventListener("click", function () {
      var open = body.classList.toggle("side-open");
      menuBtn.setAttribute("aria-expanded", open ? "true" : "false");
    });
    document.addEventListener("click", function (e) {
      if (body.classList.contains("side-open") && !e.target.closest(".side") && !e.target.closest(".menu-btn")) body.classList.remove("side-open");
    });
  }
  var current = $('.side a[aria-current="page"]');
  if (current) {
    var side = $(".side");
    if (side && side.scrollHeight > side.clientHeight) side.scrollTop = current.offsetTop - side.clientHeight / 2;
  }
  var tocLinks = $$(".toc a");
  if (tocLinks.length && "IntersectionObserver" in window) {
    var secs = tocLinks.map(function (a) { return document.getElementById(a.getAttribute("href").slice(1)); }).filter(Boolean);
    var spy = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (en.isIntersecting) {
          tocLinks.forEach(function (a) { a.classList.toggle("is-active", a.getAttribute("href") === "#" + en.target.id); });
        }
      });
    }, { rootMargin: "-30% 0px -60% 0px" });
    secs.forEach(function (s) { spy.observe(s); });
  }
  var prog = $(".progress i");
  if (prog) {
    var upd = function () {
      var h = document.documentElement.scrollHeight - innerHeight;
      prog.style.setProperty("--p", h > 0 ? Math.min(1, scrollY / h).toFixed(3) : 1);
    };
    window.addEventListener("scroll", upd, { passive: true }); upd();
  }

  /* ---------- Hero ---------- */
  var hero = $(".hero");
  if (hero) initHero(hero);

  function splitChars(el) {
    $$(".line > span", el).forEach(function (s) {
      var txt = s.textContent; s.textContent = "";
      s.setAttribute("aria-hidden", "true");
      txt.split("").forEach(function (c) {
        var ch = document.createElement("span");
        ch.className = "ch"; ch.textContent = c === " " ? " " : c;
        s.appendChild(ch);
      });
    });
  }

  function initHero(hero) {
    var h1 = $("h1", hero);
    var motion = doc.classList.contains("motion");
    if (motion && hasGsap) {
      splitChars(h1);
      var tl = gsap.timeline({ delay: 0.15 });
      tl.to($$(".ch", h1), { y: 0, duration: 1.1, ease: "expo.out", stagger: 0.028 })
        .to($$("[data-hero-fade]", hero), { opacity: 1, y: 0, duration: 0.9, ease: "power3.out", stagger: 0.08 }, "-=0.75");
      if (window.ScrollTrigger) {
        gsap.to($(".hero__inner", hero), {
          yPercent: -18, opacity: 0.25, ease: "none",
          scrollTrigger: { trigger: hero, start: "top top", end: "bottom top", scrub: true }
        });
      }
    } else {
      doc.classList.remove("motion");
      $$("[data-reveal]").forEach(function (el) { el.classList.add("in"); });
    }

    var stage = $(".hero__stage", hero);
    if (!stage || !hasWebGL()) return;
    var start = function () {
      loadScript(root + "assets/vendor/three.min.js", function () {
        loadScript(root + "assets/hero3d.js", function () {
          if (!window.FeederHero) return;
          try {
            window.FeederHero.init(stage, { reduce: reduce, fine: fine });
            stage.classList.add("is-live");
          } catch (err) { if (window.console) console.warn("Hero 3D tắt:", err.message); }
        });
      });
    };
    if (document.readyState === "complete") idle(start);
    else window.addEventListener("load", function () { idle(start); });
  }

  function idle(fn) { (window.requestIdleCallback || function (f) { setTimeout(f, 200); })(fn, { timeout: 1500 }); }
  function hasWebGL() {
    try {
      var c = document.createElement("canvas");
      return !!(window.WebGLRenderingContext && (c.getContext("webgl2") || c.getContext("webgl")));
    } catch (e) { return false; }
  }
  function loadScript(src, cb) {
    var s = document.createElement("script");
    s.src = src; s.async = true; s.onload = cb;
    s.onerror = function () { if (window.console) console.info("Không tải được " + src + ", giữ ảnh tĩnh."); };
    document.head.appendChild(s);
  }


  /* ---------- Danh sách tool: lọc, nhóm, xem trước ---------- */
  var tl = $(".tl");
  if (tl) initToolList(tl);

  function initToolList(tl) {
    var cards = $$(".tcard", tl);
    var groups = $$(".tl-group", tl);
    var input = $(".tl-search input");
    var tabs = $$(".tl-tab");
    var count = $(".tl-count");
    var empty = $(".tl-empty", tl);
    var pv = $(".tl-preview", tl);
    var activeGroup = "";
    var current = null, hoverTimer = 0, token = 0;
    var GROUP_CLASSES = ["g-asset", "g-prefab", "g-optimize"];

    cards.forEach(function (c) {
      c._n = norm(c.textContent + " " + (c.getAttribute("data-when") || "") + " " + c.getAttribute("data-group"));
    });

    function field(name) { return pv && pv.querySelector('[data-f="' + name + '"]'); }

    function show(card) {
      if (!pv || !card || card === current) return;
      if (current) current.classList.remove("is-current");
      current = card;
      card.classList.add("is-current");
      var slug = card.getAttribute("data-slug");
      var group = card.getAttribute("data-group");
      var name = $(".tcard__name", card).textContent;
      GROUP_CLASSES.forEach(function (g) { pv.classList.remove(g); });
      pv.classList.add("g-" + group.toLowerCase());
      field("group").textContent = group;
      field("name").textContent = name;
      field("summary").innerHTML = $(".tcard__sum", card).innerHTML;
      field("when").textContent = card.getAttribute("data-when");
      field("steps").textContent = card.getAttribute("data-steps") + " bước";
      var mod = card.getAttribute("data-module");
      var m = field("module");
      m.hidden = !mod;
      m.textContent = mod ? "Cần cài module " + mod : "";
      field("href").setAttribute("href", card.getAttribute("href"));
      var img = $(".frame__img img", pv);
      var frameEl = $(".frame", pv);
      var src = root + "images/" + slug + ".png";
      var my = ++token;
      pv.classList.add("is-loading");
      var pre = new Image();
      pre.onload = pre.onerror = function () {
        if (my !== token) return;
        img.src = src;
        frameEl.style.setProperty("--ch", card.getAttribute("data-ch") || 681);
        img.alt = "Ảnh chụp " + name + " trong cửa sổ Editor Tools.";
        pv.classList.remove("is-loading");
      };
      pre.src = src;
    }

    tl.addEventListener("pointerover", function (e) {
      var c = e.target.closest(".tcard");
      if (!c) return;
      clearTimeout(hoverTimer);
      hoverTimer = setTimeout(function () { show(c); }, 70);
    });
    tl.addEventListener("focusin", function (e) {
      var c = e.target.closest(".tcard");
      if (c) show(c);
    });
    if (fine) {
      tl.addEventListener("pointermove", function (e) {
        var c = e.target.closest(".tcard");
        if (!c) return;
        var r = c.getBoundingClientRect();
        c.style.setProperty("--mx", (e.clientX - r.left) + "px");
        c.style.setProperty("--my", (e.clientY - r.top) + "px");
      }, { passive: true });
    }

    function apply() {
      var q = input ? norm(input.value.trim()) : "";
      var parts = q.split(/\s+/).filter(Boolean);
      var shown = 0, firstShown = null;
      cards.forEach(function (c) {
        var ok = (!activeGroup || c.getAttribute("data-group") === activeGroup) &&
          parts.every(function (p) { return c._n.indexOf(p) >= 0; });
        c.hidden = !ok;
        if (ok) { shown++; if (!firstShown) firstShown = c; }
      });
      groups.forEach(function (g) { g.hidden = !$$(".tcard:not([hidden])", g).length; });
      if (empty) empty.hidden = shown > 0;
      if (count) count.textContent = shown === cards.length ? cards.length + " tool" : "Hiện " + shown + " / " + cards.length + " tool";
      if (firstShown && (!current || current.hidden)) { current && current.classList.remove("is-current"); current = null; show(firstShown); }
      if (window.ScrollTrigger) ScrollTrigger.refresh();
    }

    function setGroup(g) {
      activeGroup = g;
      tabs.forEach(function (t) {
        var on = t.getAttribute("data-g") === g;
        t.setAttribute("aria-pressed", on ? "true" : "false");
      });
      apply();
    }
    tabs.forEach(function (t) {
      t.addEventListener("click", function () {
        setGroup(t.getAttribute("data-g"));
        var head = $(".tl-bar");
        if (head && head.getBoundingClientRect().top > innerHeight * 0.6) scrollToEl(head);
      });
    });
    if (input) {
      input.addEventListener("input", apply);
      input.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && input.value) { e.stopPropagation(); input.value = ""; apply(); }
        if (e.key === "Enter") { var f = $(".tcard:not([hidden])", tl); if (f) { e.preventDefault(); f.focus(); } }
      });
    }
    var m = /^#nhom-(asset|prefab|optimize)$/.exec(location.hash);
    if (m) setGroup(m[1].charAt(0).toUpperCase() + m[1].slice(1)); else apply();
    current = null;
    show(m ? $(".tcard:not([hidden])", tl) : cards[0]);
  }

  /* ---------- Menu Tools > Feeder ---------- */
  var mock = $(".menu-mock");
  if (mock) {
    var desc = $(".mm-desc", mock);
    var subs = $$(".mm-sub", mock);
    var openers = $$(".mm-root .mm-has", mock);
    var onItem = function (li) {
      if (!li || !mock.contains(li)) return;
      var sub = li.getAttribute("data-sub");
      if (sub) {
        openers.forEach(function (o) { o.classList.toggle("is-open", o === li); });
        subs.forEach(function (u) { u.hidden = u.getAttribute("data-sub") !== sub; });
        desc.textContent = "Menu con " + sub + ": " + $$("li", subs.filter(function (u) { return !u.hidden; })[0]).length + " cửa sổ.";
        return;
      }
      var d = li.getAttribute("data-desc");
      if (d) {
        var i = d.indexOf(": ");
        desc.innerHTML = "<b>" + esc(d.slice(0, i)) + "</b>: " + esc(d.slice(i + 2));
      }
    };
    mock.addEventListener("pointerover", function (e) { onItem(e.target.closest("li")); });
    mock.addEventListener("focusin", function (e) { onItem(e.target.closest("li")); });
  }

  /* Ảnh trong khung: lưu ý missing */
  $$(".shot img").forEach(function (img) {
    if (img.complete && img.naturalWidth === 0) img.closest(".shot").classList.add("missing");
  });
})();
