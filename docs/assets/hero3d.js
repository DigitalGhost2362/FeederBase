/* Hero 3D: một mesh low-poly đặt trên lưới Scene view, có Box Collider và gizmo XYZ.
   Cuộn trang thì mesh "unpack" ra từng mặt; chuột đẩy hạt bụi và xoay vật thể.
   Dùng three.js r149 bản UMD (window.THREE). */
(function () {
  "use strict";

  function init(stage, opts) {
    var THREE = window.THREE;
    if (!THREE) throw new Error("three.js chưa tải");
    var reduce = !!opts.reduce;
    var small = window.innerWidth < 760;

    var renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: "high-performance" });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, small ? 1.5 : 1.75));
    renderer.setClearColor(0x000000, 0);
    stage.appendChild(renderer.domElement);
    renderer.domElement.setAttribute("aria-hidden", "true");

    var scene = new THREE.Scene();
    scene.fog = new THREE.Fog(0x0f131a, 9, 22);
    var camera = new THREE.PerspectiveCamera(34, 1, 0.1, 60);
    var camBase = new THREE.Vector3(0, 1.4, 8.2);

    var C = {
      handle: new THREE.Color("#ffc44d"),
      x: new THREE.Color("#ff5a6a"),
      y: new THREE.Color("#7fe08e"),
      z: new THREE.Color("#58a0ff"),
      ink: new THREE.Color("#1c2330")
    };

    /* ---------- Lưới Scene view ---------- */
    var gridMat = new THREE.ShaderMaterial({
      transparent: true, depthWrite: false,
      extensions: { derivatives: true },
      uniforms: { uTime: { value: 0 }, uX: { value: C.x }, uZ: { value: C.z } },
      vertexShader: [
        "varying vec3 vW;",
        "void main(){ vec4 w = modelMatrix * vec4(position,1.0); vW = w.xyz; gl_Position = projectionMatrix * viewMatrix * w; }"
      ].join("\n"),
      fragmentShader: [
        "uniform float uTime; uniform vec3 uX; uniform vec3 uZ; varying vec3 vW;",
        "float line(vec2 p, float s){ vec2 g = abs(fract(p/s - 0.5) - 0.5) / fwidth(p/s); return 1.0 - min(min(g.x, g.y), 1.0); }",
        "void main(){",
        "  vec2 p = vW.xz;",
        "  float d = length(p);",
        "  float fade = smoothstep(11.0, 1.5, d);",
        "  float a = line(p, 0.5) * 0.18 + line(p, 2.5) * 0.35;",
        "  vec3 col = vec3(0.62, 0.7, 0.86) * a;",
        "  float ax = 1.0 - min(abs(p.y) / fwidth(p.y) / 1.5, 1.0);",
        "  float az = 1.0 - min(abs(p.x) / fwidth(p.x) / 1.5, 1.0);",
        "  col = mix(col, uX, ax * 0.9); col = mix(col, uZ, az * 0.9);",
        "  float alpha = max(a, max(ax, az) * 0.8) * fade;",
        "  float pulse = smoothstep(0.15, 0.0, abs(d - mod(uTime * 1.6, 12.0))) * fade * 0.35;",
        "  col += vec3(1.0, 0.77, 0.3) * pulse; alpha += pulse;",
        "  gl_FragColor = vec4(col, alpha);",
        "}"
      ].join("\n")
    });
    var grid = new THREE.Mesh(new THREE.PlaneGeometry(26, 26), gridMat);
    grid.rotation.x = -Math.PI / 2;
    grid.position.y = -1.35;
    scene.add(grid);

    /* ---------- Mesh low-poly (non-indexed để nổ từng mặt) ---------- */
    var rig = new THREE.Group();
    scene.add(rig);

    var base = new THREE.IcosahedronGeometry(1.15, 1);
    var pos = base.attributes.position;
    var seed = 7;
    function rnd() { seed = (seed * 16807) % 2147483647; return seed / 2147483647; }
    // Bóp méo đỉnh có kiểm soát: dùng map theo vị trí để các mặt kề nhau vẫn liền
    var jitter = {};
    for (var i = 0; i < pos.count; i++) {
      var k = pos.getX(i).toFixed(3) + "," + pos.getY(i).toFixed(3) + "," + pos.getZ(i).toFixed(3);
      if (!jitter[k]) jitter[k] = 0.82 + rnd() * 0.36;
      var s = jitter[k];
      pos.setXYZ(i, pos.getX(i) * s, pos.getY(i) * s * 1.08, pos.getZ(i) * s);
    }
    var geo = base.index ? base.toNonIndexed() : base;
    geo.computeVertexNormals();
    var n = geo.attributes.position.count;
    var centers = new Float32Array(n * 3), rands = new Float32Array(n), bary = new Float32Array(n * 3);
    var p = geo.attributes.position.array;
    for (var f = 0; f < n; f += 3) {
      var cx = (p[f * 3] + p[f * 3 + 3] + p[f * 3 + 6]) / 3;
      var cy = (p[f * 3 + 1] + p[f * 3 + 4] + p[f * 3 + 7]) / 3;
      var cz = (p[f * 3 + 2] + p[f * 3 + 5] + p[f * 3 + 8]) / 3;
      var r = rnd();
      for (var v = 0; v < 3; v++) {
        centers[(f + v) * 3] = cx; centers[(f + v) * 3 + 1] = cy; centers[(f + v) * 3 + 2] = cz;
        rands[f + v] = r;
        bary[(f + v) * 3 + v] = 1;
      }
    }
    geo.setAttribute("aCenter", new THREE.BufferAttribute(centers, 3));
    geo.setAttribute("aRand", new THREE.BufferAttribute(rands, 1));
    geo.setAttribute("aBary", new THREE.BufferAttribute(bary, 3));

    var meshMat = new THREE.ShaderMaterial({
      extensions: { derivatives: true },
      side: THREE.DoubleSide,
      transparent: true,
      uniforms: {
        uTime: { value: 0 }, uExplode: { value: 0 },
        uHandle: { value: C.handle }, uInk: { value: C.ink }, uZ: { value: C.z }
      },
      vertexShader: [
        "uniform float uTime; uniform float uExplode;",
        "attribute vec3 aCenter; attribute float aRand; attribute vec3 aBary;",
        "varying vec3 vN; varying vec3 vBary; varying float vRand; varying vec3 vView;",
        "mat3 rotA(vec3 ax, float a){ ax = normalize(ax); float s = sin(a), c = cos(a), oc = 1.0 - c;",
        "  return mat3(oc*ax.x*ax.x+c, oc*ax.x*ax.y+ax.z*s, oc*ax.z*ax.x-ax.y*s,",
        "              oc*ax.x*ax.y-ax.z*s, oc*ax.y*ax.y+c, oc*ax.y*ax.z+ax.x*s,",
        "              oc*ax.z*ax.x+ax.y*s, oc*ax.y*ax.z-ax.x*s, oc*ax.z*ax.z+c); }",
        "void main(){",
        "  float e = uExplode * (0.55 + aRand * 0.9);",
        "  vec3 local = position - aCenter;",
        "  mat3 R = rotA(vec3(aRand, 1.0 - aRand, 0.5), e * 3.0);",
        "  vec3 dir = normalize(aCenter);",
        "  float breathe = sin(uTime * 1.3 + aRand * 6.283) * 0.012;",
        "  vec3 pos = aCenter * (1.0 + breathe) + dir * e * 1.9 + R * local * (1.0 - uExplode * 0.25);",
        "  vN = normalize(normalMatrix * (R * normal));",
        "  vBary = aBary; vRand = aRand;",
        "  vec4 mv = modelViewMatrix * vec4(pos, 1.0);",
        "  vView = normalize(-mv.xyz);",
        "  gl_Position = projectionMatrix * mv;",
        "}"
      ].join("\n"),
      fragmentShader: [
        "uniform float uTime; uniform float uExplode; uniform vec3 uHandle; uniform vec3 uInk; uniform vec3 uZ;",
        "varying vec3 vN; varying vec3 vBary; varying float vRand; varying vec3 vView;",
        "void main(){",
        "  vec3 N = normalize(vN); if(!gl_FrontFacing) N = -N;",
        "  vec3 L = normalize(vec3(0.5, 0.9, 0.6));",
        "  float diff = max(dot(N, L), 0.0);",
        "  float fres = pow(1.0 - max(dot(N, vView), 0.0), 2.5);",
        "  vec3 base = uInk * (0.55 + diff * 0.9) + vec3(0.05, 0.07, 0.1) * vRand;",
        "  vec3 d = fwidth(vBary); vec3 a3 = smoothstep(vec3(0.0), d * 1.4, vBary);",
        "  float edge = 1.0 - min(min(a3.x, a3.y), a3.z);",
        "  float scan = smoothstep(0.07, 0.0, abs(fract(vRand * 0.35 + uTime * 0.09) - 0.5));",
        "  vec3 wire = mix(uZ, uHandle, clamp(scan + uExplode, 0.0, 1.0));",
        "  vec3 col = base + uHandle * fres * 0.45;",
        "  col = mix(col, wire * 1.25, edge * 0.9);",
        "  gl_FragColor = vec4(col, 0.97);",
        "}"
      ].join("\n")
    });
    var mesh = new THREE.Mesh(geo, meshMat);
    rig.add(mesh);

    /* ---------- Box Collider (xanh lá như gizmo collider của Unity) ---------- */
    var boxGeo = new THREE.EdgesGeometry(new THREE.BoxGeometry(2.5, 2.7, 2.5));
    var boxMat = new THREE.LineBasicMaterial({ color: C.y, transparent: true, opacity: 0.75 });
    var box = new THREE.LineSegments(boxGeo, boxMat);
    rig.add(box);
    var cornerGeo = new THREE.BufferGeometry();
    var corners = [];
    [-1, 1].forEach(function (a) { [-1, 1].forEach(function (b) { [-1, 1].forEach(function (c) { corners.push(a * 1.25, b * 1.35, c * 1.25); }); }); });
    cornerGeo.setAttribute("position", new THREE.Float32BufferAttribute(corners, 3));
    var cornerPts = new THREE.Points(cornerGeo, new THREE.PointsMaterial({ color: C.y, size: 0.09, sizeAttenuation: true }));
    rig.add(cornerPts);

    /* ---------- Gizmo XYZ ---------- */
    var gizmo = new THREE.Group();
    function arrow(color, rot) {
      var g = new THREE.Group();
      var m = new THREE.MeshBasicMaterial({ color: color, depthTest: false, transparent: true });
      var shaft = new THREE.Mesh(new THREE.CylinderGeometry(0.018, 0.018, 1.1, 8), m);
      shaft.position.y = 0.55;
      var tip = new THREE.Mesh(new THREE.ConeGeometry(0.07, 0.22, 12), m);
      tip.position.y = 1.2;
      g.add(shaft); g.add(tip);
      g.rotation.set(rot[0], rot[1], rot[2]);
      g.renderOrder = 10; shaft.renderOrder = 10; tip.renderOrder = 10;
      return g;
    }
    gizmo.add(arrow(C.x, [0, 0, -Math.PI / 2]));
    gizmo.add(arrow(C.y, [0, 0, 0]));
    gizmo.add(arrow(C.z, [Math.PI / 2, 0, 0]));
    var pivot = new THREE.Mesh(new THREE.BoxGeometry(0.12, 0.12, 0.12), new THREE.MeshBasicMaterial({ color: 0xe7ebf3, depthTest: false }));
    pivot.renderOrder = 11;
    gizmo.add(pivot);
    rig.add(gizmo);

    /* ---------- Các "prefab" nhỏ bay quanh ---------- */
    var orbiters = [];
    var orbitGeos = [new THREE.BoxGeometry(0.26, 0.26, 0.26), new THREE.OctahedronGeometry(0.2), new THREE.CylinderGeometry(0.13, 0.13, 0.3, 6), new THREE.TetrahedronGeometry(0.22), new THREE.ConeGeometry(0.16, 0.32, 5)];
    var orbitCols = [C.x, C.y, C.z, C.handle, C.z];
    for (var o = 0; o < orbitGeos.length; o++) {
      var wire = new THREE.LineSegments(new THREE.EdgesGeometry(orbitGeos[o]), new THREE.LineBasicMaterial({ color: orbitCols[o], transparent: true, opacity: 0.9 }));
      var fill = new THREE.Mesh(orbitGeos[o], new THREE.MeshBasicMaterial({ color: 0x151a23, transparent: true, opacity: 0.85 }));
      var g2 = new THREE.Group(); g2.add(fill); g2.add(wire);
      g2.userData = { r: 2.3 + o * 0.28, s: 0.18 + o * 0.035, ph: o * 1.4, h: (o % 2 ? 0.6 : -0.3) + o * 0.12 };
      scene.add(g2); orbiters.push(g2);
    }

    /* ---------- Hạt bụi phản ứng chuột ---------- */
    var count = small ? 650 : 1500;
    var pp = new Float32Array(count * 3), ps = new Float32Array(count), pc = new Float32Array(count);
    for (var q = 0; q < count; q++) {
      var rr = 2.2 + Math.pow(rnd(), 0.7) * 6.5, th = rnd() * Math.PI * 2, ph2 = (rnd() - 0.5) * 1.6;
      pp[q * 3] = Math.cos(th) * rr; pp[q * 3 + 1] = ph2 * 3.2 + 0.6; pp[q * 3 + 2] = Math.sin(th) * rr - 1.5;
      ps[q] = 0.5 + rnd() * 1.5; pc[q] = rnd();
    }
    var pGeo = new THREE.BufferGeometry();
    pGeo.setAttribute("position", new THREE.BufferAttribute(pp, 3));
    pGeo.setAttribute("aSize", new THREE.BufferAttribute(ps, 1));
    pGeo.setAttribute("aKind", new THREE.BufferAttribute(pc, 1));
    var pMat = new THREE.ShaderMaterial({
      transparent: true, depthWrite: false, blending: THREE.AdditiveBlending,
      uniforms: { uTime: { value: 0 }, uMouse: { value: new THREE.Vector3(99, 99, 99) }, uScroll: { value: 0 }, uPR: { value: renderer.getPixelRatio() }, uHandle: { value: C.handle } },
      vertexShader: [
        "uniform float uTime; uniform vec3 uMouse; uniform float uScroll; uniform float uPR;",
        "attribute float aSize; attribute float aKind; varying float vKind; varying float vA;",
        "void main(){",
        "  vec3 p = position;",
        "  p.y += mod(uTime * 0.08 * (0.4 + aKind) + uScroll * 2.0 + aKind * 7.0, 7.0) - 3.0;",
        "  p.x += sin(uTime * 0.3 + aKind * 12.0) * 0.12;",
        "  vec3 d = p - uMouse; float l = length(d);",
        "  p += normalize(d) * smoothstep(1.8, 0.0, l) * 0.9;",
        "  vec4 mv = modelViewMatrix * vec4(p, 1.0);",
        "  gl_Position = projectionMatrix * mv;",
        "  gl_PointSize = aSize * uPR * (14.0 / -mv.z);",
        "  vKind = aKind; vA = smoothstep(18.0, 4.0, -mv.z) * (0.35 + smoothstep(1.8, 0.0, l) * 0.9);",
        "}"
      ].join("\n"),
      fragmentShader: [
        "uniform vec3 uHandle; varying float vKind; varying float vA;",
        "void main(){ vec2 c = gl_PointCoord - 0.5; float d = length(c); if(d > 0.5) discard;",
        "  vec3 col = vKind > 0.86 ? uHandle : vec3(0.66, 0.75, 0.92);",
        "  gl_FragColor = vec4(col, smoothstep(0.5, 0.0, d) * vA); }"
      ].join("\n")
    });
    var dust = new THREE.Points(pGeo, pMat);
    scene.add(dust);

    /* ---------- Bố cục, chuột, cuộn ---------- */
    function layout() {
      var w = stage.clientWidth, h = stage.clientHeight;
      small = w < 760;
      renderer.setSize(w, h, false);
      camera.aspect = w / h;
      camera.updateProjectionMatrix();
      rig.position.set(small ? 0.3 : 2.35, small ? 1.15 : 0.15, 0);
      var sc = small ? 0.78 : 1;
      rig.scale.setScalar(sc);
    }
    layout();
    window.addEventListener("resize", layout);

    var mouse = new THREE.Vector2(0, 0), mTarget = new THREE.Vector2(0, 0);
    var ray = new THREE.Raycaster(), plane = new THREE.Plane(new THREE.Vector3(0, 0, 1), 0), hit = new THREE.Vector3();
    var hasMouse = false;
    window.addEventListener("pointermove", function (e) {
      mTarget.set((e.clientX / window.innerWidth) * 2 - 1, -(e.clientY / window.innerHeight) * 2 + 1);
      hasMouse = true;
    }, { passive: true });

    var scrollP = 0;
    function readScroll() {
      var h = stage.clientHeight || window.innerHeight;
      scrollP = Math.min(1, Math.max(0, window.scrollY / (h * 0.9)));
    }
    window.addEventListener("scroll", readScroll, { passive: true });
    readScroll();

    var visible = true, running = false, t0 = performance.now(), last = t0;
    var rotY = 0.4, explode = 0;

    function frame(now) {
      var dt = Math.min(0.05, (now - last) / 1000); last = now;
      var t = (now - t0) / 1000;
      mouse.lerp(mTarget, 0.06);
      explode += (scrollP - explode) * 0.08;
      var ease = explode * explode * (3 - 2 * explode);

      gridMat.uniforms.uTime.value = t;
      meshMat.uniforms.uTime.value = t;
      meshMat.uniforms.uExplode.value = ease;
      pMat.uniforms.uTime.value = t;
      pMat.uniforms.uScroll.value = explode;

      rotY += dt * (0.22 + ease * 0.6);
      mesh.rotation.y = rotY + mouse.x * 0.5;
      mesh.rotation.x = -mouse.y * 0.35 + Math.sin(t * 0.5) * 0.06;
      box.rotation.copy(mesh.rotation);
      cornerPts.rotation.copy(mesh.rotation);
      box.scale.setScalar(1 + ease * 0.6);
      cornerPts.scale.copy(box.scale);
      boxMat.opacity = 0.75 * (1 - ease * 0.6) * (0.75 + Math.sin(t * 2.2) * 0.25);
      gizmo.rotation.y = mouse.x * 0.25 - 0.6;
      gizmo.scale.setScalar(1 - ease * 0.4);
      rig.position.y = (small ? 1.15 : 0.15) + Math.sin(t * 0.9) * 0.06;

      for (var i = 0; i < orbiters.length; i++) {
        var u = orbiters[i].userData, a = t * u.s + u.ph;
        orbiters[i].position.set(rig.position.x + Math.cos(a) * u.r * (1 + ease), rig.position.y + u.h + Math.sin(t * 0.7 + u.ph) * 0.2, Math.sin(a) * u.r * (1 + ease) - 0.2);
        orbiters[i].rotation.x = t * 0.6 + i; orbiters[i].rotation.y = t * 0.4 + i;
      }

      camera.position.set(camBase.x + mouse.x * 0.35, camBase.y + mouse.y * 0.25 + ease * 0.6, camBase.z + ease * 1.2);
      camera.lookAt(small ? 0.3 : 1.1, 0.25 + ease * 0.3, 0);

      if (hasMouse) {
        ray.setFromCamera(mouse, camera);
        if (ray.ray.intersectPlane(plane, hit)) pMat.uniforms.uMouse.value.copy(hit);
      }
      renderer.render(scene, camera);
      if (running) requestAnimationFrame(frame);
    }

    function play() { if (running || reduce || !visible || document.hidden) return; running = true; last = performance.now(); requestAnimationFrame(frame); }
    function pause() { running = false; }

    if (reduce) {
      frame(performance.now());
      window.addEventListener("resize", function () { frame(performance.now()); });
      return;
    }
    if ("IntersectionObserver" in window) {
      new IntersectionObserver(function (en) { visible = en[0].isIntersecting; visible ? play() : pause(); }).observe(stage);
    }
    document.addEventListener("visibilitychange", function () { document.hidden ? pause() : play(); });
    play();
  }

  window.FeederHero = { init: init };
})();
