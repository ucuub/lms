/* ─────────────────────────────────────────────────────────────────
   site.js — Global UI interactions
   ───────────────────────────────────────────────────────────────── */

document.addEventListener('DOMContentLoaded', function () {

  // ── 1. Auto-dismiss alerts after 5 s ──────────────────────────
  document.querySelectorAll('.alert').forEach(function (alert) {
    setTimeout(function () {
      alert.style.transition = 'opacity 0.5s, transform 0.5s';
      alert.style.opacity = '0';
      alert.style.transform = 'translateY(-8px)';
      setTimeout(() => alert.remove(), 500);
    }, 5000);
  });

  // ── 2. Ripple effect on every .btn ────────────────────────────
  document.addEventListener('click', function (e) {
    const btn = e.target.closest('.btn');
    if (!btn || btn.disabled || btn.classList.contains('loading')) return;

    const rect = btn.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height) * 2;
    const x    = e.clientX - rect.left - size / 2;
    const y    = e.clientY - rect.top  - size / 2;

    const ripple = document.createElement('span');
    ripple.className = 'ripple';
    ripple.style.cssText =
      `width:${size}px;height:${size}px;left:${x}px;top:${y}px`;
    btn.appendChild(ripple);
    ripple.addEventListener('animationend', () => ripple.remove());
  });

  // ── 3. Form submit → loading state on submit button ───────────
  document.querySelectorAll('form').forEach(function (form) {
    if (form.dataset.noLoading !== undefined) return;

    form.addEventListener('submit', function () {
      // find which submit btn was clicked (focused) or first one
      const submitBtn =
        form.querySelector('button[type=submit]:focus') ||
        form.querySelector('button[type=submit]');

      if (!submitBtn || submitBtn.classList.contains('loading')) return;

      setTimeout(() => {
        submitBtn.classList.add('loading');
        submitBtn.dataset.originalHtml = submitBtn.innerHTML;
        submitBtn.innerHTML = '<span class="btn-spinner"></span> Memproses...';
        submitBtn.disabled = true;

        // Safety reset after 10 s (page might not navigate on error)
        setTimeout(() => {
          if (submitBtn.classList.contains('loading')) {
            submitBtn.disabled = false;
            submitBtn.classList.remove('loading');
            submitBtn.innerHTML = submitBtn.dataset.originalHtml || 'Submit';
          }
        }, 10000);
      }, 0);
    });
  });

  // ── 4. Link-buttons that navigate — subtle loading state ──────
  document.querySelectorAll('a.btn[href]:not([target="_blank"])').forEach(function (link) {
    link.addEventListener('click', function () {
      // Don't lock if it's just an anchor or has modifier key pressed
      link.classList.add('loading');
    });
    window.addEventListener('pageshow', () => link.classList.remove('loading'));
  });

  // ── 5. Input focus — highlight parent form-group ──────────────
  document.querySelectorAll('.form-control, select.form-control').forEach(function (el) {
    el.addEventListener('focus', () =>
      el.closest('.form-group')?.classList.add('focused'));
    el.addEventListener('blur', () =>
      el.closest('.form-group')?.classList.remove('focused'));
  });

  // ── 6. Navbar active link highlight ───────────────────────────
  const path = window.location.pathname.toLowerCase();
  document.querySelectorAll('.navbar-menu a').forEach(function (link) {
    const href = (link.getAttribute('href') || '').toLowerCase();
    if (!href || href === '#' || href === '/') return;
    if (path.startsWith(href)) {
      link.classList.add('nav-active');
    }
  });

});
