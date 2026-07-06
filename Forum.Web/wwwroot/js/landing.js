// Forum.Web/wwwroot/js/landing.js
// Client-side motion for the Quorum marketing landing page (LOCKED spec §6–§7).
// Loaded as a self-initializing ES module from Landing.razor:
//     <script type="module" src="js/landing.js"></script>
// It auto-bootstraps off the #q-landing root's data-motion attribute (see the
// bottom of this file). Everything degrades gracefully: if this never runs, the
// page is fully visible (reveal elements only get their hidden state applied
// *here*, first).
//
// Also exports init(opts)/dispose() for programmatic use (e.g. interop/tests).

let cleanupFns = [];
let started = false;

const REDUCED = () =>
  window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

export function init(opts) {
  if (started) return;
  started = true;

  const motionIntensity =
    opts && typeof opts.motionIntensity === 'number' ? opts.motionIntensity : 0.6;
  const reduced = REDUCED();

  setupProgressBar();
  setupNavState();
  setupReveal(reduced);
  setupCountUp(reduced);
  setupWaveField(motionIntensity, reduced);
}

export function dispose() {
  cleanupFns.forEach((fn) => { try { fn(); } catch { /* noop */ } });
  cleanupFns = [];
  started = false;
}

function on(target, evt, handler, opts) {
  target.addEventListener(evt, handler, opts);
  cleanupFns.push(() => target.removeEventListener(evt, handler, opts));
}

// ---------------------------------------------------------------- §7 progress
function setupProgressBar() {
  const bar = document.getElementById('q-progress');
  if (!bar) return;
  const update = () => {
    const h = document.documentElement;
    const max = h.scrollHeight - h.clientHeight;
    bar.style.width = (max > 0 ? (h.scrollTop / max) * 100 : 0) + '%';
  };
  on(window, 'scroll', update, { passive: true });
  on(window, 'resize', update);
  update();
}

// ---------------------------------------------------------------- §7 nav state
function setupNavState() {
  const nav = document.getElementById('q-nav');
  if (!nav) return;
  const update = () => nav.classList.toggle('scrolled', window.scrollY > 24);
  on(window, 'scroll', update, { passive: true });
  update();
}

// ---------------------------------------------------------------- §7 reveal
function setupReveal(reduced) {
  const els = Array.from(document.querySelectorAll('[data-reveal]'));
  if (!els.length) return;

  if (reduced || !('IntersectionObserver' in window)) {
    els.forEach((el) => el.classList.add('q-revealed'));
    return;
  }

  // Apply hidden state now (JS-only). No-JS never reaches this → stays visible.
  els.forEach((el) => el.classList.add('q-reveal-init'));

  const io = new IntersectionObserver(
    (entries, obs) => {
      // Stagger siblings entering together by 90ms, capped at ~5.
      const shown = entries.filter((e) => e.isIntersecting);
      shown.forEach((entry, i) => {
        const delay = Math.min(i, 5) * 90;
        setTimeout(() => entry.target.classList.add('q-revealed'), delay);
        obs.unobserve(entry.target); // reveal once
      });
    },
    { threshold: 0.12, rootMargin: '0px 0px -8% 0px' }
  );
  els.forEach((el) => io.observe(el));
  cleanupFns.push(() => io.disconnect());
}

// ---------------------------------------------------------------- §7 count-up
function setupCountUp(reduced) {
  const nums = Array.from(document.querySelectorAll('[data-count]'));
  if (!nums.length) return;

  const render = (el, value) => {
    const decimals = parseInt(el.dataset.decimals || '0', 10);
    const prefix = el.dataset.prefix || '';
    const suffix = el.dataset.suffix || '';
    el.textContent = prefix + value.toFixed(decimals) + suffix;
  };

  if (reduced || !('IntersectionObserver' in window)) {
    nums.forEach((el) => render(el, parseFloat(el.dataset.count)));
    return;
  }

  nums.forEach((el) => render(el, 0));

  const easeOut = (t) => 1 - Math.pow(1 - t, 3);
  const animate = (el) => {
    const target = parseFloat(el.dataset.count);
    const duration = 1500;
    let start = null;
    const step = (ts) => {
      if (start === null) start = ts;
      const p = Math.min((ts - start) / duration, 1);
      render(el, target * easeOut(p));
      if (p < 1) requestAnimationFrame(step);
      else render(el, target);
    };
    requestAnimationFrame(step);
  };

  const io = new IntersectionObserver(
    (entries, obs) => {
      entries.forEach((e) => {
        if (e.isIntersecting) { animate(e.target); obs.unobserve(e.target); }
      });
    },
    { threshold: 0.3 }
  );
  nums.forEach((el) => io.observe(el));
  cleanupFns.push(() => io.disconnect());
}

