const menuButton = document.querySelector('#menuButton');
const topnav = document.querySelector('.topnav');
const sidebar = document.querySelector('#sidebar');
const searchInput = document.querySelector('#searchInput');

menuButton?.addEventListener('click', () => {
  const open = topnav.classList.toggle('open');
  sidebar.classList.toggle('open', open);
  menuButton.setAttribute('aria-expanded', String(open));
});

document.querySelectorAll('.copy-button').forEach((button) => {
  button.addEventListener('click', async () => {
    const value = button.dataset.copy ?? '';
    try {
      await navigator.clipboard.writeText(value);
      const original = button.textContent;
      button.textContent = '已复制';
      window.setTimeout(() => { button.textContent = original; }, 1200);
    } catch {
      button.textContent = '复制失败';
      window.setTimeout(() => { button.textContent = '复制'; }, 1200);
    }
  });
});

searchInput?.addEventListener('input', () => {
  const query = searchInput.value.trim().toLowerCase();
  document.querySelectorAll('article[data-section]').forEach((article) => {
    const matches = !query || article.textContent.toLowerCase().includes(query);
    article.classList.toggle('hidden', !matches);
  });
});

const links = [...document.querySelectorAll('.sidebar-link')];
const sections = [...document.querySelectorAll('article[data-section]')];
const observer = new IntersectionObserver((entries) => {
  const visible = entries.filter((entry) => entry.isIntersecting).sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
  if (!visible) return;
  links.forEach((link) => link.classList.toggle('active', link.getAttribute('href') === `#${visible.target.id}`));
}, { rootMargin: '-15% 0px -70% 0px', threshold: [0, .2, .5] });
sections.forEach((section) => observer.observe(section));