// ---------------------------------------------------- §6.1 canvas wave-field
function setupWaveField(motionIntensity, reduced) {
  const canvas = document.getElementById('quorum-bg-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  const NEAR = [74, 110, 224];  // --accent (top)
  const FAR = [122, 59, 224];   // violet (bottom)
  const LINES = 6;

  let w = 0, h = 0, dpr = 1;
  const resize = () => {
    dpr = Math.min(window.devicePixelRatio || 1, 2); // DPR-aware, capped 2×
    w = window.innerWidth;
    h = window.innerHeight;
    canvas.width = Math.floor(w * dpr);
    canvas.height = Math.floor(h * dpr);
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  };
  resize();
  on(window, 'resize', resize);

  const lerp = (a, b, t) => Math.round(a + (b - a) * t);
  const amp = 26 * motionIntensity;
  const baseAlpha = 0.5 * motionIntensity;

  const drawFrame = (time) => {
    ctx.clearRect(0, 0, w, h);
    const top = h * 0.16;
    const gap = (h * 0.6) / (LINES - 1);

    for (let i = 0; i < LINES; i++) {
      const t = i / (LINES - 1);
      const y0 = top + gap * i;
      const r = lerp(NEAR[0], FAR[0], t);
      const g = lerp(NEAR[1], FAR[1], t);
      const b = lerp(NEAR[2], FAR[2], t);

      // Horizontal gradient stroke, fading in/out at the edges.
      const grad = ctx.createLinearGradient(0, 0, w, 0);
      const a = baseAlpha * (0.85 - t * 0.4);
      grad.addColorStop(0, `rgba(${r},${g},${b},0)`);
      grad.addColorStop(0.5, `rgba(${r},${g},${b},${a})`);
      grad.addColorStop(1, `rgba(${r},${g},${b},0)`);
      ctx.strokeStyle = grad;
      ctx.lineWidth = 1.4;

      ctx.beginPath();
      const phase1 = time * 0.00018 + i * 0.7;
      const phase2 = time * 0.00031 - i * 0.4;
      const k1 = 0.0016 + i * 0.0002;
      const k2 = 0.0037 - i * 0.00015;
      for (let x = 0; x <= w; x += 6) {
        // Two summed sines per line, slow phase drift.
        const y = y0
          + Math.sin(x * k1 + phase1) * amp
          + Math.sin(x * k2 + phase2) * amp * 0.4;
        if (x === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();
    }
  };

  if (reduced) {
    drawFrame(0); // static frame, no loop
    return;
  }

  let raf = 0;
  let running = true;
  const loop = (ts) => {
    if (!running) return;
    drawFrame(ts);
    raf = requestAnimationFrame(loop);
  };
  raf = requestAnimationFrame(loop);

  // Pause when the tab is hidden / offscreen.
  const onVis = () => {
    if (document.hidden) {
      running = false;
      cancelAnimationFrame(raf);
    } else if (!running) {
      running = true;
      raf = requestAnimationFrame(loop);
    }
  };
  on(document, 'visibilitychange', onVis);
  cleanupFns.push(() => { running = false; cancelAnimationFrame(raf); });
}

// ------------------------------------------------------------ auto-bootstrap
// Read motionIntensity from the landing root and start. Runs once the module
// loads; guarded so a missing root (module imported elsewhere) is a no-op.
function bootstrap() {
  const root = document.getElementById('q-landing');
  if (!root) return;
  const raw = parseFloat(root.dataset.motion);
  init({ motionIntensity: Number.isFinite(raw) ? raw : 0.6 });
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', bootstrap, { once: true });
} else {
  bootstrap();
}
